using System.ComponentModel.DataAnnotations;

namespace UtilityBillSplitterAPI.Models
{
    public class EmailRequest
    {
        [Key]
        public required string ToEmail { get; set; }
        public string Subject { get; set; }
        public required string Body { get; set; }
    }
}
