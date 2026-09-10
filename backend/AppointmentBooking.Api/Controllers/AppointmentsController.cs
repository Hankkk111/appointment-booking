using System.Security.Claims;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing user id claim"));

    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// Books an appointment for the currently authenticated customer.
    /// Conflict checking (double-booking the same provider) happens inside
    /// AppointmentService and results in a 409 Conflict if the slot is taken.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentDto dto)
    {
        var result = await _appointmentService.CreateAppointmentAsync(CurrentUserId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Returns the current user's own appointments, or -- for Admins -- every
    /// appointment in the system, optionally filtered to a single provider/date.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetMine(
        [FromQuery] int? providerId, [FromQuery] DateTime? date)
    {
        if (IsAdmin)
        {
            if (providerId.HasValue)
            {
                return Ok(await _appointmentService.GetAppointmentsForProviderAsync(providerId.Value, date));
            }
            return Ok(await _appointmentService.GetAllAppointmentsAsync());
        }

        return Ok(await _appointmentService.GetAppointmentsForCustomerAsync(CurrentUserId));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(int id, UpdateStatusDto dto)
    {
        var result = await _appointmentService.UpdateStatusAsync(id, dto.Status);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _appointmentService.CancelAppointmentAsync(id, CurrentUserId, IsAdmin);
        return NoContent();
    }
}
