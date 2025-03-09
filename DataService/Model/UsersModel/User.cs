using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataService.Model.UsersModel
{
    public class User
    {
        [Key]
        public required Guid Id { get; set; }

        [Required]
        [StringLength(20)]
        public required string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(30)]
        public required string Email { get; set; }

        [Required]
        [StringLength(225)]
        [PasswordPropertyText]
        public required string PasswordHash { get; set; }

        public ICollection<RoleEntity> Roles { get; set; } = [];
    }
}