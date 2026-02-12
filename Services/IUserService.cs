using System.Security.Claims;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public interface IUserService
{
    bool Register(string username, string password, string role = "User");
    ClaimsPrincipal? ValidateCredentials(string username, string password);
    PagedResult<UserDto> GetUsers(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
    List<UserDto> GetAllUsers(string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
    bool DeleteUser(string username);
    bool UpdateUserRole(string username, string role);
}