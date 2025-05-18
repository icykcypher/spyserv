using Grpc.Core;
using MonitoringService.Dto;
using Microsoft.AspNetCore.Mvc;
using MonitoringService.Services;
using System.IdentityModel.Tokens.Jwt;
using MonitoringService.AsyncDataServices;
using MonitoringService.SyncDataServices.Grpc;
using Microsoft.AspNetCore.Cors;

namespace MonitoringService.Controllers
{
    [ApiController]
    [Route("api/m")]
    public class MonitoringController(
        IMonitoringDataService service,
        GrpcMonitoringCommunicationService grpc,
        MonitoringMessageBusPublisher mes,
        JwtService jwt) : ControllerBase
    {
        private readonly IMonitoringDataService _service = service;
        private readonly GrpcMonitoringCommunicationService _grpc = grpc;
        private readonly MonitoringMessageBusPublisher _messageBus = mes;
        private readonly JwtService _jwt = jwt;

        [EnableCors("AllowAllOrigins")]
        [HttpPost("data/{deviceName}")]
        public async Task<IActionResult> PostMonitoringData([FromRoute] string deviceName, [FromBody] Dto.MonitoringData data)
        {
            if (data == null) return BadRequest("Monitoring data is null.");
            var userEmail = data.UserEmail;

            await _service.UpdateDataAsync(userEmail, deviceName, data);

            return Ok();
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("{deviceName}")]
        public async Task<ActionResult<MonitoringData>> GetLatestMonitoringData([FromRoute] string deviceName)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(HttpContext.Request.Cookies["homka-lox"]);

                var emailClaim = jwtToken?.Claims?.FirstOrDefault(c => c.Type.ToLower() == "UserEmail".ToLower());

                if (emailClaim == null)
                    return Unauthorized("Token does not contain a valid 'UserEmail' claim.");

                var userEmail = emailClaim.Value;


                var latestData = await _service.GetLatestAsync(userEmail, deviceName);

                return Ok(latestData);
            }
            catch (ArgumentNullException)
            {
                return NotFound("No monitoring data found.");
            }
            catch (RpcException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ClientAppRegisterDto dto)
        {
            if (!await _grpc.UserExistsAsync(dto.UserEmail))
                return BadRequest("User does not exist.");

            var clientApp = new ClientApp
            {
                UserEmail = dto.UserEmail,
                DeviceName = dto.DeviceName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            };

            clientApp.Id = await _messageBus.SendNewClientAppAsync(clientApp);

            var token = _jwt.GenerateToken(clientApp);

            Response.Cookies.Append("homka-lox2", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });

            return Ok(new { token });
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("apps")]
        public async Task<IActionResult> GetUserApps()
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(HttpContext.Request.Cookies["homka-lox"]);
            var emailClaim = jwtToken?.Claims?.FirstOrDefault(c => c.Type.ToLower() == "userId".ToLower());
            if (emailClaim == null)
                return Unauthorized("Token does not contain a valid 'userId' claim.");
            var userId = emailClaim.Value;
            var apps = await _grpc.GetUserAppsAsync(userId);
            return Ok(apps);
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("statuses/{deviceName}")]
        public async Task<IActionResult> GetAppStatuses([FromRoute] string deviceName)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(HttpContext.Request.Cookies["homka-lox"]);

            var emailClaim = jwtToken?.Claims?.FirstOrDefault(c => c.Type.Equals("UserEmail", StringComparison.OrdinalIgnoreCase));
            if (emailClaim == null)
                return Unauthorized("Token does not contain a valid 'UserEmail' claim.");

            var userEmail = emailClaim.Value;

            try
            {
                var statuses = await _grpc.GetAppStatusesAsync(userEmail, deviceName);
                return Ok(statuses);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, $"gRPC error: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error.");
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("statuses/{deviceName}")]
        public async Task<IActionResult> PostAppStatuses([FromRoute] string deviceName, [FromBody] AppStatusRequest request)
        {
            if (request.Statuses == null || request.Statuses.Count == 0)
            {
                return BadRequest("App statuses data is empty.");
            }

            try
            {
                await _messageBus.PublishAppStatusAsync(request.UserEmail, deviceName, request.Statuses);
                return Ok("App statuses have been successfully submitted.");
            }
            catch (RpcException ex)
            {
                return StatusCode(500, $"gRPC error: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error.");
            }
        }

    }
}