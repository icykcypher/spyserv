using Serilog;
using UserService.Data;
using UserService.Static;
using UserService.ApiExtensions;
using UserService.MappingProfiles;
using Microsoft.EntityFrameworkCore;
using UserService.AsyncDataServices;
using UserService.Services.JwtProvider;
using UserService.SyncDataServices.Grpc;
using Microsoft.AspNetCore.DataProtection;
using UserService.Services.PasswordHasher;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using UserService.Services.UserManagmentService;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.CookiePolicy;

namespace UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //var certPath = "/app/devcert.pfx";
            //var certPassword = "myPassword123";
            //X509Certificate2 certificate = new X509Certificate2();
            //if (!File.Exists(certPath))
            //{
            //    Console.WriteLine("Certificate file not found at " + certPath);
            //}
            //else
            //{
            //    certificate = new X509Certificate2(certPath, certPassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
            //}


            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080, listenOptions =>
                {
                    //listenOptions.UseHttps(certificate);
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
            });

            Log.Logger = new LoggerConfiguration()
             .WriteTo.File(StaticClaims.PathToLogs, rollingInterval: RollingInterval.Day)
             .CreateLogger();

            builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowLocalhost", policy =>
                    {
                        policy.WithOrigins("http://localhost:12345", "http://spyserv.dev")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
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

            app.UseCors("AllowLocalhost");

            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.SameAsRequest
            });

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();
            app.MapGrpcService<GrpcUserCommunicationService>();

            app.Run();
        }
    }
}