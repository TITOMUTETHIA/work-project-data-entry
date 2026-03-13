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
    // Reset the database to ensure seeding runs on a clean slate
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    await SeedUserDataAsync(app);
    await SeedWorkTicketDataAsync(app);
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

static async Task SeedUserDataAsync(WebApplication app)
{
    // Create a scope to resolve Scoped services
    using var scope = app.Services.CreateScope();
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

    var config = app.Configuration.GetSection("AdminUser");
    var username = config["Username"];
    var password = config["Password"];

    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
    {
        // The Register method is idempotent for our use case because it won't
        // add the user if they already exist.
        if (await userService.RegisterAsync(username, password, "Admin"))
        {
            app.Logger.LogInformation("Default admin user '{Username}' created.", username);
        }
    }

    // Seed 5 standard operators
    for (int i = 1; i <= 5; i++)
    {
        await userService.RegisterAsync($"Operator{i}", "Password123!", "User");
    }
}

static async Task SeedWorkTicketDataAsync(WebApplication app)
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

    var tickets = new List<WorkTicket>();
    var random = new Random();
    var costCentres = new[] { "CC-MECH", "CC-ELEC", "CC-CIVIL", "CC-ADMIN" };
    var activities = new[] { "Routine Maintenance", "Panel Inspection", "Repair", "Cleaning", "Logistics" };

    for (int i = 1; i <= 5; i++)
    {
        var operatorName = $"Operator{i}";
        for (int j = 1; j <= 20; j++)
        {
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)).Date.AddHours(random.Next(7, 15));
            tickets.Add(new WorkTicket {
                TicketNumber = $"TKT-{i}-{j:000}",
                CostCentre = costCentres[random.Next(costCentres.Length)],
                Activity = activities[random.Next(activities.Length)],
                OperatorName = operatorName,
                NumOperators = random.Next(1, 4),
                StartCounter = random.Next(1000, 5000),
                EndCounter = random.Next(5001, 9000),
                QuantityIn = random.Next(10, 100),
                QuantityOut = random.Next(10, 100),
                MaterialUsed = "Consumables",
                DT = createdAt.ToString("o"),
                CreatedBy = "system",
                CreatedAt = createdAt
            });
        }
    }

    context.WorkTickets.AddRange(tickets);
    await context.SaveChangesAsync();
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

    // API endpoints for WorkTickets
    var ticketsGroup = app.MapGroup("/api/tickets")
        .WithName("WorkTickets")
        .RequireAuthorization(); // Secure all ticket endpoints

    ticketsGroup.MapGet("/", async (IWorkTicketService ticketService, int pageNumber = 1, int pageSize = 10) =>
    {
        return Results.Ok(await ticketService.GetWorkTicketsAsync(pageNumber, pageSize));
    });

    ticketsGroup.MapGet("/{id:int}", async (IWorkTicketService ticketService, int id) =>
    {
        var ticket = await ticketService.GetTicketByIdAsync(id);
        return ticket is not null ? Results.Ok(ticket) : Results.NotFound();
    });
}

static async Task<IResult> Login(HttpContext ctx, IUserService users, [FromForm] LoginDto creds)
{
    if (creds?.Username == null || creds?.Password == null)
    {
        return Results.Redirect("/account/login?error=MissingCredentials");
    }

    var principal = await users.ValidateCredentialsAsync(creds.Username, creds.Password);
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
