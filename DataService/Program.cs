using Serilog;
using DataService.Data;
using DataService.MappingProfiles;
using Microsoft.EntityFrameworkCore;
using DataService.StorageRepositories;
using DataService.Services.UserServices;
using DataService.AsyncDataServices.UserServiceSubscribers;

namespace DataService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            builder.Services.AddDbContext<UserServiceDbContext>(options =>
                options.UseNpgsql("ConnectionStrings:postgres"));

            builder.Services.AddHostedService<CreateUserSubscriberService>();
            builder.Services.AddHostedService<UpdateUserSubscriberService>();
            builder.Services.AddAutoMapper(typeof(UserMappingProfile));
            builder.Services.AddScoped<IUserStorageRepository, UserStorageRepository>();
            builder.Services.AddScoped<IUserDatabaseService, UserDatabaseService>();

            var app = builder.Build();
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    Console.WriteLine("--> Staring Migration");
                    var context = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
                    context.Database.Migrate();
                }

                Console.WriteLine("--> Migrated to the database");
            }
            catch (Exception e)
            {
                Console.WriteLine($"--> Error occured while trying migrate to the database: {e.Message}");
                throw;
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCors("AllowAllOrigins");

            app.MapControllers();
            Console.WriteLine("--> Controllers was mapped!");

            app.Run();
        }
    }
}