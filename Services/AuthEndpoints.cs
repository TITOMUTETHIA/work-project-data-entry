using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WorkTicketApp.Models;
using WorkTicketApp.Services;

namespace WorkTicketApp.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithName("Auth");

        authGroup.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticate user");

        authGroup.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register new user");

        authGroup.MapPost("/logout", (Delegate)Logout)
            .WithName("Logout")
            .WithSummary("Logout user")
            .RequireAuthorization();
        
        return app;
    }

    private static async Task<IResult> Login(HttpContext ctx, IUserService users, [FromForm] LoginDto creds)
    {
        if (string.IsNullOrEmpty(creds.Username) || string.IsNullOrEmpty(creds.Password))
        {
            return Results.Redirect("/account/login?error=MissingCredentials");
        }

        var user = await users.ValidateUserAsync(creds.Username, creds.Password);
        if (user is null)
        {
            return Results.Redirect("/account/login?error=InvalidCredentials");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Results.Redirect("/");
    }

    private static async Task<IResult> Register(IUserService users, [FromForm] LoginDto creds)
    {
        if (string.IsNullOrEmpty(creds.Username) || string.IsNullOrEmpty(creds.Password))
        {
            return Results.BadRequest("Username and password are required");
        }

        if (!await users.RegisterAsync(creds.Username, creds.Password))
        {
            return Results.Conflict("User already exists");
        }

        return Results.Ok();
    }

    private static async Task<IResult> Logout(HttpContext ctx)
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/account/login");
    }
}