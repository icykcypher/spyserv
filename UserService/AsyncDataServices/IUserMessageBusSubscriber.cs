using UserService.Model;

namespace UserService.AsyncDataServices
{
    public interface IUserMessageBusSubscriber
    {
        void Dispose();
        Task<Guid> SendNewUserAsync(User user);
    }
}