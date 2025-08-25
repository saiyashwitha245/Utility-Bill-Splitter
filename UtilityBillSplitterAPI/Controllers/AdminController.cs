using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UtilityBillSplitterAPI.Data;
using UtilityBillSplitterAPI.DTO;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ✅ Admin Login Endpoint (public)
        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginDto dto)
        {
            var admin = _context.Users.FirstOrDefault(u =>
                u.Username == dto.Username &&
                u.PasswordHash == dto.Password && // 🔒 Replace with hashing in production
                u.Role == "Admin");

            if (admin == null)
                return Unauthorized("Invalid credentials or not an admin.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        // 🔓 All endpoints are now public

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role
            }).ToList();

            return Ok(users);
        }

        [HttpPut("users/{id}/role")]
        public IActionResult UpdateRole(int id, [FromQuery] string role)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound(new { message = "User not found" });

            user.Role = role;
            _context.SaveChanges();
            return Ok(new { message = "Role updated." });
        }

        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound(new { message = "User not found" });

            // Block deletion if user owns resources that would orphan data
            var ownsGroups = _context.Groups.Any(g => g.CreatedById == id);
            var ownsBills = _context.Bills.Any(b => b.PayerId == id);
            if (ownsGroups || ownsBills)
            {
                return BadRequest(new { message = "Cannot delete user because they own groups/bills. Reassign or remove those first." });
            }

            // Remove payments created by the user directly
            var directPayments = _context.Payments.Where(p => p.UserId == id).ToList();
            if (directPayments.Any()) _context.Payments.RemoveRange(directPayments);

            // Remove payments tied to this user's bill shares, then remove those shares
            var userShares = _context.BillShares.Where(bs => bs.UserId == id).ToList();
            if (userShares.Any())
            {
                var shareIds = userShares.Select(s => s.Id).ToList();
                var sharePayments = _context.Payments.Where(p => shareIds.Contains(p.BillShareId)).ToList();
                if (sharePayments.Any()) _context.Payments.RemoveRange(sharePayments);
                _context.BillShares.RemoveRange(userShares);
            }

            // Remove group memberships
            var memberships = _context.GroupMembers.Where(gm => gm.UserId == id).ToList();
            if (memberships.Any()) _context.GroupMembers.RemoveRange(memberships);

            // Remove notifications for this user (cascade configured, but safe to do explicitly)
            var notes = _context.Notifications.Where(n => n.UserId == id).ToList();
            if (notes.Any()) _context.Notifications.RemoveRange(notes);

            _context.SaveChanges();

            // Finally, remove the user
            _context.Users.Remove(user);
            _context.SaveChanges();
            return Ok(new { message = "User deleted." });
        }

        [HttpGet("logs")]
        public IActionResult GetActivityLogs()
        {
            var logs = _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new
                {
                    l.Action,
                    l.Actor,
                    l.Timestamp,
                    l.Description
                })
                .ToList();

            return Ok(logs);
        }
    }
}


