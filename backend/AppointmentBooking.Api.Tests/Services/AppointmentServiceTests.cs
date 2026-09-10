using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppointmentBooking.Api.Tests.Services;

/// <summary>
/// Tests for the double-booking / conflict-detection logic in
/// AppointmentService -- the core business rule of this system. Each test
/// gets a fresh EF Core InMemory database (named per test run) so tests
/// never interfere with each other.
/// </summary>
public class AppointmentServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Provider provider, Service service, User customer)> SeedAsync()
    {
        var db = CreateInMemoryDb();

        var provider = new Provider { Name = "Jane Stylist" };
        var service = new Service { Name = "Haircut", DurationMinutes = 60, Price = 45m };
        var customer = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash" };

        db.Providers.Add(provider);
        db.Services.Add(service);
        db.Users.Add(customer);
        await db.SaveChangesAsync();

        return (db, provider, service, customer);
    }

    [Fact]
    public async Task CreateAppointment_Succeeds_WhenNoExistingAppointments()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var result = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        result.Status.Should().Be("Pending");
        result.ProviderId.Should().Be(provider.Id);
    }

    [Fact]
    public async Task CreateAppointment_ThrowsConflict_WhenTimeSlotFullyOverlapsExisting()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10); // 10:00 next day

        await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start // books 10:00-11:00
        });

        var act = () => sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start // same exact slot again
        });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAppointment_ThrowsConflict_WhenTimeSlotPartiallyOverlapsExisting()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Books 10:00-11:00
        await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        // Tries to book 10:30-11:30 -- overlaps the last 30 minutes of the first booking
        var act = () => sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start.AddMinutes(30)
        });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAppointment_Succeeds_WhenBackToBackWithNoOverlap()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Books 10:00-11:00
        await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        // Books 11:00-12:00 -- starts exactly when the first one ends, should be allowed
        var result = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start.AddHours(1)
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAppointment_Succeeds_WhenSameTimeSlotButDifferentProvider()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var otherProvider = new Provider { Name = "Bob Stylist" };
        db.Providers.Add(otherProvider);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        // Same time slot, but a different provider -- no conflict
        var result = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = otherProvider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        result.ProviderId.Should().Be(otherProvider.Id);
    }

    [Fact]
    public async Task CreateAppointment_IgnoresCancelledAppointments_WhenCheckingConflict()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        var first = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        await sut.CancelAppointmentAsync(first.Id, customer.Id, isAdmin: false);

        // The original slot is now free because the booking was cancelled
        var result = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = start
        });

        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateAppointment_ThrowsNotFound_WhenServiceDoesNotExist()
    {
        var (db, provider, _, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var act = () => sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = 9999,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAppointment_ThrowsNotFound_WhenProviderDoesNotExist()
    {
        var (db, _, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var act = () => sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = 9999,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAppointment_ThrowsConflict_WhenBookingInThePast()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var act = () => sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(-1)
        });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CancelAppointment_Succeeds_ForOwnAppointment()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var appointment = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        await sut.CancelAppointmentAsync(appointment.Id, customer.Id, isAdmin: false);

        var appointments = await sut.GetAppointmentsForCustomerAsync(customer.Id);
        appointments.Single().Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelAppointment_ThrowsUnauthorized_WhenCancellingSomeoneElsesAppointment()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var otherUser = new User { Username = "mallory", Email = "mallory@example.com", PasswordHash = "hash" };
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db);
        var appointment = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        var act = () => sut.CancelAppointmentAsync(appointment.Id, otherUser.Id, isAdmin: false);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task CancelAppointment_Succeeds_ForAdmin_EvenIfNotTheOwner()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var admin = new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var sut = new AppointmentService(db);
        var appointment = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        var act = () => sut.CancelAppointmentAsync(appointment.Id, admin.Id, isAdmin: true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateStatus_ChangesAppointmentStatus()
    {
        var (db, provider, service, customer) = await SeedAsync();
        var sut = new AppointmentService(db);

        var appointment = await sut.CreateAppointmentAsync(customer.Id, new CreateAppointmentDto
        {
            ProviderId = provider.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1)
        });

        var updated = await sut.UpdateStatusAsync(appointment.Id, AppointmentStatus.Confirmed);

        updated.Status.Should().Be("Confirmed");
    }
}
