using MimeKit;
using MailKit.Net.Smtp;
using NotificationService.Dto;
using Microsoft.Extensions.Options;

namespace NotificationService.Services
{
    public class NotificationManagerService(IOptions<MailSettings> options) : INotificationManagerService
    {
        private readonly MailSettings _mailSettings = options.Value;

        public async Task<bool> SendMail(MailData Mail_Data)
        {
            try
            {
                var emailMessage = new MimeMessage();
                var emailFrom = new MailboxAddress(_mailSettings.Name, _mailSettings.EmailId);

                emailMessage.From.Add(emailFrom);

                var emailTo = new MailboxAddress(Mail_Data.EmailToName, Mail_Data.EmailToId);

                emailMessage.To.Add(emailTo);
                emailMessage.Subject = Mail_Data.EmailSubject;

                var emailBodyBuilder = new BodyBuilder();
                emailBodyBuilder.TextBody = Mail_Data.EmailBody;
                emailMessage.Body = emailBodyBuilder.ToMessageBody();

                var mailClient = new SmtpClient();

                await mailClient.ConnectAsync(_mailSettings.Host, _mailSettings.Port, _mailSettings.UseSSL);
                await mailClient.AuthenticateAsync(_mailSettings.EmailId, _mailSettings.Password);
                await mailClient.SendAsync(emailMessage);
                await mailClient.DisconnectAsync(true);

                mailClient.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Error occured while sending a notification: {ex.Message}");
                return false;
            }
        }
    }
}