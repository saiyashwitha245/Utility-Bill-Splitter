using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.Models;
using UtilityBillSplitter.Models;
using UtilityBillSplitterAPI.Data; // Update this to your actual DbContext namespace

namespace UtilityBillSplitter.Tests
{
    public class UtilityBillSplitterTests
    {
        private DbContextOptions<AppDbContext> _options;
        private AppDbContext _context;

        [SetUp]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
        }

        [Test]
        public void Should_CreateUserAndVerifyData()
        {
            var user = new User
            {
                Username = "Maya",
                PasswordHash = "secure123",
                Role = "Admin",
                Email = "maya@example.com"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = _context.Users.FirstOrDefault(u => u.Email == "maya@example.com");
            Assert.NotNull(result);
            Assert.AreEqual("Maya", result.Username);
        }

        [Test]
        public void Should_CreateGroupWithMembers()
        {
            var admin = new User { Username = "Admin1", PasswordHash = "pw", Role = "Admin", Email = "admin1@example.com" };
            var member = new User { Username = "User2", PasswordHash = "pw", Role = "Contributor", Email = "user2@example.com" };
            var group = new Group { Name = "Water Bills", CreatedBy = admin };
            var gm1 = new GroupMember { Group = group, User = admin, Role = "Admin" };
            var gm2 = new GroupMember { Group = group, User = member, Role = "Contributor" };

            _context.Users.AddRange(admin, member);
            _context.Groups.Add(group);
            _context.GroupMembers.AddRange(gm1, gm2);
            _context.SaveChanges();

            var savedGroup = _context.Groups.Include(g => g.Members).FirstOrDefault();
            Assert.NotNull(savedGroup);
            Assert.AreEqual(2, savedGroup.Members.Count);
        }

        [Test]
        public void Should_AddBillAndVerifyShares()
        {
            var payer = new User { Username = "Ravi", PasswordHash = "pw", Role = "Admin", Email = "ravi@example.com" };
            var userA = new User { Username = "Asha", PasswordHash = "pw", Role = "Contributor", Email = "asha@example.com" };
            var group = new Group { Name = "Electric Bills", CreatedBy = payer };
            var bill = new Bill { Title = "July Bill", Amount = 900, PaidAmount = 300, DueDate = DateTime.Today.AddDays(10), UtilityType = "Electricity", Group = group, Payer = payer };
            var share1 = new BillShare { Bill = bill, User = payer, ShareAmount = 450, IsPaid = true };
            var share2 = new BillShare { Bill = bill, User = userA, ShareAmount = 450, IsPaid = false };

            _context.Users.AddRange(payer, userA);
            _context.Groups.Add(group);
            _context.Bills.Add(bill);
            _context.BillShares.AddRange(share1, share2);
            _context.SaveChanges();

            var savedBill = _context.Bills.Include(b => b.Shares).FirstOrDefault();
            Assert.NotNull(savedBill);
            Assert.AreEqual(2, savedBill.Shares.Count);
            Assert.AreEqual(900, savedBill.Shares.Sum(s => s.ShareAmount));
        }

        [Test]
        public void Should_RecordPaymentSuccessfully()
        {
            var user = new User { Username = "Sneha", PasswordHash = "pw", Role = "Contributor", Email = "sneha@example.com" };
            var bill = new Bill { Title = "Internet", Amount = 500, DueDate = DateTime.Today.AddDays(5), UtilityType = "WiFi", Group = new Group { Name = "Net Crew", CreatedBy = user }, Payer = user };
            var share = new BillShare { Bill = bill, User = user, ShareAmount = 500, IsPaid = false };
            var payment = new Payment { User = user, BillShare = share, AmountPaid = 500, Method = "UPI" };

            _context.Users.Add(user);
            _context.Bills.Add(bill);
            _context.BillShares.Add(share);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = _context.Payments.Include(p => p.BillShare).FirstOrDefault();
            Assert.NotNull(result);
            Assert.AreEqual(500, result.AmountPaid);
            Assert.AreEqual("UPI", result.Method);
        }

        [Test]
        public void Should_CreateNotificationForUser()
        {
            var user = new User
            {
                Username = "Devi",
                PasswordHash = "pw",
                Role = "Contributor",
                Email = "devi@example.com"
            };

            _context.Users.Add(user);
            _context.SaveChanges(); // ensures user.Id is generated

            var notification = new Notification
            {
                UserId = user.Id,
                Message = "Payment Reminder",
                IsRead = false,
                RecipientEmail = user.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            var result = _context.Notifications
                .Include(n => n.User)
                .FirstOrDefault();

            Assert.NotNull(result);
            Assert.AreEqual("Payment Reminder", result.Message);
            Assert.False(result.IsRead);
        }


        [Test]
        public void Should_HandleEmptyBillListGracefully()
        {
            var group = new Group { Name = "Empty Group", CreatedBy = new User { Username = "Ghost", PasswordHash = "pw", Role = "Admin", Email = "ghost@example.com" } };
            _context.Groups.Add(group);
            _context.SaveChanges();

            var bills = _context.Bills.Where(b => b.GroupId == group.Id).ToList();
            Assert.IsEmpty(bills);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }
    }
}

