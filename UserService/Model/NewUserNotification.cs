namespace UserService.Model
{
    public class NewUserNotification
    {
        public string EmailToId { get; set; } = string.Empty;
        public string EmailToName { get; set; } = string.Empty;
        public string EmailSubject { get; set; } = "Welcome to Our Service!";
        public string EmailBody { get; set; } = "Thank you for registering!";
    }
}