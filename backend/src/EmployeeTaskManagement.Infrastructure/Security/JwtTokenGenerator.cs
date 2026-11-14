using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Core.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmployeeTaskManagement.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName)
        {
            var jwtSecret = _configuration["Jwt:Key"] ?? "TaskFlow_Ultra_Secure_Secret_Key_For_JWT_Authentication_2026_Enterprise_Grade!";
            var issuer = _configuration["Jwt:Issuer"] ?? "EmployeeTaskManagementAPI";
            var audience = _configuration["Jwt:Audience"] ?? "EmployeeTaskManagementClient";
            var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var exp) && exp > 0 ? exp : 15;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("fullName", user.FullName),
                new Claim("department", user.Department ?? string.Empty),
                new Claim("roleId", user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(securityToken);

            return (tokenString, expiresAt);
        }
    }
}
