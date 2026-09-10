# Appointment Booking System

A full-stack appointment booking system: ASP.NET Core Web API (C#) + SQL Server + Entity Framework Core backend, React + Vite frontend, Jenkins CI pipeline running in Docker.

## Structure

```
appointment-booking/
├── backend/      ASP.NET Core Web API (C#, .NET 8)
│   ├── AppointmentBooking.Api/         The API project
│   └── AppointmentBooking.Api.Tests/   xUnit tests
├── frontend/     React frontend (Vite)
├── jenkins/      Custom Jenkins image (adds the Docker CLI + plugins)
├── docker-compose.yml   Runs SQL Server (dev DB) and Jenkins locally
└── Jenkinsfile   CI pipeline definition
```

## Features

- JWT-based authentication (register/login), roles: `Customer` and `Admin`
- Multiple providers, each offering bookable services with a fixed duration
- **Conflict detection**: a provider can't be double-booked for overlapping time slots (the core business rule of this project, covered by the most detailed tests)
- Appointment lifecycle: `Pending → Confirmed → Completed`, or `Cancelled` at any point
- Admins can see every appointment and confirm/complete them; customers see and manage only their own

## Prerequisites

- .NET 8 SDK (`dotnet --version`)
- Node.js 18+ (`node -v`)
- Docker Desktop (`docker --version`) -- used to run SQL Server and Jenkins locally, no need to install either natively

## 1. Start the database

```bash
docker compose up -d sqlserver
```

This starts SQL Server (Azure SQL Edge image, works on Apple Silicon) on `localhost:1433`. Default credentials are in `docker-compose.yml` (`sa` / `YourStrong!Passw0rd`) and already match `backend/AppointmentBooking.Api/appsettings.json` -- change both together if you want different credentials.

## 2. Create the database schema

EF Core Migrations manage the schema (unlike the first project's Hibernate `ddl-auto: update`, this is the more production-realistic approach and worth mentioning in interviews).

```bash
cd backend
dotnet tool install --global dotnet-ef   # one-time, skip if you already have it
dotnet ef migrations add InitialCreate --project AppointmentBooking.Api
dotnet ef database update --project AppointmentBooking.Api
```

## 3. Run the backend

```bash
cd backend
dotnet run --project AppointmentBooking.Api
```

Runs on `http://localhost:5080` by default (see `Properties/launchSettings.json`). Swagger UI is available at `http://localhost:5080/swagger` in development.

## 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Runs on `http://localhost:5173`, proxying `/api/**` to the backend on port 5080.

## 5. Usage

Register an account (defaults to the `Customer` role), then book an appointment by picking a service, a provider, and a date/time. To try the Admin view, promote a user to `Admin` directly in the database:

```sql
UPDATE Users SET Role = 1 WHERE Username = 'your-username';  -- 0 = Customer, 1 = Admin
```

## Testing

```bash
cd backend
dotnet test
```

```bash
cd frontend
npm install
npm test
```

Backend tests use xUnit + Moq + FluentAssertions, with EF Core's InMemory provider standing in for SQL Server so the conflict-detection logic (the interesting part) is tested against real query behavior without needing a live database. Frontend tests use Vitest + React Testing Library.

## CI/CD with Jenkins

```bash
docker compose up -d jenkins
```

First run: `docker compose logs jenkins` to get the initial admin password, then open `http://localhost:8080` and follow the setup wizard (the plugins listed in `jenkins/plugins.txt` are pre-installed, so you can skip the "install suggested plugins" step).

Once set up:

1. **New Item** → Pipeline → name it (e.g. `appointment-booking`)
2. Under **Pipeline**, set "Definition" to **Pipeline script from SCM**, SCM to **Git**, and point it at this repository's URL and the `Jenkinsfile` at the repo root
3. Save, then **Build Now**

The pipeline (`Jenkinsfile`) runs each stage in its own short-lived container (`mcr.microsoft.com/dotnet/sdk:8.0` for the backend, `node:20` for the frontend) via the Docker socket mounted into the Jenkins container -- no need to install .NET or Node inside Jenkins itself.

## API Overview

| Method | Path | Description | Auth |
|---|---|---|---|
| POST | /api/auth/register | Register | No |
| POST | /api/auth/login | Login, returns a JWT | No |
| GET | /api/services | List bookable services | No |
| POST | /api/services | Create a service | Admin |
| GET | /api/providers | List providers | No |
| POST | /api/providers | Create a provider | Admin |
| POST | /api/appointments | Book an appointment (409 if the slot conflicts) | Yes |
| GET | /api/appointments | List your own appointments (or all, for Admins) | Yes |
| PATCH | /api/appointments/{id}/status | Update status | Admin |
| DELETE | /api/appointments/{id} | Cancel (own appointment, or any as Admin) | Yes |

## Ideas for further work

- Available-slots endpoint that computes free windows from a provider's working hours, instead of the client just guessing a time and finding out via a 409
- Email/SMS reminders (background job)
- Per-provider service assignment (currently every provider can perform every service)
- Refresh tokens
- Integration tests with `WebApplicationFactory` against a real (containerized) SQL Server, complementing the InMemory-provider unit tests
