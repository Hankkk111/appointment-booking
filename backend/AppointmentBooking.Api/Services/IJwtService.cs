using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
