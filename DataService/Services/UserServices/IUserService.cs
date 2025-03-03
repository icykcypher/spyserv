
using DataService.Model.UsersModel;

namespace DataService.Services.UserServices
{
    public interface IUserService
    {
        Task<User> Register(RegisterUserDto registerUserDto);
    }
}