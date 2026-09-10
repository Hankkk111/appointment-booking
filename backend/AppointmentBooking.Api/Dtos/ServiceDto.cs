using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Dtos;

public class ServiceDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Service name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes")]
    public int DurationMinutes { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative")]
    public decimal Price { get; set; }
}
