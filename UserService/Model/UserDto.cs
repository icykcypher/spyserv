using System.ComponentModel;
using UserService.CustomValidators;
using System.ComponentModel.DataAnnotations;

namespace UserService.Model
{
    public class UserDto
    {
        [Key]
        public required Guid Id { get; set; }

        [Required]
        [StringLength(20)]
        public required string Name { get; set; }

        [Age]
        [Required]
        public required DateTime BirthdayDate { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(20)]
        public required string Email { get; set; }

        [Required]
        [StringLength(20)]
        [PasswordPropertyText]
        public required string PasswordHash { get; set; }
    }
}