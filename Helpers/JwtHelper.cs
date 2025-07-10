using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UAS_PAA.Models;

namespace UAS_PAA.Helpers
{
    public class JwtHelpers
    {
        private readonly IConfiguration _configuration;

        public JwtHelpers(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Users users)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new List<Claim>
            {
                new Claim("Id_Users", users.Id_Users.ToString()),
                new Claim(ClaimTypes.Email, users.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, users.Username ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
