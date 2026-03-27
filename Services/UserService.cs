using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class UserService : IUserService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<UserService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(IDbContextFactory<ApplicationDbContext> factory, ILogger<UserService> logger, IAuditLogService auditLogService, IHttpContextAccessor httpContextAccessor)
    {
        _factory = factory;
        _logger = logger;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task<User?> GetAndValidateUserAsync(string username, string password)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            _logger.LogWarning("Login failed for user '{Username}'. Invalid credentials.", username);
            return null;
        }
        return user;
    }

    public async Task<ClaimsPrincipal?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetAndValidateUserAsync(username, password);
        if (user == null) return null;
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User '{Username}' logged in successfully.", username);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// This method validates user credentials and returns the full User object.
    /// It is now efficient and avoids redundant database lookups.
    /// </summary>
    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        return await GetAndValidateUserAsync(username, password);
    }
}