using Serilog;
using UserService.Data;
using UserService.Static;
using UserService.ApiExtensions;
using UserService.MappingProfiles;
using Microsoft.EntityFrameworkCore;
using UserService.StorageRepositories;
using UserService.Services.JwtProvider;
using UserService.Services.UserService;
using Microsoft.AspNetCore.CookiePolicy;
using UserService.Services.PasswordHasher;

namespace UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
             .WriteTo.File(StaticClaims.PathToLogs, rollingInterval: RollingInterval.Day)
             .CreateLogger();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton(Log.Logger);
            builder.Services.AddSingleton<Serilog.Extensions.Hosting.DiagnosticContext>();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));
            builder.Services.Configure<Configurations.AuthorizationOptions>(builder.Configuration.GetSection(nameof(Configurations.AuthorizationOptions)));

            builder.Services.AddDbContext<UserDbContext>(options =>
                options.UseInMemoryDatabase("InMem"));

            builder.Services.AddAutoMapper(typeof(UserMappingProfile));
            builder.Services.AddScoped<IUserStorageRepository, UserStorageRepository>();

            builder.Services.AddScoped<IJwtProvider, JwtProvider>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IUserManagmentService, UserManagmentService>();
            builder.Services.AddApiAuthentication(builder.Configuration);

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCors("AllowAllOrigins");

            app.MapControllers();

            app.Run();
        }
    }
}