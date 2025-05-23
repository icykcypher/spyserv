﻿using UserService.Model;

namespace UserService.AsyncDataServices
{
    public interface IUserMessageBusSubscriber
    {
        Task<User> DeleteUserAsync(Guid userId);
        void Dispose();
        Task<Guid> SendNewUserAsync(User user);
    }
}