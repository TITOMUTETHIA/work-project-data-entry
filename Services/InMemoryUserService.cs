using System.Collections.Concurrent;
using System.Security.Claims;

namespace WorkTicketApp.Services
{
    public class InMemoryUserService : IUserService
    {
        private readonly ConcurrentDictionary<string, string> _users = new();

        public bool Register(string username, string password)
        {
            return _users.TryAdd(username.ToLowerInvariant(), password);
        }

        public ClaimsPrincipal? ValidateCredentials(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || password == null) return null;
            if (_users.TryGetValue(username.ToLowerInvariant(), out var pw) && pw == password)
            {
                var claims = new[] { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, "Cookies");
                return new ClaimsPrincipal(identity);
            }

            return null;
        }
    }
}
