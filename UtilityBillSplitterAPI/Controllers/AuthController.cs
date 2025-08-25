using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.Data;
using UtilityBillSplitterAPI.DTO;
using UtilityBillSplitterAPI.Models;
using UtilityBillSplitterAPI.Services;
using UtilityBillSplitter.Services;
using System.ComponentModel.DataAnnotations;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ITokenGenerator _tokenGenerator;

        public AuthController(AppDbContext context, IAuthService authService, ITokenGenerator tokenGenerator)
        {
            _context = context;
            _authService = authService;
            _tokenGenerator = tokenGenerator;
        }

        // 📝 POST: api/auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Invalid registration data." });
            }

            // Check if user already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists." });
            }

            // Hash the password and create new user
            var user = new User
            {
                Username = dto.Name,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password),
                Role = "Member"
            };

            _context.Users.Add(user);
            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "User Registration",
                Actor = user.Username,
                Timestamp = DateTime.UtcNow,
                Description = $"New user registered with email: {user.Email}."
            });
            _context.SaveChanges();

            return Ok(new { message = "Registration successful." });
        }

        // 🔐 POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto? dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid login data.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            // Generate a JWT token for the user
            var token = _tokenGenerator.GenerateToken(user);
            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "User Login",
                Actor = user.Username,
                Timestamp = DateTime.UtcNow,
                Description = $"User {user.Username} logged in."
            });
            _context.SaveChanges();

            return Ok(new
            {
                Message = "Login successful.",
                Token = token,
                UserId = user.Id,
                Role = user.Role
            });
        }

        // Optional: GET user profile by ID
        [HttpGet("{id}")]
        public IActionResult GetProfile(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "ViewProfile",
                Actor = user.Username,
                Timestamp = DateTime.UtcNow,
                Description = $"User {user.Username} viewed their profile."
            });

            _context.SaveChanges();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        // Get Users - for adding to groups
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    email = u.Email,
                    role = u.Role
                })
                .ToList();

            return Ok(users);
        }
    }
}
