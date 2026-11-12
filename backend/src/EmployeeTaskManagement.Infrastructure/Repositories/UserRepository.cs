using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class UserRepository : Repository<ApplicationDbContext, User, UserDto>, IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UserRepository(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<User?> GetUserEntityByIdAsync(int id, bool includeRole = true)
        {
            IQueryable<User> query = _context.Users;
            if (includeRole)
            {
                query = query.Include(u => u.Role);
            }
            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserEntityByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<bool> IsUsernameTakenAsync(string username, int? excludeUserId = null)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower() && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
        }

        public async Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
        }

        public async Task<IEnumerable<UserDto>> GetEmployeesAsync()
        {
            var employees = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.Name == "Employee")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDto>>(employees);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public void UpdateEntity(User user)
        {
            _context.Users.Update(user);
        }

        public void AddEntity(User user)
        {
            _context.Users.Add(user);
        }
    }
}
