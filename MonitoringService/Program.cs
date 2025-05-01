using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MonitoringService.Services;
using Serilog;

namespace MonitoringService
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
             .WriteTo.File(@"/var/log/monitor-srv.log", rollingInterval: RollingInterval.Day)
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

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<IMonitoringDataService, MonitoringDataService>();
            builder.Services.AddScoped<JwtService>();

            builder.Services.AddGrpc();
            builder.Services.AddGrpcClient<MonitoringGrpcService.MonitoringGrpcServiceClient>(o =>
            {
                o.Address = new Uri(builder.Configuration["DataService"]!);
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.Always
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}