using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Dtos;

public class ProviderDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Provider name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Bio { get; set; }
}
