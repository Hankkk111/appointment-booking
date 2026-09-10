using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Services;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(int customerId, CreateAppointmentDto dto);
    Task<List<AppointmentDto>> GetAppointmentsForCustomerAsync(int customerId);
    Task<List<AppointmentDto>> GetAppointmentsForProviderAsync(int providerId, DateTime? date);
    Task<List<AppointmentDto>> GetAllAppointmentsAsync();
    Task<AppointmentDto> UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus);
    Task CancelAppointmentAsync(int appointmentId, int requestingUserId, bool isAdmin);
}
