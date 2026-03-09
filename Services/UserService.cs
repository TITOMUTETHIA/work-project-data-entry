using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public UserService(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Users.ToListAsync();
        }

        public async Task AddUserAsync(User user)
        {
            using var context = await _factory.CreateDbContextAsync();
            // Hash the password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return user;
            }
            return null;
        }

        public bool Register(string username, string password, string role = "User")
        {
            using var context = _factory.CreateDbContext();
            if (context.Users.Any(u => u.Username == username))
            {
                return false;
            }

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };

            context.Users.Add(user);
            context.SaveChanges();
            return true;
        }

        public ClaimsPrincipal? ValidateCredentials(string username, string password)
        {
            using var context = _factory.CreateDbContext();
            var user = context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                return new ClaimsPrincipal(identity);
            }
            return null;
        }

        public PagedResult<UserDto> GetUsers(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Username.Contains(searchTerm));
            }

            query = (sortBy?.ToLowerInvariant(), sortAscending) switch
            {
                ("role", true) => query.OrderBy(u => u.Role),
                ("role", false) => query.OrderByDescending(u => u.Role),
                ("username", false) => query.OrderByDescending(u => u.Username),
                _ => query.OrderBy(u => u.Username)
            };

            var totalCount = query.Count();
            var items = query.Skip((pageNumber - 1) * pageSize)
                             .Take(pageSize)
                             .Select(u => new UserDto { Username = u.Username, Role = u.Role })
                             .ToList();

            return new PagedResult<UserDto> { Items = items, TotalCount = totalCount };
        }

        public List<UserDto> GetAllUsers(string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Username.Contains(searchTerm));
            }

            query = (sortBy?.ToLowerInvariant(), sortAscending) switch
            {
                ("role", true) => query.OrderBy(u => u.Role),
                ("role", false) => query.OrderByDescending(u => u.Role),
                ("username", false) => query.OrderByDescending(u => u.Username),
                _ => query.OrderBy(u => u.Username)
            };

            return query.Select(u => new UserDto { Username = u.Username, Role = u.Role }).ToList();
        }

        public bool DeleteUser(string username)
        {
            using var context = _factory.CreateDbContext();
            var user = context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return false;
            context.Users.Remove(user);
            context.SaveChanges();
            return true;
        }

        public bool UpdateUserRole(string username, string role)
        {
            using var context = _factory.CreateDbContext();
            var user = context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return false;
            user.Role = role;
            context.SaveChanges();
            return true;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
        {
            using var context = await _factory.CreateDbContextAsync();
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Username.Contains(searchTerm));
            }

            query = (sortBy?.ToLowerInvariant(), sortAscending) switch
            {
                ("role", true) => query.OrderBy(u => u.Role),
                ("role", false) => query.OrderByDescending(u => u.Role),
                ("username", false) => query.OrderByDescending(u => u.Username),
                _ => query.OrderBy(u => u.Username)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize)
                             .Take(pageSize)
                             .Select(u => new UserDto { Username = u.Username, Role = u.Role })
                             .ToListAsync();

            return new PagedResult<UserDto> { Items = items, TotalCount = totalCount };
        }

        public async Task<bool> DeleteUserAsync(string username)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserRoleAsync(string username, string role)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            user.Role = role;
            await context.SaveChangesAsync();
            return true;
        }
    }
}