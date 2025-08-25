using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.DTO;
using UtilityBillSplitterAPI.Models;
using UtilityBillSplitterAPI.Data;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillsController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to calculate bill status - now static
        public static string CalculateBillStatus(DateTime dueDate, decimal totalAmount, decimal totalPaid)
        {
            if (totalPaid >= totalAmount)
                return "Paid";

            if (dueDate < DateTime.UtcNow.Date)
                return "Overdue";

            return "Pending";
        }

        // 🔹 GET: api/bills - Get all bills
        [HttpGet]
        public IActionResult GetAllBills()
        {
            // First, get the raw bill data without any complex projections
            var rawBills = _context.Bills.ToList();

            var bills = rawBills.Select(bill =>
            {
                // Get bill shares for this bill
                var shares = _context.BillShares
                    .Where(s => s.BillId == bill.Id)
                    .ToList();

                // Calculate total paid amount
                var totalPaid = shares.Where(s => s.IsPaid).Sum(s => s.ShareAmount);

                // Calculate status
                string status;
                if (totalPaid >= bill.Amount)
                    status = "Paid";
                else if (bill.DueDate < DateTime.UtcNow.Date)
                    status = "Overdue";
                else
                    status = "Pending";

                // Get participant details
                var participants = shares.Select(s =>
                {
                    var user = _context.Users.Find(s.UserId);
                    return new
                    {
                        id = s.Id,
                        billId = s.BillId,
                        userId = s.UserId,
                        username = user?.Username ?? "",
                        email = user?.Email ?? "",
                        shareAmount = s.ShareAmount,
                        isPaid = s.IsPaid
                    };
                }).ToList();

                return new
                {
                    id = bill.Id,
                    title = bill.Title,
                    totalAmount = bill.Amount,
                    utilityType = bill.UtilityType,
                    dueDate = bill.DueDate.ToString("yyyy-MM-dd"),
                    status = status,
                    description = bill.Description,
                    groupId = bill.GroupId,
                    createdBy = bill.PayerId,
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    participants = participants
                };
            }).ToList();

            return Ok(bills);
        }

        // 🔹 GET: api/bills/{id} - Get single bill
        [HttpGet("{id}")]
        public IActionResult GetBill(int id)
        {
            var billData = _context.Bills
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    totalAmount = b.Amount,
                    utilityType = b.UtilityType,
                    dueDate = b.DueDate,
                    description = b.Description,
                    groupId = b.GroupId,
                    createdBy = b.PayerId,
                    participants = _context.BillShares
                        .Where(s => s.BillId == b.Id)
                        .Select(s => new
                        {
                            id = s.Id,
                            billId = s.BillId,
                            userId = s.UserId,
                            username = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                            email = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                            shareAmount = s.ShareAmount,
                            isPaid = s.IsPaid,
                            paidAmount = (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0),
                            outstanding = s.ShareAmount - (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0)
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (billData == null)
                return NotFound("Bill not found");

            // Calculate status after retrieving from database
            var bill = new
            {
                billData.id,
                billData.title,
                billData.totalAmount,
                billData.utilityType,
                dueDate = billData.dueDate.ToString("yyyy-MM-dd"),
                status = CalculateBillStatus(billData.dueDate, billData.totalAmount,
                    billData.participants.Where(p => p.isPaid).Sum(p => p.shareAmount)),
                billData.description,
                billData.groupId,
                billData.createdBy,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                billData.participants
            };

            return Ok(bill);
        }

        // 🔹 POST: api/bills
        [HttpPost]
        public IActionResult AddBill(BillCreateDto dto)
        {
            var bill = new Bill
            {
                Title = dto.Title,
                Amount = dto.Amount,
                DueDate = dto.DueDate,
                UtilityType = dto.UtilityType,
                Description = dto.Description,
                GroupId = dto.GroupId,
                PayerId = dto.PayerId
            };

            _context.Bills.Add(bill);
            _context.SaveChanges();

            decimal individualShare = dto.Amount / dto.ParticipantIds.Count;

            foreach (var userId in dto.ParticipantIds)
            {
                _context.BillShares.Add(new BillShare
                {
                    BillId = bill.Id,
                    UserId = userId,
                    ShareAmount = individualShare,
                    IsPaid = false
                });
            }

            _context.SaveChanges();

            // 🔄 Return enriched bill response
            var response = new
            {
                id = bill.Id,
                title = bill.Title,
                totalAmount = bill.Amount,
                utilityType = bill.UtilityType,
                dueDate = bill.DueDate.ToString("yyyy-MM-dd"),
                status = "Pending",
                description = bill.Description,
                groupId = bill.GroupId,
                createdBy = bill.PayerId,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                participants = _context.BillShares
                    .Where(s => s.BillId == bill.Id)
                    .Select(s => new
                    {
                        id = s.Id,
                        billId = s.BillId,
                        userId = s.UserId,
                        username = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                        email = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                        shareAmount = s.ShareAmount,
                        isPaid = s.IsPaid,
                        paidAmount = (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0),
                        outstanding = s.ShareAmount - (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0)
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // 🔹 PUT: api/bills/{id} - Update bill
        [HttpPut("{id}")]
        public IActionResult UpdateBill(int id, [FromBody] BillCreateDto dto)
        {
            var bill = _context.Bills.Find(id);
            if (bill == null)
                return NotFound("Bill not found");

            bill.Title = dto.Title;
            bill.Amount = dto.Amount;
            bill.DueDate = dto.DueDate;
            bill.UtilityType = dto.UtilityType;
            bill.Description = dto.Description;

            _context.SaveChanges();

            return Ok(new { message = "Bill updated successfully", billId = id });
        }

        // 🔹 DELETE: api/bills/{id} - Delete bill
        [HttpDelete("{id}")]
        public IActionResult DeleteBill(int id)
        {
            var bill = _context.Bills.Find(id);
            if (bill == null)
                return NotFound("Bill not found");

            // Remove associated bill shares
            var shares = _context.BillShares.Where(s => s.BillId == id);
            _context.BillShares.RemoveRange(shares);

            _context.Bills.Remove(bill);
            _context.SaveChanges();

            return Ok(new { message = "Bill deleted successfully" });
        }

        // 🔹 PATCH: api/bills/{id}/mark-paid - Mark bill as paid
        [HttpPatch("{id}/mark-paid")]
        public IActionResult MarkBillAsPaid(int id)
        {
            var bill = _context.Bills.Find(id);
            if (bill == null)
                return NotFound("Bill not found");

            // Mark all shares as paid
            var shares = _context.BillShares.Where(s => s.BillId == id);
            foreach (var share in shares)
            {
                share.IsPaid = true;
            }

            _context.SaveChanges();

            // Return updated bill
            var response = new
            {
                id = bill.Id,
                title = bill.Title,
                totalAmount = bill.Amount,
                utilityType = bill.UtilityType,
                dueDate = bill.DueDate.ToString("yyyy-MM-dd"),
                status = "Paid",
                description = bill.Description,
                groupId = bill.GroupId,
                createdBy = bill.PayerId,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                participants = _context.BillShares
                    .Where(s => s.BillId == bill.Id)
                    .Select(s => new
                    {
                        id = s.Id,
                        billId = s.BillId,
                        userId = s.UserId,
                        username = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                        email = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                        shareAmount = s.ShareAmount,
                        isPaid = s.IsPaid,
                        paidAmount = (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0),
                        outstanding = s.ShareAmount - (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0)
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // 🔹 GET: api/bills/group/{groupId}
        [HttpGet("group/{groupId}")]
        public IActionResult GetBillsByGroup(int groupId)
        {
            var billsData = _context.Bills
                .Where(b => b.GroupId == groupId)
                .Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    totalAmount = b.Amount,
                    utilityType = b.UtilityType,
                    dueDate = b.DueDate,
                    description = b.Description,
                    groupId = b.GroupId,
                    createdBy = b.PayerId,
                    participants = _context.BillShares
                        .Where(s => s.BillId == b.Id)
                        .Select(s => new
                        {
                            id = s.Id,
                            billId = s.BillId,
                            userId = s.UserId,
                            username = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Username).FirstOrDefault() ?? "",
                            email = _context.Users.Where(u => u.Id == s.UserId).Select(u => u.Email).FirstOrDefault() ?? "",
                            shareAmount = s.ShareAmount,
                            isPaid = s.IsPaid,
                            paidAmount = (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0),
                            outstanding = s.ShareAmount - (_context.Payments.Where(p => p.BillShareId == s.Id && p.UserId == s.UserId).Sum(p => (decimal?)p.AmountPaid) ?? 0)
                        })
                        .ToList()
                })
                .ToList();

            // Calculate status after retrieving from database
            var bills = billsData.Select(b => new
            {
                b.id,
                b.title,
                b.totalAmount,
                b.utilityType,
                dueDate = b.dueDate.ToString("yyyy-MM-dd"),
                status = CalculateBillStatus(b.dueDate, b.totalAmount,
                    b.participants.Where(p => p.isPaid).Sum(p => p.shareAmount)),
                b.description,
                b.groupId,
                b.createdBy,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                b.participants
            }).ToList();

            return Ok(bills);
        }
    }
}
