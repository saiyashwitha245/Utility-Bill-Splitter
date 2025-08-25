using System.ComponentModel.DataAnnotations;

namespace UtilityBillSplitterAPI.DTO
{
   
        public class RegisterDto
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            [Required]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
            public string Password { get; set; }
        }

        public class LoginDto
        {

            [Required]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }
        }
    
}
