using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace WorkTicketApp.Authentication;

public class ServerAuthenticationStateProvider : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private Task<AuthenticationState> _authenticationStateTask;

    public ServerAuthenticationStateProvider()
    {
        var unauthenticated = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        _authenticationStateTask = Task.FromResult(unauthenticated);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;

    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
        NotifyAuthenticationStateChanged(_authenticationStateTask);
    }
}