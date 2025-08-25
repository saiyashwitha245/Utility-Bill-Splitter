using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace UtilityBillSplitterAPI.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }


        public decimal PaidAmount { get; set; }
        public DateTime DueDate { get; set; }
        public string UtilityType { get; set; } 
        public int GroupId { get; set; }
        public int PayerId { get; set; }

        public Group Group { get; set; }
        public User Payer { get; set; }
        public List<BillShare> Shares { get; set; }
        public string Description { get; internal set; } = string.Empty;
    }
}
