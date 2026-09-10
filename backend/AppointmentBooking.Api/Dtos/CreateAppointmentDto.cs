using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Dtos;

public class CreateAppointmentDto
{
    [Required(ErrorMessage = "ProviderId is required")]
    public int ProviderId { get; set; }

    [Required(ErrorMessage = "ServiceId is required")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "StartTime is required")]
    public DateTime StartTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
