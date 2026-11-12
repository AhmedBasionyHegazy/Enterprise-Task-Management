using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
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

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponseDto>.Fail("Username/Email and Password are required.", statusCode: 400);
            }

            var user = await _unitOfWork.Users.GetUserEntityByUsernameOrEmailAsync(request.UsernameOrEmail.Trim());
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials.", statusCode: 401);
            }

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Fail("This account has been deactivated. Please contact your system administrator.", statusCode: 403);
            }

            bool isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isValid)
            {
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials.", statusCode: 401);
            }

            string roleName = user.Role?.Name ?? "Employee";
            var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roleName);

            var userDto = _mapper.Map<UserDto>(user);

            var authResponse = new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = userDto
            };

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful.");
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(userId, includeRole: true);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<UserDto>.Fail("User not found or inactive.", statusCode: 404);
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.Ok(userDto);
        }
    }
}
