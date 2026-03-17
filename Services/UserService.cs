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

    public async Task<bool> RegisterAsync(string username, string password, string role = "User")
    {
        await using var context = await _factory.CreateDbContextAsync();
        if (await context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            _logger.LogWarning("Registration failed: Username '{Username}' already exists.", username);
            return false;
        }

        var user = new User
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        _logger.LogInformation("User '{Username}' registered successfully with role '{Role}'.", username, role);
        return true;
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

    public async Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Use EF.Functions.Like for case-insensitive search, relying on the database's collation.
            // This is generally more performant than client-side ToLower().
            query = query.Where(u => EF.Functions.Like(u.Username, $"%{searchTerm}%") || EF.Functions.Like(u.Role, $"%{searchTerm}%"));
        }

        // Refactor sorting to use a type-safe switch expression instead of reflection.
        // This is faster, more maintainable, and prevents sorting on unintended properties.
        var orderedQuery = (sortBy?.ToLowerInvariant()) switch
        {
            "role" => sortAscending ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
            _ => sortAscending ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username),
        };
        
        // Add a secondary sort for stable ordering.
        query = orderedQuery.ThenBy(u => u.Id);

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto { Username = u.Username, Role = u.Role })
            .ToListAsync();

        return new PagedResult<UserDto> { Items = users, TotalCount = totalCount };
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        var performedBy = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
        await _auditLogService.LogAsync(performedBy, "User Deleted", username, $"User '{username}' was deleted.");


        _logger.LogInformation("User '{Username}' deleted successfully.", username);

        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(string username, string role)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        var oldRole = user.Role;
        user.Role = role;
        await context.SaveChangesAsync();

        var performedBy = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
        await _auditLogService.LogAsync(performedBy, "User Role Changed", username, $"Changed role for user '{username}' from '{oldRole}' to '{role}'.");

        _logger.LogInformation("User '{Username}' role changed successfully.", username);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string username, string newPassword)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await context.SaveChangesAsync();

        var performedBy = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
        await _auditLogService.LogAsync(performedBy, "Password Reset", username, $"Password reset for user '{username}'.");

        return true;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
    {
        // This method is useful for scenarios like populating a dropdown where all users are needed.
        // It leverages the existing paged method by requesting all items on a single "page".
        // Note: For very large user bases, this could be memory-intensive.
        var pagedResult = await GetUsersAsync(1, int.MaxValue, searchTerm, sortBy, sortAscending);
        return pagedResult.Items;
    }

    public Task AddUserAsync(User user) => RegisterAsync(user.Username, user.Password, user.Role);

    /// <summary>
    /// This method validates user credentials and returns the full User object.
    /// It is now efficient and avoids redundant database lookups.
    /// </summary>
    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        return await GetAndValidateUserAsync(username, password);
    }

    public async Task<bool> UpdateProfileAsync(string username, string newUsername)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        user.Username = newUsername;
        await context.SaveChangesAsync();

        var performedBy = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
        await _auditLogService.LogAsync(performedBy, "Profile Updated", username, $"Username changed for user '{username}' to '{newUsername}'.");

        _logger.LogInformation("User '{Username}' username changed successfully.", username);
        return true;
    }
}