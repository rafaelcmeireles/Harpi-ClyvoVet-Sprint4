using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Abstractions
{
    /// <summary>
    /// Porta de acesso a dados de Pet.
    /// </summary>
    public interface IPetRepository
    {
        Task<List<Pet>> GetAllAsync(CancellationToken ct = default);
        Task<Pet?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Pet>> GetByTutorAsync(int tutorId, CancellationToken ct = default);
        Task<List<Pet>> GetBySpeciesAsync(string species, CancellationToken ct = default);
        Task<List<Pet>> GetByBreedAsync(string breed, CancellationToken ct = default);
        Task<List<Pet>> SearchByNameAsync(string name, CancellationToken ct = default);
        Task<PagedResult<Pet>> GetPagedAsync(PetFilter filter, PageQuery page, CancellationToken ct = default);
        Task AddAsync(Pet pet, CancellationToken ct = default);
        Task DeleteAsync(Pet pet, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
