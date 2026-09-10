using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
