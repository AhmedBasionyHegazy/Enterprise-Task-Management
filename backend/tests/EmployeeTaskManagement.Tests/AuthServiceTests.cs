using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Application.Mappings;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class AuthServiceTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            var provider = services.BuildServiceProvider();
            _mapper = provider.GetRequiredService<IMapper>();

            _mockUow = new Mock<IUnitOfWork>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();

            _authService = new AuthService(
                _mockUow.Object,
                _mockPasswordHasher.Object,
                _mockJwtGenerator.Object,
                _mapper,
                Mock.Of<IAuditService>(),
                NullLogger<AuthService>.Instance
            );
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsDeactivated_ReturnsForbidden()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                UsernameOrEmail = "inactive.user",
                Password = "Password123"
            };

            var user = new User
            {
                Id = 5,
                Username = "inactive.user",
                Email = "inactive@test.com",
                PasswordHash = "hash",
                IsActive = false // DEACTIVATED
            };

            _mockUow.Setup(u => u.Users.GetUserEntityByUsernameOrEmailAsync("inactive.user"))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_WhenCredentialsValid_ReturnsJwtTokenAndUser()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                UsernameOrEmail = "admin",
                Password = "Admin@123"
            };

            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@taskflow.com",
                PasswordHash = "hashed_pw",
                FullName = "Admin User",
                IsActive = true,
                Role = new Role { Id = 1, Name = "Admin" }
            };

            _mockUow.Setup(u => u.Users.GetUserEntityByUsernameOrEmailAsync("admin"))
                .ReturnsAsync(user);
            _mockPasswordHasher.Setup(p => p.VerifyPassword("Admin@123", "hashed_pw"))
                .Returns(true);
            _mockJwtGenerator.Setup(j => j.GenerateToken(user, "Admin"))
                .Returns(("fake_jwt_token_xyz", DateTime.UtcNow.AddHours(8)));

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("fake_jwt_token_xyz", result.Data.Token);
            Assert.Equal("admin", result.Data.User.Username);
        }
    }
}
