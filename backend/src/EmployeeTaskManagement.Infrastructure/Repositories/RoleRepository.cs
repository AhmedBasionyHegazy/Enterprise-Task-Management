using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class RoleRepository : Repository<ApplicationDbContext, Role, RoleDto>, IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public RoleRepository(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Role?> GetRoleByNameAsync(string name)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _context.Roles.ToListAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }
    }
}
