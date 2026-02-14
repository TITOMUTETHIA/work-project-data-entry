using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class InMemoryUserService : IUserService
{
    // Use a thread-safe dictionary to store users.
    // This service is registered as a singleton, so this dictionary will persist
    // for the lifetime of the application.
    private readonly ConcurrentDictionary<string, (string PasswordHash, string Role, string? ResetToken, DateTime? ResetTokenExpires)> _users = new();

    public Task<bool> RegisterAsync(string username, string password, string role = "User")
    {
        // Hash the password securely using BCrypt.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // TryAdd is a thread-safe way to add a user if they don't already exist.
        return Task.FromResult(_users.TryAdd(username, (passwordHash, role, null, null)));
    }

    public ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        if (_users.TryGetValue(username, out var userInfo) && BCrypt.Net.BCrypt.Verify(password, userInfo.PasswordHash))
        {
            var claims = new[] 
            { 
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, userInfo.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        return null;
    }

    public PagedResult<UserDto> GetUsers(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
    {
        var query = _users
            .Select(u => new UserDto { Username = u.Key, Role = u.Value.Role });

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        IOrderedEnumerable<UserDto> orderedQuery;
        switch (sortBy?.ToLowerInvariant())
        {
            case "role":
                orderedQuery = sortAscending ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role);
                break;
            case "username":
            default:
                orderedQuery = sortAscending ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username);
                break;
        }

        var pagedUsers = orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // The total count should be based on the filtered query before paging.
        return new PagedResult<UserDto> { Items = pagedUsers, TotalCount = query.Count() };
    }

    public List<UserDto> GetAllUsers(string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
    {
        var query = _users
            .Select(u => new UserDto { Username = u.Key, Role = u.Value.Role });

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        IOrderedEnumerable<UserDto> orderedQuery;
        switch (sortBy?.ToLowerInvariant())
        {
            case "role":
                orderedQuery = sortAscending ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role);
                break;
            case "username":
            default:
                orderedQuery = sortAscending ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username);
                break;
        }

        return orderedQuery.ToList();
    }

    public bool DeleteUser(string username)
    {
        return _users.TryRemove(username, out _);
    }

    public bool UpdateUserRole(string username, string role)
    {
        if (_users.TryGetValue(username, out var userInfo))
        {
            _users[username] = (userInfo.PasswordHash, role, userInfo.ResetToken, userInfo.ResetTokenExpires);
            return true;
        }
        return false;
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        var users = _users.Select((kvp, index) => new User
        {
            Id = index + 1,
            Username = kvp.Key,
            Role = kvp.Value.Role,
            Password = kvp.Value.PasswordHash,
            PasswordResetToken = kvp.Value.ResetToken,
            PasswordResetTokenExpires = kvp.Value.ResetTokenExpires
        }).ToList();
        return Task.FromResult(users);
    }

    public async Task AddUserAsync(User user)
    {
        await RegisterAsync(user.Username, user.Password, user.Role);
    }

    public Task<User?> ValidateUserAsync(string username, string password)
    {
        if (_users.TryGetValue(username, out var userInfo) && BCrypt.Net.BCrypt.Verify(password, userInfo.PasswordHash))
        {
            return Task.FromResult<User?>(new User { Username = username, Role = userInfo.Role, Password = userInfo.PasswordHash });
        }
        return Task.FromResult<User?>(null);
    }

    public Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
    {
        return Task.FromResult(GetUsers(pageNumber, pageSize, searchTerm, sortBy, sortAscending));
    }

    public Task<bool> DeleteUserAsync(string username)
    {
        return Task.FromResult(DeleteUser(username));
    }

    public Task<bool> UpdateUserRoleAsync(string username, string role)
    {
        return Task.FromResult(UpdateUserRole(username, role));
    }

    public Task<string?> GeneratePasswordResetTokenAsync(string username)
    {
        if (_users.TryGetValue(username, out var userInfo))
        {
            var token = Guid.NewGuid().ToString();
            var expires = DateTime.UtcNow.AddHours(1);
            _users[username] = (userInfo.PasswordHash, userInfo.Role, token, expires);
            return Task.FromResult<string?>(token);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> ResetPasswordAsync(string username, string token, string newPassword)
    {
        if (_users.TryGetValue(username, out var userInfo))
        {
            if (userInfo.ResetToken == token && userInfo.ResetTokenExpires > DateTime.UtcNow)
            {
                var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                // Update password and clear token
                _users[username] = (newHash, userInfo.Role, null, null);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}