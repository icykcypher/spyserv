using Serilog;
using UserService.Data;
using UserService.Static;
using UserService.ApiExtensions;
using UserService.MappingProfiles;
using Microsoft.EntityFrameworkCore;
using UserService.Services.JwtProvider;
using Microsoft.AspNetCore.CookiePolicy;
using UserService.SyncDataServices.Grpc;
using Microsoft.AspNetCore.DataProtection;
using UserService.Services.PasswordHasher;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using UserService.Services.UserManagmentService;
using UserService.AsyncDataServices;

namespace UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var certPath = "/app/certs/devcert.pfx";
            var certPassword = "myPassword123";

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(@"./app/certs/"));


            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });

                if (File.Exists(certPath))
                {
                    Console.WriteLine($"HTTPS certificate found at {certPath}");
                    options.ListenAnyIP(8081, listenOptions =>
                    {
                        listenOptions.UseHttps(certPath, certPassword);
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }
                else
                {
                    Console.WriteLine($"HTTPS certificate NOT found at {certPath}");
                }
            });
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

            builder.Services.AddGrpc();
            builder.Services.AddGrpcClient<GrpcUserService.GrpcUserServiceClient>(o =>
            {
                o.Address = new Uri(builder.Configuration["DataService"]!);
            });
            builder.Services.AddScoped<IGrpcUserCommunicationService, GrpcUserCommunicationService>();
            builder.Services.AddScoped<GrpcUserCommunicationService>();
            builder.Services.AddScoped<IUserMessageBusSubscriber, UserMessageBusSubscriber>();

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
            app.MapGrpcService<GrpcUserCommunicationService>();

            app.Run();
        }
    }
}