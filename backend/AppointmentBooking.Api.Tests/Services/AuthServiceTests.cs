using AppointmentBooking.Api.Data;
using AppointmentBooking.Api.Dtos;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AppointmentBooking.Api.Tests.Services;

public class AuthServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (AppDbContext db, Mock<IJwtService> jwtServiceMock, AuthService sut) CreateSut()
    {
        var db = CreateInMemoryDb();
        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("mock-jwt-token");
        var sut = new AuthService(db, jwtServiceMock.Object);
        return (db, jwtServiceMock, sut);
    }

    [Fact]
    public async Task Register_CreatesUser_AndReturnsToken()
    {
        var (db, _, sut) = CreateSut();

        var result = await sut.RegisterAsync(new RegisterRequestDto
        {
            Username = "alice",
            Password = "password123",
            Email = "alice@example.com"
        });

        result.Token.Should().Be("mock-jwt-token");
        result.Username.Should().Be("alice");
        result.Role.Should().Be("Customer");

        var savedUser = await db.Users.SingleAsync(u => u.Username == "alice");
        savedUser.Email.Should().Be("alice@example.com");
        savedUser.PasswordHash.Should().NotBe("password123"); // must be hashed, never stored raw
        BCrypt.Net.BCrypt.Verify("password123", savedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_ThrowsConflict_WhenUsernameAlreadyTaken()
    {
        var (db, _, sut) = CreateSut();
        db.Users.Add(new User { Username = "alice", Email = "existing@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var act = () => sut.RegisterAsync(new RegisterRequestDto
        {
            Username = "alice",
            Password = "password123",
            Email = "new@example.com"
        });

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Username*");
    }

    [Fact]
    public async Task Register_ThrowsConflict_WhenEmailAlreadyRegistered()
    {
        var (db, _, sut) = CreateSut();
        db.Users.Add(new User { Username = "existing", Email = "alice@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var act = () => sut.RegisterAsync(new RegisterRequestDto
        {
            Username = "alice2",
            Password = "password123",
            Email = "alice@example.com"
        });

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Email*");
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        var (db, _, sut) = CreateSut();
        db.Users.Add(new User
        {
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        });
        await db.SaveChangesAsync();

        var result = await sut.LoginAsync(new LoginRequestDto { Username = "alice", Password = "password123" });

        result.Token.Should().Be("mock-jwt-token");
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var (db, _, sut) = CreateSut();
        db.Users.Add(new User
        {
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        });
        await db.SaveChangesAsync();

        var act = () => sut.LoginAsync(new LoginRequestDto { Username = "alice", Password = "wrong-password" });

        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenUsernameDoesNotExist()
    {
        var (_, _, sut) = CreateSut();

        var act = () => sut.LoginAsync(new LoginRequestDto { Username = "ghost", Password = "password123" });

        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }
}
