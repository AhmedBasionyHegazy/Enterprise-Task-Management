using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class Repository<TContext, T, TDto> : IRepository<TContext, T, TDto> 
        where T : class 
        where TContext : DbContext 
        where TDto : class
    {
        private readonly TContext _db;
        private readonly IMapper _mapper;
        internal DbSet<T> dbSet;

        public Repository(TContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            this.dbSet = _db.Set<T>();
        }

        public TDto Add(TDto entityDto)
        {
            T entity = _mapper.Map<TDto, T>(entityDto);
            dbSet.Add(entity);
            return _mapper.Map<TDto>(entity);
        }

        public async Task<IEnumerable<TDto>> GetAll(string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property.Trim());
                }
            }
            return _mapper.Map<IEnumerable<TDto>>(await query.ToListAsync());
        }

        public async Task<TDto?> GetFirstOrDefault(Expression<Func<T, bool>> filter, bool? disableTracking = false, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            query = (disableTracking.HasValue && disableTracking.Value) ? query.Where(filter).AsNoTracking() : query.Where(filter);

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property.Trim());
                }
            }
            var entity = await query.FirstOrDefaultAsync();
            if (entity == null) return null;
            return _mapper.Map<TDto>(entity);
        }

        public bool Remove(TDto entityDto)
        {
            try
            {
                if (entityDto == null)
                {
                    return false;
                }
                T entity = _mapper.Map<T>(entityDto);
                dbSet.Remove(entity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RemoveRange(IEnumerable<TDto> entityDtos)
        {
            try
            {
                IEnumerable<T> entities = _mapper.Map<IEnumerable<T>>(entityDtos);
                dbSet.RemoveRange(entities);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TDto Update(TDto entityDto)
        {
            T entity = _mapper.Map<TDto, T>(entityDto);
            _db.Entry(entity).State = EntityState.Modified;
            dbSet.Update(entity);
            return _mapper.Map<TDto>(entity);
        }

        public async Task<IEnumerable<TDto>> GetAllRecords(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) 
            { 
                query = query.Where(filter); 
            }
            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property.Trim());
                }
            }
            return _mapper.Map<IEnumerable<TDto>>(await query.ToListAsync());
        }

        public async Task<int> SaveChanges()
        {
            return await _db.SaveChangesAsync();
        }
    }
}
