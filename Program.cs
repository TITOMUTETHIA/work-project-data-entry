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

// Configure services
ConfigureServices(builder.Services);

// Add DbContextFactory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

var app = builder.Build();

// Initialize database

// Seed default admin user
try
{
    SeedUserData(app);
    SeedWorkTicketData(app);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An error occurred seeding the DB. Ensure SQL Server is running and connection string is correct.");
}

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
    services.AddScoped<IWorkTicketService, WorkTicketService>();
    services.AddScoped<IUserService, UserService>();
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

static void SeedUserData(WebApplication app)
{
    var config = app.Configuration.GetSection("AdminUser");
    var username = config["Username"];
    var password = config["Password"];

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        app.Logger.LogWarning("Admin user not configured in appsettings.json. Skipping seed.");
        return;
    }

    // Create a scope to resolve Scoped services
    using var scope = app.Services.CreateScope();
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

    // The Register method is idempotent for our use case because it won't
    // add the user if they already exist.
    if (userService.Register(username, password, "Admin"))
    {
        app.Logger.LogInformation("Default admin user '{Username}' created.", username);
    }
}

static void SeedWorkTicketData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // The database is created by migrations, but we can check if it's empty.
    if (context.WorkTickets.Any())
    {
        app.Logger.LogInformation("Work ticket data already exists. Skipping seed.");
        return;
    }

    app.Logger.LogInformation("Seeding work ticket data...");

    var tickets = new List<WorkTicket>
    {
        new() {
            TicketNumber = "TKT-001",
            CostCentre = "CC-MECH",
            Activity = "Routine Maintenance",
            OperatorName = "John Doe",
            NumOperators = 1,
            StartDateTime = "2023-10-26 08:00",
            StartCounter = 1000,
            EndDateTime = "2023-10-26 10:00",
            EndCounter = 1050,
            QuantityIn = 50,
            QuantityOut = 48,
            MaterialUsed = "Oil, Filter",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            CreatedBy = "system"
        },
        new() {
            TicketNumber = "TKT-002",
            CostCentre = "CC-ELEC",
            Activity = "Panel Inspection",
            OperatorName = "Jane Smith",
            NumOperators = 2,
            StartDateTime = "2023-10-27 09:30",
            StartCounter = 500,
            EndDateTime = "2023-10-27 11:00",
            EndCounter = 500,
            QuantityIn = 10,
            QuantityOut = 10,
            MaterialUsed = "Cleaning supplies",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "system"
        }
    };

    context.WorkTickets.AddRange(tickets);
    context.SaveChanges();
    app.Logger.LogInformation("Finished seeding work ticket data.");
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

static IResult Register(IUserService users, LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
    {
        return Results.BadRequest("Username and password are required");
    }

    if (!users.Register(creds.Username, creds.Password))
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
