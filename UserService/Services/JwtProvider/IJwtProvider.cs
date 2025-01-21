using UserService.Model;

namespace UserService.Services.JwtProvider
{
    public interface IJwtProvider
    {
        string GenerateToken(User user);
    }
}