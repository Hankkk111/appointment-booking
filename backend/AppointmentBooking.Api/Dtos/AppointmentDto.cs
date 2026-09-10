namespace AppointmentBooking.Api.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerUsername { get; set; }
    public int ProviderId { get; set; }
    public string? ProviderName { get; set; }
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
