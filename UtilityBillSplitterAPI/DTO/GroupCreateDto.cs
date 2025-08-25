namespace UtilityBillSplitterAPI.DTO
{
    public class GroupCreateDto
    {
        public string Name { get; set; }
        public int CreatedById { get; set; }
        public required List<int> MemberIds { get; set; } // IDs of invited members
        public string? Description { get; set; } = string.Empty;
    }
}
