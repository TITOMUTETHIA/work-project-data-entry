using System.Security.Claims;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public interface IUserService
    {
        // Core auth methods
        Task<bool> RegisterAsync(string username, string password, string role = "User");
        Task<ClaimsPrincipal?> ValidateCredentialsAsync(string username, string password);

        // User management methods
        Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
        Task<bool> DeleteUserAsync(string username);
        Task<bool> UpdateUserRoleAsync(string username, string role);

        // Utility methods
        Task<List<UserDto>> GetAllUsersAsync(string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
        Task AddUserAsync(User user);
        Task<User?> ValidateUserAsync(string username, string password);
    }
}