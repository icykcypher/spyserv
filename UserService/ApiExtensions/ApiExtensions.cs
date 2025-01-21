using System.Text;
using UserService.Model;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using UserService.Services.JwtProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UserService.Services.Authentication;

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

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy =>
                {
                    policy.AddRequirements(new PermissionRequirement(new RoleEntity[]
                    {
                        new RoleEntity { Id = 1, Name = "Read", Permissions = [] },
                        new RoleEntity { Id = 2, Name = "Create", Permissions = [] },
                        new RoleEntity { Id = 3, Name = "Update", Permissions = [] },
                        new RoleEntity { Id = 4, Name = "Delete", Permissions = [] },
                    }));
                    policy.RequireClaim("Admin", "true");
                });

                options.AddPolicy("UserPolicy", policy =>
                {
                    policy.AddRequirements(new PermissionRequirement(new RoleEntity[]
                    {
                        new RoleEntity { Id = 1, Name = "Read" }
                    }));
                });
            });
        }
    }
}