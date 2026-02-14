using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task AddUserAsync(User user)
        {
            // Hash the password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return user;
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string username, string password, string role = "User")
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return false;
            }

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public ClaimsPrincipal? ValidateCredentials(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
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
            var query = _context.Users.AsQueryable();

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
            // Re-use the paging logic but take all (or a large number) if needed, or just implement simple list
            // For simplicity, we'll just return the first 1000 to avoid performance issues if the DB grows large
            return GetUsers(1, 1000, searchTerm, sortBy, sortAscending).Items;
        }

        public bool DeleteUser(string username)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return false;
            _context.Users.Remove(user);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateUserRole(string username, string role)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return false;
            user.Role = role;
            _context.SaveChanges();
            return true;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true)
        {
            var query = _context.Users.AsQueryable();

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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserRoleAsync(string username, string role)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            user.Role = role;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            // Note: Ensure User model has PasswordResetToken and PasswordResetTokenExpires properties
            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);
            
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<bool> ResetPasswordAsync(string username, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                return false;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}