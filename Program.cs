using WorkTicketApp.Components;
using WorkTicketApp.Services;
using WorkTicketApp.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using WorkTicketApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient for server-side components
builder.Services.AddHttpClient();

// Authentication/Authorization
builder.Services.AddSingleton<IUserService, InMemoryUserService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "WorkTicketAuth";
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal endpoints for auth
app.MapPost("/auth/login", async (HttpContext ctx, IUserService users) =>
{
    var creds = await ctx.Request.ReadFromJsonAsync<LoginDto>();
    if (creds == null) return Results.BadRequest();
    var principal = users.ValidateCredentials(creds.Username, creds.Password);
    if (principal == null) return Results.Unauthorized();
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Ok();
});

app.MapPost("/auth/register", (IUserService users, LoginDto creds) =>
{
    if (creds?.Username == null || creds?.Password == null) return Results.BadRequest();
    var ok = users.Register(creds.Username, creds.Password);
    return ok ? Results.Ok() : Results.Conflict();
});

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

app.Run();
