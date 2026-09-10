using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentBooking.Api.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }
    public User? Customer { get; set; }

    [Required]
    public int ProviderId { get; set; }
    public Provider? Provider { get; set; }

    [Required]
    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    // Computed at creation time as StartTime + Service.DurationMinutes,
    // and stored so conflict-detection queries don't need to join/compute
    // duration on every check.
    [Required]
    public DateTime EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
