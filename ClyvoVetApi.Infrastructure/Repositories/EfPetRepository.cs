using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Infrastructure.Repositories
{
    public class EfPetRepository : IPetRepository
    {
        private readonly AppDbContext _context;

        public EfPetRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Pet>> GetAllAsync(CancellationToken ct = default) =>
            _context.Pets.AsNoTracking().ToListAsync(ct);

        public Task<Pet?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _context.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);

        public Task<List<Pet>> GetByTutorAsync(int tutorId, CancellationToken ct = default) =>
            _context.Pets.AsNoTracking()
                .Where(p => p.TutorId == tutorId)
                .ToListAsync(ct);

        public Task<List<Pet>> GetBySpeciesAsync(string species, CancellationToken ct = default) =>
            _context.Pets.AsNoTracking()
                .Where(p => p.Species.ToLower() == species.ToLower())
                .ToListAsync(ct);

        public Task<List<Pet>> GetByBreedAsync(string breed, CancellationToken ct = default) =>
            _context.Pets.AsNoTracking()
                .Where(p => p.Breed != null && p.Breed.ToLower().Contains(breed.ToLower()))
                .ToListAsync(ct);

        public Task<List<Pet>> SearchByNameAsync(string name, CancellationToken ct = default) =>
            _context.Pets.AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync(ct);

        public Task<PagedResult<Pet>> GetPagedAsync(PetFilter filter, PageQuery page, CancellationToken ct = default)
        {
            var query = _context.Pets.AsNoTracking().AsQueryable();

            if (filter.TutorId.HasValue)
                query = query.Where(p => p.TutorId == filter.TutorId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Species))
                query = query.Where(p => p.Species.ToLower() == filter.Species.ToLower());

            if (!string.IsNullOrWhiteSpace(filter.Breed))
                query = query.Where(p => p.Breed != null && p.Breed.ToLower() == filter.Breed.ToLower());

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p => p.Name.ToLower().Contains(filter.Search.ToLower()));

            var ordered = (page.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.Order(p => p.Name, page.Desc),
                "species" => query.Order(p => p.Species, page.Desc),
                "breed" => query.Order(p => p.Breed, page.Desc),
                "weightkg" => query.Order(p => p.WeightKg, page.Desc),
                _ => query.Order(p => p.Id, page.Desc)
            };

            return ordered.ToPagedAsync(page, ct);
        }

        public async Task AddAsync(Pet pet, CancellationToken ct = default) =>
            await _context.Pets.AddAsync(pet, ct);

        public Task DeleteAsync(Pet pet, CancellationToken ct = default)
        {
            _context.Pets.Remove(pet);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);
    }
}
