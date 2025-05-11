using Serilog;
using MonitoringService.Services;
using Microsoft.AspNetCore.CookiePolicy;
using MonitoringService.AsyncDataServices;
using MonitoringService.SyncDataServices.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace MonitoringService
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
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
            });
            Log.Logger = new LoggerConfiguration()
             .WriteTo.File(@"/var/log/monitor-srv.log", rollingInterval: RollingInterval.Day)
             .CreateLogger();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.WithOrigins("http://localhost:12345", "http://spyserv.dev", "https://localhost:12345", "https://spyserv.dev")
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      .AllowCredentials());
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

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<IMonitoringDataService, MonitoringDataService>();
            builder.Services.AddScoped<JwtService>();
            builder.Services.AddScoped<MonitoringMessageBusPublisher>();
            builder.Services.AddGrpc();
            builder.Services.AddGrpcClient<MonitoringGrpcService.MonitoringGrpcServiceClient>(o =>
            {
                o.Address = new Uri(builder.Configuration["DataService"]!);
            });

            builder.Services.AddScoped<GrpcMonitoringCommunicationService>();

            var app = builder.Build();

            app.UseCors("AllowAllOrigins");

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

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}