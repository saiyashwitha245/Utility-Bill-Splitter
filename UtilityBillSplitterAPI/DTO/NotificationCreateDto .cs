namespace UtilityBillSplitterAPI.DTO
{
    public class NotificationCreateDto
    {
        public int UserId { get; set; }
        public string Message { get; set; }
        public string RecipientEmail { get; set; }
    }
}
