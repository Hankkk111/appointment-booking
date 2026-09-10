using System.ComponentModel.DataAnnotations;
using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Dtos;

public class UpdateStatusDto
{
    [Required(ErrorMessage = "Status is required")]
    public AppointmentStatus Status { get; set; }
}
