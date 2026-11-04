using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Abstractions
{
    /// <summary>
    /// Porta de acesso a dados de Usuário. Abstrai o EF Core para que a camada de
    /// Aplicação possa ser testada com Moq, sem depender de um banco de dados real.
    /// </summary>
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<User>> GetByRoleAsync(string role, CancellationToken ct = default);
        Task<List<User>> SearchByNameAsync(string name, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default);
        Task<PagedResult<User>> GetPagedAsync(UserFilter filter, PageQuery page, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        Task DeleteAsync(User user, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
