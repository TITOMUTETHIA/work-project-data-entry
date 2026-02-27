using WorkTicketApp.Components;
using WorkTicketApp.Services;
using WorkTicketApp.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Endpoints;

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
    services.AddScoped<IWorkTicketService, WorkTicketService>();
    services.AddScoped<IUserService, UserService>();
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
    // Create a scope to resolve scoped services like DbContext and UserService
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var config = app.Configuration.GetSection("AdminUser");
        var username = config["Username"];
        var password = config["Password"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            app.Logger.LogWarning("Admin user not configured in appsettings.json. Skipping seed.");
            return;
        }

        var userService = services.GetRequiredService<IUserService>();

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

        // Seed user1
        if (await userService.RegisterAsync("user1", "user1", "User"))
        {
            app.Logger.LogInformation("User 'user1' created.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating or seeding the database.");
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
    app.MapAuthEndpoints();
}
