using Grpc.Core;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf.WellKnownTypes;

namespace DataService.SyncDataServices.Grpc.UserService
{
    public class GrpcUserCommunicationService(UserServiceDbContext repository) : GrpcUserService.GrpcUserServiceBase
    {
        private readonly UserServiceDbContext _repository = repository;

        public override Task<RolePermissionList> GetRolePermissions(Empty request, ServerCallContext context)
        {
            var list = new RolePermissionList();
            
            _repository.RolePermissionEntity.ToList().ForEach(x =>
            {
                list.RolePermissions.Add(new RolePermissionGrpcEntity
                {
                    RoleId = x.RoleId,
                    PermissionId = x.PermissionId
                });
            });

            return Task.FromResult(list);
        }

        public override Task<PermissionList> GetPermissions(Empty request, ServerCallContext context)
        {
            var list = new PermissionList();

            _repository.PermissionEntity.ToList().ForEach(x =>
            {
                list.Permissions.Add(new PermissionGrpcEntity
                {
                    Id = x.Id,
                    Name = x.Name
                });
            });

            return Task.FromResult(list);
        }

        public override Task<RoleList> GetRoles(Empty request, ServerCallContext context)
        {
            var list = new RoleList();

            _repository.Roles.ToList().ForEach(x =>
            {
                list.Roles.Add(new RoleGrpcEntity
                {
                    Id = x.Id,
                    Name = x.Name
                });
            });

            return Task.FromResult(list);
        }

        public override Task<RoleGrpcEntity> GetRoleById(RoleRequest request, ServerCallContext context)
        {
            var role = new RoleGrpcEntity
            {
                Id = request.Id,
                Name = "Admin"
            };
            return Task.FromResult(role);
        }

        public override async Task<UserResponse> GetUserByEmail(UserEmailRequest request, ServerCallContext context)
        {
            var user = await _repository.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
            {
                return new UserResponse { Id = Guid.Empty.ToString() };
            }

            return new UserResponse
            {
                Id = user.Id.ToString(),
                PasswordHash = user.PasswordHash,
                Name = user.Name,
                Email = user.Email,
                Roles = { user.Roles.Select(r => r.Name) }
            };
        }
    }
}