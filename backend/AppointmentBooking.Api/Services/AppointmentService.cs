using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Api.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;

    public AppointmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(int customerId, CreateAppointmentDto dto)
    {
        var service = await _db.Services.FindAsync(dto.ServiceId)
                      ?? throw new NotFoundException($"Service not found with id: {dto.ServiceId}");

        var provider = await _db.Providers.FindAsync(dto.ProviderId)
                       ?? throw new NotFoundException($"Provider not found with id: {dto.ProviderId}");

        if (dto.StartTime < DateTime.UtcNow)
        {
            throw new ConflictException("Cannot book an appointment in the past");
        }

        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        var hasConflict = await HasConflictAsync(dto.ProviderId, dto.StartTime, endTime, excludeAppointmentId: null);
        if (hasConflict)
        {
            throw new ConflictException(
                $"{provider.Name} is already booked for part of that time slot. Please choose a different time.");
        }

        var appointment = new Appointment
        {
            CustomerId = customerId,
            ProviderId = dto.ProviderId,
            ServiceId = dto.ServiceId,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Notes = dto.Notes,
            Status = AppointmentStatus.Pending
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return await ToDtoAsync(appointment.Id);
    }

    /// <summary>
    /// Two time ranges [aStart, aEnd) and [bStart, bEnd) overlap exactly when
    /// aStart &lt; bEnd AND bStart &lt; aEnd. Cancelled appointments don't
    /// block the slot, so they're excluded. excludeAppointmentId lets
    /// UpdateStatus/reschedule flows check against every *other* appointment
    /// without tripping over the one being modified.
    /// </summary>
    private async Task<bool> HasConflictAsync(int providerId, DateTime start, DateTime end, int? excludeAppointmentId)
    {
        var query = _db.Appointments.Where(a =>
            a.ProviderId == providerId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartTime < end &&
            a.EndTime > start);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<AppointmentDto>> GetAppointmentsForCustomerAsync(int customerId)
    {
        var appointments = await _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetAppointmentsForProviderAsync(int providerId, DateTime? date)
    {
        var query = _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .Where(a => a.ProviderId == providerId);

        if (date.HasValue)
        {
            var dayStart = date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
        }

        var appointments = await query.OrderBy(a => a.StartTime).ToListAsync();
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync()
    {
        var appointments = await _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _db.Appointments.FindAsync(appointmentId)
                           ?? throw new NotFoundException($"Appointment not found with id: {appointmentId}");

        appointment.Status = newStatus;
        await _db.SaveChangesAsync();

        return await ToDtoAsync(appointment.Id);
    }

    public async Task CancelAppointmentAsync(int appointmentId, int requestingUserId, bool isAdmin)
    {
        var appointment = await _db.Appointments.FindAsync(appointmentId)
                           ?? throw new NotFoundException($"Appointment not found with id: {appointmentId}");

        if (!isAdmin && appointment.CustomerId != requestingUserId)
        {
            throw new UnauthorizedAppException("You can only cancel your own appointments");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    private async Task<AppointmentDto> ToDtoAsync(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .SingleAsync(a => a.Id == appointmentId);

        return MapToDto(appointment);
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        CustomerUsername = a.Customer?.Username,
        ProviderId = a.ProviderId,
        ProviderName = a.Provider?.Name,
        ServiceId = a.ServiceId,
        ServiceName = a.Service?.Name,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Status = a.Status.ToString(),
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };
}
