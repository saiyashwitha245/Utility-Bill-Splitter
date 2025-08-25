namespace UtilityBillSplitterAPI.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string Actor { get; set; } // Could be Username, Email, or ID
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}
