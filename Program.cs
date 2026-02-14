using WorkTicketApp.Components;
using WorkTicketApp.Services;
using WorkTicketApp.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using WorkTicketApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure services
ConfigureServices(builder.Services);

var app = builder.Build();

// Initialize database

// Seed default admin user
await SeedUserData(app);

// Configure middleware
ConfigureMiddleware(app);

await app.RunAsync();

static void ConfigureServices(IServiceCollection services)
{
    // Blazor components
    services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // HTTP clients
    services.AddHttpClient();

    // Application services
    services.AddSingleton<IWorkTicketService, InMemoryWorkTicketService>();
    services.AddSingleton<IUserService, InMemoryUserService>();
    services.AddTransient<IEmailService, EmailService>();
    services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
    services.AddHttpContextAccessor();

    // Authentication & Authorization
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "WorkTicketAuth";
            options.LoginPath = "/account/login";
            options.LogoutPath = "/logout";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        });

    services.AddAuthorizationBuilder();
}

static async Task SeedUserData(WebApplication app)
{
    var config = app.Configuration.GetSection("AdminUser");
    var username = config["Username"];
    var password = config["Password"];

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        app.Logger.LogWarning("Admin user not configured in appsettings.json. Skipping seed.");
        return;
    }

    // Since IUserService is a singleton, we can resolve it directly from the root provider.
    var userService = app.Services.GetRequiredService<IUserService>();

    // The Register method is idempotent for our use case because it won't
    // add the user if they already exist.
    if (await userService.RegisterAsync(username, password, "Admin"))
    {
        app.Logger.LogInformation("Default admin user '{Username}' created.", username);
    }

    // Seed a standard user if configured
    var stdConfig = app.Configuration.GetSection("StandardUser");
    var stdUsername = stdConfig["Username"];
    var stdPassword = stdConfig["Password"];

    if (!string.IsNullOrEmpty(stdUsername) && !string.IsNullOrEmpty(stdPassword))
    {
        if (await userService.RegisterAsync(stdUsername, stdPassword, "User"))
        {
            app.Logger.LogInformation("Default standard user '{Username}' created.", stdUsername);
        }
    }
}

static void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
    }
    else
    {
        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection()
        .UseStaticFiles()
        .UseAntiforgery()
        .UseAuthentication()
        .UseAuthorization();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Auth endpoints
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
}

static async Task<IResult> Login(HttpContext ctx, IUserService users, [FromForm] LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
    {
        return Results.Redirect("/account/login?error=MissingCredentials");
    }

    var principal = users.ValidateCredentials(creds.Username, creds.Password);
    if (principal is null)
    {
        return Results.Redirect("/account/login?error=InvalidCredentials");
    }

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect("/");
}

static async Task<IResult> Register(IUserService users, LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
    {
        return Results.BadRequest("Username and password are required");
    }

    if (!await users.RegisterAsync(creds.Username, creds.Password))
    {
        return Results.Conflict("User already exists");
    }

    return Results.Ok();
}

static async Task<IResult> Logout(HttpContext ctx)
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/account/login");
}
