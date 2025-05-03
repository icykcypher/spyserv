using MonitoringService.Dto;
using Microsoft.AspNetCore.Mvc;
using MonitoringService.Services;
using MonitoringService.AsyncDataServices;
using MonitoringService.SyncDataServices.Grpc;

namespace MonitoringService.Controllers
{
    [ApiController]
    [Route("api/m")]
    public class MonitoringController(IMonitoringDataService service, GrpcMonitoringCommunicationService grpc, ClientAppMessageBusPublisher mes, JwtService jwt) : ControllerBase
    {
        private readonly IMonitoringDataService _service = service;
        private readonly GrpcMonitoringCommunicationService _grpc = grpc;
        private readonly ClientAppMessageBusPublisher _messageBus = mes;
        private readonly JwtService _jwt = jwt;


        [HttpPost("data")]
        public async Task<IActionResult> PostMonitoringData([FromBody] MonitoringData data)
        {
            if (data == null)
                return BadRequest("Monitoring data is null.");

            await _service.UpdateDataAsync(data);
            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<MonitoringData>> GetLatestMonitoringData()
        {
            var latestData = await _service.GetLatestAsync();
            if (latestData == null)
                return NotFound("No monitoring data found.");

            return Ok(latestData);
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

            return Ok(new { token });
        }
    }
}