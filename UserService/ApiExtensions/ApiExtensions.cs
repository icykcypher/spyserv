using System.Text;
using UserService.Model;
using Microsoft.IdentityModel.Tokens;
using UserService.Services.JwtProvider;
using Microsoft.AspNetCore.Authorization;
using UserService.Services.Authentication;
using UserService.Services.RolePermissionProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace UserService.ApiExtensions
{
    public static class ApiExtensions
    {
        public static void AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new()
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions!.SecretKey))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["homka-lox"];

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddSingleton<RolePermissionProviderService>();

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddAuthorization(async options =>
            {
                var rolePermissions = await GetRolePermissionsFromGrpc(services);

                foreach (var rolePermission in rolePermissions)
                {
                    var roleName = rolePermission.Name;
                    var permissions = rolePermission.Permissions;

                    options.AddPolicy($"{roleName}Policy", policy =>
                    {
                        policy.AddRequirements(new PermissionRequirement(GetRoleEntities(permissions)));
                        if (roleName == "Admin") policy.RequireClaim("Admin", "true");
                    });
                }
            });
        }

        private static async Task<List<RoleEntity>> GetRolePermissionsFromGrpc(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var rolePermissionsProvider = serviceProvider.GetRequiredService<RolePermissionProviderService>();
            var roles = await rolePermissionsProvider.GetRolesAsync();

            return roles.ToList();
        }

        private static List<RoleEntity> GetRoleEntities(ICollection<PermissionEntity> permissions)
            => permissions
            .SelectMany(p => p.Roles ?? Enumerable.Empty<RoleEntity>())
            .ToList();
    }
}