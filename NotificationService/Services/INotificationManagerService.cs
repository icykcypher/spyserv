using NotificationService.Dto;

namespace NotificationService.Services
{
    public interface INotificationManagerService
    {
        Task<bool> SendMail(MailData Mail_Data);
    }
}