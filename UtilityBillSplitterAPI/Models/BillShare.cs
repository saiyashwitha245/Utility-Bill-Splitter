
namespace UtilityBillSplitterAPI.Models
{
    public class BillShare
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public int UserId { get; set; }
        public decimal ShareAmount { get; set; }
        public bool IsPaid { get; set; }

        public Bill Bill { get; set; }
        public User User { get; set; }
    }
}
