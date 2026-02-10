using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public sealed class InMemoryUserService(WorkTicketContext context) : IUserService
{
    public bool Register(string username, string password)
    {
        try
        {
            // Check if user already exists
            var existingUser = context.Users
                .FirstOrDefault(u => u.Username == username.ToLowerInvariant());
            
            if (existingUser != null)
                return false;

            var user = new User
            {
                Username = username.ToLowerInvariant(),
                Password = password,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ClaimsPrincipal? ValidateCredentials(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || password == null)
            return null;

        var user = context.Users
            .FirstOrDefault(u => u.Username == username.ToLowerInvariant());

        if (user != null && user.Password == password)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, "Cookies");
            return new ClaimsPrincipal(identity);
        }

        return null;
    }
}

