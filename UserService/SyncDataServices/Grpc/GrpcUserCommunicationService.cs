using UserService.Data;
using UserService.Model;
using Google.Protobuf.WellKnownTypes;

namespace UserService.SyncDataServices.Grpc
{
    public class GrpcUserCommunicationService(GrpcUserService.GrpcUserServiceClient grpcClient, UserDbContext db) : IGrpcUserCommunicationService
    {
        private readonly GrpcUserService.GrpcUserServiceClient _grpcClient = grpcClient;
        private readonly UserDbContext _db = db;

        public async Task<List<RoleEntity>> SeedAndGetRolesAsync()
        {
            if (_db.Roles.Any())
                return _db.Roles.ToList();

            var rolesGrpc = await _grpcClient.GetRolesAsync(new Empty());
            var permissionsGrpc = await _grpcClient.GetPermissionsAsync(new Empty());
            var rolePermissionsGrpc = await _grpcClient.GetRolePermissionsAsync(new Empty());

            var roleEntities = rolesGrpc.Roles
                .Select(r => new RoleEntity
                {
                    Id = r.Id,
                    Name = r.Name,
                    Permissions = new List<PermissionEntity>()
                }).ToList();

            var permissionEntities = permissionsGrpc.Permissions
                .Select(p => new PermissionEntity
                {
                    Id = p.Id,
                    Name = p.Name,
                    Roles = new List<RoleEntity>()
                }).ToList();

            var roleDict = roleEntities.ToDictionary(r => r.Id);
            var permissionDict = permissionEntities.ToDictionary(p => p.Id);

            foreach (var rp in rolePermissionsGrpc.RolePermissions)
            {
                if (roleDict.TryGetValue(rp.RoleId, out var role) && permissionDict.TryGetValue(rp.PermissionId, out var permission))
                {
                    role.Permissions!.Add(permission);
                    permission.Roles!.Add(role);
                }
            }

            await _db.Roles.AddRangeAsync(roleEntities);
            await _db.Permissions.AddRangeAsync(permissionEntities);
            await _db.SaveChangesAsync();

            return roleEntities;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var response = await _grpcClient.GetUserByEmailAsync(new UserEmailRequest { Email = email });

            if (response == null || string.IsNullOrEmpty(response.Id))
                return null;

            var userRoles = _db.Roles
                .Where(r => response.Roles.Contains(r.Name))
                .ToList();

            return new User
            {
                Id = Guid.Parse(response.Id),
                Name = response.Name,
                Email = response.Email,
                PasswordHash = response.PasswordHash,
                Roles = userRoles
            };
        }
    }
}