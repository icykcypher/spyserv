namespace UserService.Services.JwtProvider
{
    public class JwtOptions
    {
        public string SecretKey { get; set; } = string.Empty;

        public int ExpireHours { get; set; }
    }
}