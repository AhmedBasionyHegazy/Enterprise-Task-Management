using EmployeeTaskManagement.Core.Entities;
using System;

namespace EmployeeTaskManagement.Application.Interfaces.Security
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName);
    }
}
