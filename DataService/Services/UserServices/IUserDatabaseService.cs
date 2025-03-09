
using DataService.Model.UsersModel;

namespace DataService.Services.UserServices
{
    public interface IUserDatabaseService
    {
        Task<User> Register(RegisterUserDto registerUserDto);
        Task UpdateUser(User message);
    }
}