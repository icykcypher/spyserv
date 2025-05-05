using Grpc.Core;
using MonitoringService.Dto;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MonitoringService.Services;
using System.IdentityModel.Tokens.Jwt;
using MonitoringService.AsyncDataServices;
using MonitoringService.SyncDataServices.Grpc;

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

        [HttpPost("data/{deviceName}")]
        public async Task<IActionResult> PostMonitoringData([FromRoute] string deviceName, [FromBody] Dto.MonitoringData data)
        {
            if (data == null) return BadRequest("Monitoring data is null.");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(HttpContext.Request.Cookies["homka-lox"]);

            var emailClaim = jwtToken?.Claims?.FirstOrDefault(c => c.Type.ToLower() == "UserEmail".ToLower());

            if (emailClaim == null)
                return Unauthorized("Token does not contain a valid 'UserEmail' claim.");

            var userEmail = emailClaim.Value;

            await _service.UpdateDataAsync(userEmail, deviceName, data);

            return Ok();
        }

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

            Response.Cookies.Append("homka-lox", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });

            return Ok(new { token });
        }
    }
}