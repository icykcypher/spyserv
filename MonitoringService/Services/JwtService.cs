using System.Text;
using MonitoringService.Dto;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MonitoringService.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _jwtExpireDays;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSecretKey"]!;
            _issuer = configuration["JwtIssuer"]!;
            _audience = configuration["JwtAudience"]!;
            _jwtExpireDays = int.Parse(configuration["JwtExpireDays"] ?? "1");
        }

        public string GenerateToken(ClientApp clientApp)
        {
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new Exception("JWT Secret Key is not configured.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, clientApp.Id.ToString()),
                new Claim("UserEmail", clientApp.UserEmail),
                new Claim(ClaimTypes.Name, clientApp.DeviceName),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("deviceId", clientApp.DeviceName),
                new Claim("loginSource", "web"),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("exp_days", _jwtExpireDays.ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddDays(_jwtExpireDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}