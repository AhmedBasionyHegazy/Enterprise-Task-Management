using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
        Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMapper _mapper;
        private readonly IAuditService _audit;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper,
            IAuditService audit,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
            _audit = audit;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponseDto>.Fail("Username/Email and Password are required.", statusCode: 400);
            }

            var trimmed = request.UsernameOrEmail.Trim();
            var identifier = trimmed.Length > 150 ? trimmed[..150] : trimmed;
            var user = await _unitOfWork.Users.GetUserEntityByUsernameOrEmailAsync(identifier);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed action={Action} username={Username} reason=unknown_account", AuditActions.LoginFailed, identifier);
                await _audit.LogAsync(AuditActions.LoginFailed, "Auth", null, false,
                    "Login failed: unknown account.", username: identifier);
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials.", statusCode: 401);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication rejected action={Action} userId={UserId} username={Username} reason=deactivated", AuditActions.LoginFailed, user.Id, user.Username);
                await _audit.LogAsync(AuditActions.LoginFailed, "Auth", user.Id.ToString(), false,
                    "Login rejected: account is deactivated.", user.Id, user.Username);
                return ApiResponse<AuthResponseDto>.Fail("This account has been deactivated. Please contact your system administrator.", statusCode: 403);
            }

            bool isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isValid)
            {
                _logger.LogWarning("Authentication failed action={Action} userId={UserId} username={Username} reason=invalid_password", AuditActions.LoginFailed, user.Id, user.Username);
                await _audit.LogAsync(AuditActions.LoginFailed, "Auth", user.Id.ToString(), false,
                    "Login failed: invalid password.", user.Id, user.Username);
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials.", statusCode: 401);
            }

            string roleName = user.Role?.Name ?? "Employee";
            var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roleName);

            var authResponse = new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserDto>(user)
            };

            _logger.LogInformation("Authentication succeeded action={Action} userId={UserId} username={Username} role={Role}", AuditActions.Login, user.Id, user.Username, roleName);
            await _audit.LogAsync(AuditActions.Login, "Auth", user.Id.ToString(), true,
                "User signed in.", user.Id, user.Username);

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful.");
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(userId, includeRole: true);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<UserDto>.Fail("User not found or inactive.", statusCode: 404);
            }

            return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user));
        }
    }
}
