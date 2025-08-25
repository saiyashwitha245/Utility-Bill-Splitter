//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using UtilityBillSplitterAPI.Data;
//using UtilityBillSplitterAPI.DTO;
//using UtilityBillSplitterAPI.Models;

//namespace UtilityBillSplitter.Controllers
//{
//    [Authorize]
//    [ApiController]
//    [Route("api/[controller]")]
//    public class GroupsController : ControllerBase
//    {
//        private readonly AppDbContext _context;

//        public GroupsController(AppDbContext context)
//        {
//            _context = context;
//        }

//        //  POST - Create a Group
//        [HttpPost]
//        public IActionResult CreateGroup([FromBody] GroupCreateDto model)
//        {
//            Console.WriteLine($"Creating group: {model.Name} by user {model.CreatedById}");

//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            var group = new Group
//            {
//                Name = model.Name,
//                CreatedById = model.CreatedById
//            };

//            _context.Groups.Add(group);
//            _context.SaveChanges();

//            foreach (var memberId in model.MemberIds)
//            {
//                Console.WriteLine($"Adding member {memberId} to group {group.Id}");

//                _context.GroupMembers.Add(new GroupMember
//                {
//                    GroupId = group.Id,
//                    UserId = memberId,
//                    Role = "Contributor"
//                });
//            }

//            // Add creator as Admin
//            Console.WriteLine($"Adding creator {model.CreatedById} as Admin to group {group.Id}");

//            _context.GroupMembers.Add(new GroupMember
//            {
//                GroupId = group.Id,
//                UserId = model.CreatedById,
//                Role = "Admin"
//            });

//            _context.SaveChanges();

//            //  Build response with user info
//            var response = new
//            {
//                group.Id,
//                group.Name,
//                group.CreatedById,
//                CreatedOn = group.CreatedOn,
//                Members = _context.GroupMembers
//                    .Where(m => m.GroupId == group.Id)
//                    .Select(m => new
//                    {
//                        m.Id,
//                        m.GroupId,
//                        m.UserId,
//                        m.Role,
//                        User = _context.Users
//                            .Where(u => u.Id == m.UserId)
//                            .Select(u => new { u.Username, u.Email })
//                            .FirstOrDefault()
//                    })
//                    .ToList(),
//                Bills = new List<object>() // Empty for now
//            };

//            return Ok(response);
//        }

//        //  GET - Group Details
//        [HttpGet("{id}")]
//        public IActionResult GetGroupDetails(int id)
//        {
//            Console.WriteLine($"Fetching group details for group ID: {id}");

//            var group = _context.Groups
//                .Where(g => g.Id == id)
//                .Select(g => new
//                {
//                    g.Id,
//                    g.Name,
//                    g.CreatedById,
//                    g.CreatedOn,
//                    Members = _context.GroupMembers
//                        .Where(m => m.GroupId == g.Id)
//                        .Select(m => new
//                        {
//                            m.Id,
//                            m.GroupId,
//                            m.UserId,
//                            m.Role,
//                            User = _context.Users
//                                .Where(u => u.Id == m.UserId)
//                                .Select(u => new { u.Username, u.Email })
//                                .FirstOrDefault()
//                        })
//                        .ToList(),
//                    Bills = _context.Bills
//                        .Where(b => b.GroupId == g.Id)
//                        .Select(b => new
//                        {
//                            b.Id,
//                            b.Title,
//                            b.Amount,
//                            b.DueDate,
//                            b.PayerId
//                        })
//                        .ToList()
//                })
//                .FirstOrDefault();

//            return group != null ? Ok(group) : NotFound("Group not found");
//        }

//        // DELETE: api/group/{groupId}/member/{memberId}?adminId=123
//        [HttpDelete("{groupId}/member/{memberId}")]
//        public async Task<IActionResult> RemoveMember(int groupId, int memberId, [FromQuery] int adminId)
//        {
//            var group = await _context.Groups.FindAsync(groupId);

//            if (group == null)
//                return NotFound("Group not found.");

//            if (group.CreatedById != adminId)
//                return Forbid("Only the group creator can remove members.");

//            var member = await _context.GroupMembers
//                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == memberId);

//            if (member == null)
//                return NotFound("Member not found in group.");

//            _context.GroupMembers.Remove(member);
//            await _context.SaveChangesAsync();

//            return Ok("Member removed successfully.");
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.Data;
using UtilityBillSplitterAPI.DTO;
using UtilityBillSplitterAPI.Models;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 POST - Create a Group
        [HttpPost]
        public IActionResult CreateGroup([FromBody] GroupCreateDto model)
        {
            Console.WriteLine($"Creating group: {model.Name} by user {model.CreatedById}");

            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request payload", errors = ModelState });

            var group = new Group
            {
                Name = model.Name,
                Description = model.Description ?? string.Empty,
                CreatedById = model.CreatedById
            };

            _context.Groups.Add(group);
            _context.SaveChanges();

            // Add creator as Admin first
            Console.WriteLine($"Adding creator {model.CreatedById} as Admin to group {group.Id}");

            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = model.CreatedById,
                Role = "Admin"
            });

            // Add other members (if any) as Contributors, but skip creator to avoid duplicates
            foreach (var memberId in model.MemberIds ?? new List<int>())
            {
                if (memberId != model.CreatedById) // Avoid duplicate creator entry
                {
                    Console.WriteLine($"Adding member {memberId} to group {group.Id}");

                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = group.Id,
                        UserId = memberId,
                        Role = "Contributor"
                    });
                }
            }

            _context.SaveChanges();

            // 🔄 Build response with user info in frontend format
            var response = new
            {
                id = group.Id,
                name = group.Name,
                description = group.Description,
                createdById = group.CreatedById,
                createdAt = group.CreatedOn.ToString("yyyy-MM-dd"),
                memberCount = _context.GroupMembers.Count(m => m.GroupId == group.Id),
                totalBills = 0,
                activeBills = 0,
                members = _context.GroupMembers
                    .Where(m => m.GroupId == group.Id)
                    .Select(m => new
                    {
                        id = m.Id,
                        groupId = m.GroupId,
                        userId = m.UserId,
                        role = m.Role,
                        username = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                        email = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                        joinedAt = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // 🔹 GET - All Groups for current user
        [HttpGet]
        public IActionResult GetAllGroups()
        {
            Console.WriteLine("Fetching all groups");

            var groups = _context.Groups
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    createdById = g.CreatedById,
                    createdAt = g.CreatedOn.ToString("yyyy-MM-dd"),
                    memberCount = _context.GroupMembers.Count(m => m.GroupId == g.Id),
                    totalBills = _context.Bills.Where(b => b.GroupId == g.Id).Sum(b => (decimal?)b.Amount) ?? 0,
                    activeBills = _context.Bills.Count(b => b.GroupId == g.Id),
                    members = _context.GroupMembers
                        .Where(m => m.GroupId == g.Id)
                        .Select(m => new
                        {
                            id = m.Id,
                            groupId = m.GroupId,
                            userId = m.UserId,
                            role = m.Role,
                            username = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                            email = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                            joinedAt = DateTime.UtcNow.ToString("yyyy-MM-dd")
                        })
                        .ToList()
                })
                .ToList();

            return Ok(groups);
        }

        // 🔹 GET - Group Details
        [HttpGet("{id}")]
        public IActionResult GetGroupDetails(int id)
        {
            Console.WriteLine($"Fetching group details for group ID: {id}");

            var group = _context.Groups
                .Where(g => g.Id == id)
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    createdById = g.CreatedById,
                    createdAt = g.CreatedOn.ToString("yyyy-MM-dd"),
                    memberCount = _context.GroupMembers.Count(m => m.GroupId == g.Id),
                    totalBills = _context.Bills.Where(b => b.GroupId == g.Id).Sum(b => (decimal?)b.Amount) ?? 0,
                    activeBills = _context.Bills.Count(b => b.GroupId == g.Id),
                    members = _context.GroupMembers
                        .Where(m => m.GroupId == g.Id)
                        .Select(m => new
                        {
                            id = m.Id,
                            groupId = m.GroupId,
                            userId = m.UserId,
                            role = m.Role,
                            username = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                            email = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                            joinedAt = DateTime.UtcNow.ToString("yyyy-MM-dd")
                        })
                        .ToList()
                })
                .FirstOrDefault();

            return group != null ? Ok(group) : NotFound("Group not found");
        }

        // 🔹 PUT: api/groups/{id} - Update group
        [HttpPut("{id}")]
        public IActionResult UpdateGroup(int id, [FromBody] GroupUpdateDto dto)
        {
            var group = _context.Groups.Find(id);
            if (group == null)
                return NotFound("Group not found");

            group.Name = dto.Name;
            group.Description = dto.Description;

            _context.SaveChanges();

            return Ok(new { message = "Group updated successfully", groupId = id });
        }

        // 🔹 DELETE: api/groups/{id} - Delete group
        [HttpDelete("{id}")]
        public IActionResult DeleteGroup(int id)
        {
            var group = _context.Groups.Find(id);
            if (group == null)
                return NotFound("Group not found");

            // Remove associated group members
            var members = _context.GroupMembers.Where(m => m.GroupId == id);
            _context.GroupMembers.RemoveRange(members);

            // Remove associated bills and their shares
            var bills = _context.Bills.Where(b => b.GroupId == id);
            foreach (var bill in bills)
            {
                var shares = _context.BillShares.Where(s => s.BillId == bill.Id);
                _context.BillShares.RemoveRange(shares);
            }
            _context.Bills.RemoveRange(bills);

            _context.Groups.Remove(group);
            _context.SaveChanges();

            return Ok(new { message = "Group deleted successfully" });
        }

        // 🔹 POST: api/groups/{groupId}/members - Add member to group
        [HttpPost("{groupId}/members")]
        public IActionResult AddMember(int groupId, [FromBody] AddMemberDto dto)
        {
            var group = _context.Groups.Find(groupId);
            if (group == null)
                return NotFound("Group not found");

            var user = _context.Users.Find(dto.UserId);
            if (user == null)
                return NotFound("User not found");

            // Check if user is already a member
            var existingMember = _context.GroupMembers
                .FirstOrDefault(m => m.GroupId == groupId && m.UserId == dto.UserId);

            if (existingMember != null)
                return BadRequest("User is already a member of this group");

            // Add new member
            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = dto.UserId,
                Role = "Contributor"
            });

            _context.SaveChanges();

            return Ok(new { message = $"User {user.Username} added to group successfully" });
        }

        // DELETE: api/groups/{groupId}/member/{memberId}?adminId=123
        [HttpDelete("{groupId}/member/{memberId}")]
        public async Task<IActionResult> RemoveMember(int groupId, int memberId, [FromQuery] int adminId)
        {
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null)
                return NotFound("Group not found.");

            if (group.CreatedById != adminId)
                return Forbid("Only the group creator can remove members.");

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == memberId);

            if (member == null)
                return NotFound("Member not found in group.");

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok("Member removed successfully.");
        }
    }

    // DTO for adding members
    public class AddMemberDto
    {
        public int UserId { get; set; }
    }
}