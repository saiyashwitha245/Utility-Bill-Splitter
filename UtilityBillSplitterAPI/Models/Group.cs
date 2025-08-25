namespace UtilityBillSplitterAPI.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }=string.Empty;
        public int CreatedById { get; set; }

        public string Description {  get; set; }= string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public User CreatedBy { get; set; }
        public List<GroupMember> Members { get; set; }
        public List<Bill> Bills { get; set; } = new();
    }
}
