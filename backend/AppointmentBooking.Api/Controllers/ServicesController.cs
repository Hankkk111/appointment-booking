using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceDto>>> GetAll()
    {
        var services = await _db.Services.ToListAsync();
        return Ok(services.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceDto>> Create(ServiceDto dto)
    {
        var service = new Service
        {
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price
        };

        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        return Ok(ToDto(service));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _db.Services.FindAsync(id)
                      ?? throw new NotFoundException($"Service not found with id: {id}");

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ServiceDto ToDto(Service s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price
    };
}
