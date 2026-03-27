using System.Security.Claims;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public interface IUserService
    {
        // Core auth methods
        Task<ClaimsPrincipal?> ValidateCredentialsAsync(string username, string password);
        
        Task<User?> ValidateUserAsync(string username, string password);
    }
}