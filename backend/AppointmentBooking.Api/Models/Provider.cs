using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Models;

/// <summary>
/// A staff member / resource that appointments are booked against
/// (e.g. a specific stylist or doctor). Conflict detection is scoped
/// per-provider: two customers can't book the same provider for
/// overlapping time slots.
/// </summary>
public class Provider
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Bio { get; set; }
}
