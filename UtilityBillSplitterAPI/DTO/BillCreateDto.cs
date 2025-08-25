namespace UtilityBillSplitterAPI.DTO
{
    public class BillCreateDto
    {
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string UtilityType { get; set; }
        public int GroupId { get; set; }
        public int PayerId { get; set; }
        public List<int> ParticipantIds { get; set; }
        public string Description { get; internal set; }= string.Empty;
    }
}
