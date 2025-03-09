using Microsoft.AspNetCore.Mvc;
using NotificationService.Dto;
using NotificationService.Services;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController(ILogger<NotificationController> logger, INotificationManagerService notificationService) : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger = logger;
        private readonly INotificationManagerService _notificationService = notificationService;

        [HttpPost]
        public IActionResult SendNotification([FromBody] MailData mailData)
        {
            try
            {
                _notificationService.SendMail(mailData);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occured at: {nameof(NotificationController)}. Exception message: {e.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}