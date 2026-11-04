using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Infrastructure.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public EfUserRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
            _context.Users.AsNoTracking().ToListAsync(ct);

        public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<List<User>> GetByRoleAsync(string role, CancellationToken ct = default) =>
            _context.Users.AsNoTracking()
                .Where(u => u.Role.ToLower() == role.ToLower())
                .ToListAsync(ct);

        public Task<List<User>> SearchByNameAsync(string name, CancellationToken ct = default) =>
            _context.Users.AsNoTracking()
                .Where(u => u.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync(ct);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
        {
            var query = _context.Users.Where(u => u.Email.ToLower() == email.ToLower());

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync(ct);
        }

        public Task<PagedResult<User>> GetPagedAsync(UserFilter filter, PageQuery page, CancellationToken ct = default)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Role))
                query = query.Where(u => u.Role.ToLower() == filter.Role.ToLower());

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u => u.Name.ToLower().Contains(filter.Search.ToLower()));

            var ordered = (page.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.Order(u => u.Name, page.Desc),
                "email" => query.Order(u => u.Email, page.Desc),
                "role" => query.Order(u => u.Role, page.Desc),
                "createdat" => query.Order(u => u.CreatedAt, page.Desc),
                _ => query.Order(u => u.Id, page.Desc)
            };

            return ordered.ToPagedAsync(page, ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default) =>
            await _context.Users.AddAsync(user, ct);

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Remove(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);
    }
}
