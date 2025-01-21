using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UserService.Model
{
    public class SignInUserDto
    {
        [Required]
        [EmailAddress]
        [StringLength(30)]
        public required string Email { get; set; }

        [Required]
        [StringLength(20)]
        [PasswordPropertyText]
        public required string Password { get; set; }
    }
}