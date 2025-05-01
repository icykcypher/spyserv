using Serilog;
using DataService.Data;
using DataService.MappingProfiles;
using Microsoft.EntityFrameworkCore;
using DataService.StorageRepositories;
using DataService.Services.UserServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using DataService.SyncDataServices.Grpc.UserService;
using DataService.Configurations.UsersConfigurations;
using DataService.AsyncDataServices.UserServiceSubscribers;
using DataService.SyncDataServices.Grpc.MonitoringService;

namespace DataService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

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

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Logging.AddSerilog(logger);
            builder.Services.AddSerilog(logger);

            builder.Services.Configure<AuthorizationOptions>(
                builder.Configuration.GetSection("AuthorizationOptions"));

            builder.Services.AddDbContext<UserServiceDbContext>(options =>
                options.UseNpgsql("ConnectionStrings:postgres"));

            builder.Services.AddDbContext<MonitoringServiceDbContext>(options =>
                options.UseNpgsql("ConnectionStrings:postgres"));

            builder.Services.AddGrpc();

            builder.Services.AddAutoMapper(typeof(UserMappingProfile));
            builder.Services.AddScoped<IUserStorageRepository, UserStorageRepository>();
            builder.Services.AddScoped<IUserDatabaseService, UserDatabaseService>();

            builder.Services.AddSingleton<CreateUserSubscriberService>();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<CreateUserSubscriberService>());
            Console.WriteLine("CreateUserSubscriberService registered as HostedService");

            var app = builder.Build();
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    Console.WriteLine("--> Staring Migration");
                    var context = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
                    context.Database.Migrate();

                    var context2 = scope.ServiceProvider.GetRequiredService<MonitoringServiceDbContext>();
                    context2.Database.Migrate();
                }

                Console.WriteLine("--> Migrated to the database");
            }
            catch (Exception e)
            {
                Console.WriteLine($"--> Error occured while trying migrate to the database: {e.Message}");
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCors("AllowAllOrigins");

            app.MapControllers();
            Console.WriteLine("--> Controllers were mapped!");

            app.MapGrpcService<GrpcUserCommunicationService>();
            app.MapGrpcService<GrpcMonitoringService>();
            app.Run();
        }
    }
}