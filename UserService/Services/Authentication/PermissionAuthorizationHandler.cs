using Microsoft.AspNetCore.Authorization;

namespace UserService.Services.Authentication
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _factory;

        public PermissionAuthorizationHandler(IServiceScopeFactory factory)
        {
            this._factory = factory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userId = context.User.Claims.FirstOrDefault(x => x.Type == "userId");

            if (userId is null || !Guid.TryParse(userId.Value, out var id)) return;

            using (var scope = _factory.CreateScope())
            {
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

                var permissions = await permissionService.GetPermissionsAsync(id);

                if (permissions.Intersect(requirement.Permissions).All(x => true))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}