using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace WorkTicketApp.Authentication
{
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(user));
        }

        public void NotifyAuthenticationStateChanged() =>
            base.NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity()))));
    }
}
