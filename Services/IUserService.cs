using System.Security.Claims;

namespace WorkTicketApp.Services
{
    public interface IUserService
    {
        bool Register(string username, string password);
        ClaimsPrincipal? ValidateCredentials(string? username, string? password);
    }
}
