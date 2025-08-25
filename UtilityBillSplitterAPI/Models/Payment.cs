using UtilityBillSplitterAPI.Models;

namespace UtilityBillSplitter.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BillShareId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaidOn { get; set; } = DateTime.UtcNow;
        public string Method { get; set; } // UPI, Cash, Bank Transfer

        public User User { get; set; }
        public BillShare BillShare { get; set; }
    }
}

