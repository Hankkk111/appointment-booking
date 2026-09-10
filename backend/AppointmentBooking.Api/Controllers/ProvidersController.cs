using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Api.Controllers;

[ApiController]
[Route("api/providers")]
[Authorize]
public class ProvidersController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProvidersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProviderDto>>> GetAll()
    {
        var providers = await _db.Providers.ToListAsync();
        return Ok(providers.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProviderDto>> Create(ProviderDto dto)
    {
        var provider = new Provider
        {
            Name = dto.Name,
            Bio = dto.Bio
        };

        _db.Providers.Add(provider);
        await _db.SaveChangesAsync();

        return Ok(ToDto(provider));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var provider = await _db.Providers.FindAsync(id)
                       ?? throw new NotFoundException($"Provider not found with id: {id}");

        _db.Providers.Remove(provider);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ProviderDto ToDto(Provider p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Bio = p.Bio
    };
}
