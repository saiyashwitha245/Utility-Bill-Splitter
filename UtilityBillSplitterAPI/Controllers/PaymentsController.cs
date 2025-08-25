using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.Data;
using UtilityBillSplitter.Models;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/payments
        [HttpGet]
        public IActionResult GetAllPayments()
        {
            try
            {
                var payments = _context.Payments
                    .Include(p => p.BillShare)
                    .ThenInclude(bs => bs.Bill)
                    .Include(p => p.User)
                    .Select(p => new
                    {
                        id = p.Id,
                        amount = p.AmountPaid,
                        method = p.Method,
                        paidAt = p.PaidOn.ToString("yyyy-MM-dd"),
                        billId = p.BillShare.BillId,
                        userId = p.UserId,
                        bill = new { p.BillShare.Bill.Title, p.BillShare.Bill.Amount },
                        user = new { p.User.Username, p.User.Email }
                    })
                    .ToList();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting payments: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/payments/{id}
        [HttpGet("{id}")]
        public IActionResult GetPayment(int id)
        {
            try
            {
                var payment = _context.Payments
                    .Include(p => p.BillShare)
                    .ThenInclude(bs => bs.Bill)
                    .Include(p => p.User)
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        id = p.Id,
                        amount = p.AmountPaid,
                        method = p.Method,
                        paidAt = p.PaidOn.ToString("yyyy-MM-dd"),
                        billId = p.BillShare.BillId,
                        userId = p.UserId,
                        bill = new { p.BillShare.Bill.Title, p.BillShare.Bill.Amount },
                        user = new { p.User.Username, p.User.Email }
                    })
                    .FirstOrDefault();

                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                return Ok(payment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting payment: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/payments
        [HttpPost]
        public IActionResult CreatePayment([FromBody] UtilityBillSplitter.DTOs.PaymentCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid request payload", errors = ModelState });

                if (dto.AmountPaid <= 0)
                    return BadRequest(new { message = "Amount must be greater than 0" });

                // Find the targeted BillShare
                var billShare = _context.BillShares
                    .Include(bs => bs.Bill)
                    .FirstOrDefault(bs => bs.Id == dto.BillShareId && bs.UserId == dto.UserId);

                if (billShare == null)
                    return BadRequest(new { message = "Invalid billShareId or userId" });

                // Compute already paid amount for this share
                var alreadyPaid = _context.Payments
                    .Where(p => p.BillShareId == dto.BillShareId && p.UserId == dto.UserId)
                    .Sum(p => p.AmountPaid);

                var outstanding = billShare.ShareAmount - alreadyPaid;
                if (outstanding <= 0 || billShare.IsPaid)
                    return BadRequest(new { message = "This user's share is already fully paid." });

                if (dto.AmountPaid > outstanding)
                    return BadRequest(new { message = $"Payment exceeds outstanding amount (₹{outstanding})." });

                // Create payment
                var payment = new Payment
                {
                    AmountPaid = dto.AmountPaid,
                    Method = dto.Method,
                    BillShareId = dto.BillShareId,
                    UserId = dto.UserId,
                    PaidOn = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                // If cumulative paid >= share amount, mark share as paid
                if (alreadyPaid + dto.AmountPaid >= billShare.ShareAmount)
                {
                    billShare.IsPaid = true;
                }

                _context.SaveChanges();

                var response = new
                {
                    id = payment.Id,
                    amount = payment.AmountPaid,
                    method = payment.Method,
                    paidAt = payment.PaidOn.ToString("yyyy-MM-dd"),
                    billId = billShare.BillId,
                    userId = payment.UserId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating payment: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PATCH: api/payments/{id}/mark-paid - Mark bill as paid
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
                        isPaid = s.IsPaid
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // 🔹 GET: api/payments/group/{groupId}
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
                            isPaid = s.IsPaid
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
                status = BillsController.CalculateBillStatus(b.dueDate, b.totalAmount,
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