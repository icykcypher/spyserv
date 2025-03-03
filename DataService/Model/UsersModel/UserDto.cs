using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataService.Model.UsersModel
{
    public class UserDto
    {
        [Required]
        [StringLength(20)]
        public required string Name { get; set; }

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