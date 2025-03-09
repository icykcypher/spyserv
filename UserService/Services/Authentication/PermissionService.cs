using UserService.Data;
using UserService.Model;
using Microsoft.EntityFrameworkCore;

namespace UserService.Services.Authentication
{
    public class PermissionService(UserDbContext repository) : IPermissionService
    {
        private readonly UserDbContext _context = repository;

        public async Task<ICollection<RoleEntity>> GetPermissionsAsync(Guid userId) => await _context.Roles.ToArrayAsync();
    }
}