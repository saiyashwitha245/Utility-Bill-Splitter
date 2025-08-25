using System.ComponentModel.DataAnnotations;

namespace UtilityBillSplitterAPI.Models
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } // Admin / Contributor

        public Group Group { get; set; } //For the Navigation proeprty
        public User User { get; set; }
    }
}
