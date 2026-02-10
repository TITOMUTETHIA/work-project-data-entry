using WorkTicketApp.Components;
using WorkTicketApp.Services;
using WorkTicketApp.Authentication;
using WorkTicketApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure database
ConfigureDatabase(builder);

// Configure services
ConfigureServices(builder.Services);

var app = builder.Build();

// Initialize database
await InitializeDatabase(app);

// Configure middleware
ConfigureMiddleware(app);

await app.RunAsync();

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=WorkTicketAppDb;Trusted_Connection=true;";
    
    builder.Services.AddDbContext<WorkTicketContext>((sp, options) =>
    {
        options.UseSqlServer(connectionString);
        // Log pending model changes warnings
        options.ConfigureWarnings(w => 
            w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
}

static void ConfigureServices(IServiceCollection services)
{
    // Blazor components
    services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // HTTP clients
    services.AddHttpClient();

    // Application services
    services.AddScoped<IWorkTicketService, WorkTicketService>();
    services.AddScoped<IUserService, InMemoryUserService>();
    services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

    // Authentication & Authorization
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "WorkTicketAuth";
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        });

    services.AddAuthorizationBuilder();
}

static async Task InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<WorkTicketContext>();
    await db.Database.MigrateAsync();
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

    authGroup.MapPost("/logout", Logout)
        .WithName("Logout")
        .WithSummary("Logout user")
        .RequireAuthorization();
}

async Task Login(HttpContext ctx, InMemoryUserService users, LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var principal = users.ValidateCredentials(creds.Username, creds.Password);
    if (principal is null)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    ctx.Response.StatusCode = StatusCodes.Status200OK;
}

Task Register(IUserService users, LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
        throw new ArgumentException("Username and password are required");

    if (!users.Register(creds.Username, creds.Password))
        throw new InvalidOperationException("User already exists");

    return Task.CompletedTask;
}

async Task Logout(HttpContext ctx)
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.StatusCode = StatusCodes.Status200OK;
}
