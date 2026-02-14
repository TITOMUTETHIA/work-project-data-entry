using System.Security.Claims;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public interface IUserService
    {
        // Async methods
        Task<bool> RegisterAsync(string username, string password, string role = "User");
        Task<List<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task<User?> ValidateUserAsync(string username, string password);
        Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
        Task<bool> DeleteUserAsync(string username);
        Task<bool> UpdateUserRoleAsync(string username, string role);
        Task<string?> GeneratePasswordResetTokenAsync(string username);
        Task<bool> ResetPasswordAsync(string username, string token, string newPassword);

        // Synchronous methods (kept for backward compatibility or specific auth scenarios)
        ClaimsPrincipal? ValidateCredentials(string username, string password);
        PagedResult<UserDto> GetUsers(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
        List<UserDto> GetAllUsers(string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
        bool DeleteUser(string username);
        bool UpdateUserRole(string username, string role);
    }
}