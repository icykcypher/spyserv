using UserService.Model;

namespace UserService.Services.UserService
{
    public interface IUserManagmentService
    {
        Task<string> Login(SignInUserDto signInUserDto);
        Task<User> Register(RegisterUserDto registerUserDto);
    }
}