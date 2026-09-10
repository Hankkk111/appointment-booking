using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new ConflictException("Username is already taken");
        }

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ConflictException("Email is already registered");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid username or password");
        }

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }
}
