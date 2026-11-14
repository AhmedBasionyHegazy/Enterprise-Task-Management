using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Infrastructure.Data;
using EmployeeTaskManagement.Infrastructure.Repositories;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext? _context;
        private IDbContextTransaction? _transaction;
        private readonly IMapper _mapper;
        private readonly ConcurrentDictionary<string, object> _repositories = new();

        public IUserRepository Users { get; private set; }
        public ITaskRepository Tasks { get; private set; }
        public ITaskCommentRepository TaskComments { get; private set; }
        public IRoleRepository Roles { get; private set; }
        public IAuditLogRepository AuditLogs { get; private set; }
        public INotificationRepository Notifications { get; private set; }

        public UnitOfWork(ApplicationDbContext context, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            Users = new UserRepository(_context, _mapper);
            Tasks = new TaskRepository(_context, _mapper);
            TaskComments = new TaskCommentRepository(_context, _mapper);
            Roles = new RoleRepository(_context, _mapper);
            AuditLogs = new AuditLogRepository(_context);
            Notifications = new NotificationRepository(_context);
        }

        public IRepository<ApplicationDbContext, T, TDto> GetRepository<T, TDto>() 
            where T : class 
            where TDto : class
        {
            if (_context == null) throw new ObjectDisposedException(nameof(UnitOfWork));

            var key = $"{typeof(T).FullName}_{typeof(TDto).FullName}";
            return (IRepository<ApplicationDbContext, T, TDto>)_repositories.GetOrAdd(key, _ => 
                new Repository<ApplicationDbContext, T, TDto>(_context, _mapper));
        }

        public async Task<int> SaveChanges()
        {
            if (_context == null) throw new ObjectDisposedException(nameof(UnitOfWork));
            return await _context.SaveChangesAsync();
        }

        public void StartTransaction()
        {
            if (_context == null) throw new ObjectDisposedException(nameof(UnitOfWork));
            _transaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction == null) throw new InvalidOperationException("No transaction is currently active.");
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        public void Rollback()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_context == null)
            {
                return;
            }

            _context.Dispose();
            _context = null;
        }
    }
}
