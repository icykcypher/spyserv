
namespace DataService.AsyncDataServices.UserServiceSubscribers
{
    public class CreateUserSubscriberService : BackgroundService
    {
        public CreateUserSubscriberService(IConfiguration configuration)
        {
            
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}