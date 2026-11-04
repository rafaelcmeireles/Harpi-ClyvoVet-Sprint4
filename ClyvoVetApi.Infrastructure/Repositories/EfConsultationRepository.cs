using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Data;
using ClyvoVetApi.Domain;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Infrastructure.Repositories
{
    public class EfConsultationRepository : IConsultationRepository
    {
        private readonly AppDbContext _context;

        public EfConsultationRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Consultation>> GetAllAsync(CancellationToken ct = default) =>
            _context.Consultations.AsNoTracking().ToListAsync(ct);

        public Task<Consultation?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _context.Consultations.FirstOrDefaultAsync(c => c.Id == id, ct);

        public Task<List<Consultation>> GetByPetAsync(int petId, CancellationToken ct = default) =>
            _context.Consultations.AsNoTracking()
                .Where(c => c.PetId == petId)
                .OrderByDescending(c => c.ConsultationDate)
                .ToListAsync(ct);

        public Task<List<Consultation>> GetByVetAsync(int vetId, CancellationToken ct = default) =>
            _context.Consultations.AsNoTracking()
                .Where(c => c.VetId == vetId)
                .OrderByDescending(c => c.ConsultationDate)
                .ToListAsync(ct);

        public Task<List<Consultation>> GetByStatusAsync(string status, CancellationToken ct = default) =>
            _context.Consultations.AsNoTracking()
                .Where(c => c.Status.ToLower() == status.ToLower())
                .OrderByDescending(c => c.ConsultationDate)
                .ToListAsync(ct);

        public Task<List<Consultation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken ct = default) =>
            _context.Consultations.AsNoTracking()
                .Where(c => c.ConsultationDate >= start && c.ConsultationDate <= end)
                .OrderByDescending(c => c.ConsultationDate)
                .ToListAsync(ct);

        /// <summary>
        /// Busca no banco um superconjunto de candidatos dentro de uma janela ampla (tradutível para SQL)
        /// e aplica a regra exata de conflito (<see cref="ConsultationSchedulingRules.HasConflict"/>) em
        /// memória — mantendo a regra de negócio na camada de Domínio, testável isoladamente.
        /// </summary>
        public async Task<bool> HasScheduleConflictAsync(
            int vetId,
            DateTime date,
            TimeSpan minimumGap,
            int? excludeConsultationId = null,
            CancellationToken ct = default)
        {
            var windowStart = date - minimumGap;
            var windowEnd = date + minimumGap;

            var query = _context.Consultations.AsNoTracking()
                .Where(c => c.VetId == vetId)
                .Where(c => c.Status != ConsultationStatuses.Cancelada)
                .Where(c => c.ConsultationDate >= windowStart && c.ConsultationDate <= windowEnd);

            if (excludeConsultationId.HasValue)
                query = query.Where(c => c.Id != excludeConsultationId.Value);

            var candidateDates = await query.Select(c => c.ConsultationDate).ToListAsync(ct);

            return candidateDates.Any(existing => ConsultationSchedulingRules.HasConflict(existing, date, minimumGap));
        }

        public Task<PagedResult<Consultation>> GetPagedAsync(ConsultationFilter filter, PageQuery page, CancellationToken ct = default)
        {
            var query = _context.Consultations.AsNoTracking().AsQueryable();

            if (filter.PetId.HasValue)
                query = query.Where(c => c.PetId == filter.PetId.Value);

            if (filter.VetId.HasValue)
                query = query.Where(c => c.VetId == filter.VetId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(c => c.Status.ToLower() == filter.Status.ToLower());

            if (filter.From.HasValue)
                query = query.Where(c => c.ConsultationDate >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(c => c.ConsultationDate <= filter.To.Value);

            var ordered = (page.SortBy?.ToLowerInvariant()) switch
            {
                "consultationdate" => query.Order(c => c.ConsultationDate, page.Desc),
                "status" => query.Order(c => c.Status, page.Desc),
                "createdat" => query.Order(c => c.CreatedAt, page.Desc),
                _ => query.Order(c => c.Id, page.Desc)
            };

            return ordered.ToPagedAsync(page, ct);
        }

        public async Task AddAsync(Consultation consultation, CancellationToken ct = default) =>
            await _context.Consultations.AddAsync(consultation, ct);

        public Task DeleteAsync(Consultation consultation, CancellationToken ct = default)
        {
            _context.Consultations.Remove(consultation);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);
    }
}
