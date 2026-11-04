using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Abstractions
{
    /// <summary>
    /// Porta de acesso a dados de Consulta.
    /// </summary>
    public interface IConsultationRepository
    {
        Task<List<Consultation>> GetAllAsync(CancellationToken ct = default);
        Task<Consultation?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Consultation>> GetByPetAsync(int petId, CancellationToken ct = default);
        Task<List<Consultation>> GetByVetAsync(int vetId, CancellationToken ct = default);
        Task<List<Consultation>> GetByStatusAsync(string status, CancellationToken ct = default);
        Task<List<Consultation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken ct = default);

        /// <summary>
        /// Verifica se o veterinário já possui uma consulta marcada dentro da janela mínima
        /// (<paramref name="minimumGap"/>) em torno de <paramref name="date"/>.
        /// </summary>
        Task<bool> HasScheduleConflictAsync(
            int vetId,
            DateTime date,
            TimeSpan minimumGap,
            int? excludeConsultationId = null,
            CancellationToken ct = default);

        Task<PagedResult<Consultation>> GetPagedAsync(ConsultationFilter filter, PageQuery page, CancellationToken ct = default);

        Task AddAsync(Consultation consultation, CancellationToken ct = default);
        Task DeleteAsync(Consultation consultation, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
