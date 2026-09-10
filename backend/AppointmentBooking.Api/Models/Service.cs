using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentBooking.Api.Models;

/// <summary>
/// A bookable service offering, e.g. "Haircut" or "General Check-up".
/// DurationMinutes drives the appointment's end time and therefore the
/// conflict-detection logic in AppointmentService.
/// </summary>
public class Service
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public int DurationMinutes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
}
