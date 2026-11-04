using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Domain;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Application.Observability;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Services
{
    /// <summary>
    /// Casos de uso relacionados a Consultas veterinárias.
    /// </summary>
    public interface IConsultationService
    {
        Task<List<Consultation>> GetAllAsync(CancellationToken ct = default);
        Task<Consultation> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Consultation>> GetByPetAsync(int petId, CancellationToken ct = default);
        Task<List<Consultation>> GetByVetAsync(int vetId, CancellationToken ct = default);
        Task<List<Consultation>> GetByStatusAsync(string status, CancellationToken ct = default);
        Task<List<Consultation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken ct = default);
        Task<PagedResult<Consultation>> GetPagedAsync(ConsultationFilter filter, PageQuery page, CancellationToken ct = default);
        Task<Consultation> CreateAsync(CreateConsultationDto dto, CancellationToken ct = default);
        Task<Consultation> UpdateAsync(int id, UpdateConsultationDto dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }

    public class ConsultationService : IConsultationService
    {
        /// <summary>
        /// Janela mínima entre duas consultas de um mesmo veterinário. Exposta como público
        /// estático para ser reutilizada (e verificada) pelos testes unitários.
        /// </summary>
        public static readonly TimeSpan MinimumGapBetweenConsultations = TimeSpan.FromMinutes(30);

        private readonly IConsultationRepository _consultations;
        private readonly IPetRepository _pets;
        private readonly IUserRepository _users;
        private readonly IDateTimeProvider _clock;

        public ConsultationService(
            IConsultationRepository consultations,
            IPetRepository pets,
            IUserRepository users,
            IDateTimeProvider clock)
        {
            _consultations = consultations;
            _pets = pets;
            _users = users;
            _clock = clock;
        }

        public static readonly string[] SortableFields = { "id", "consultationDate", "status", "createdAt" };

        public Task<PagedResult<Consultation>> GetPagedAsync(ConsultationFilter filter, PageQuery page, CancellationToken ct = default)
        {
            SortValidator.Ensure(page, SortableFields);

            if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
                throw new BusinessRuleException("A data inicial não pode ser maior que a data final.");

            return _consultations.GetPagedAsync(filter, page, ct);
        }

        public Task<List<Consultation>> GetAllAsync(CancellationToken ct = default) =>
            _consultations.GetAllAsync(ct);

        public async Task<Consultation> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var consultation = await _consultations.GetByIdAsync(id, ct);
            return consultation ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");
        }

        public Task<List<Consultation>> GetByPetAsync(int petId, CancellationToken ct = default) =>
            _consultations.GetByPetAsync(petId, ct);

        public Task<List<Consultation>> GetByVetAsync(int vetId, CancellationToken ct = default) =>
            _consultations.GetByVetAsync(vetId, ct);

        public Task<List<Consultation>> GetByStatusAsync(string status, CancellationToken ct = default) =>
            _consultations.GetByStatusAsync(status, ct);

        public Task<List<Consultation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken ct = default)
        {
            if (start > end)
                throw new BusinessRuleException("A data de início deve ser anterior à data de fim.");

            return _consultations.GetByPeriodAsync(start, end, ct);
        }

        public async Task<Consultation> CreateAsync(CreateConsultationDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("ConsultationService.Create");

            var pet = await _pets.GetByIdAsync(dto.PetId, ct);
            if (pet == null)
            {
                scope.MarkAsBusinessRuleViolation("pet_not_found");
                throw new BusinessRuleException($"Pet com ID {dto.PetId} não encontrado.");
            }

            var vet = await _users.GetByIdAsync(dto.VetId, ct);
            if (vet == null)
            {
                scope.MarkAsBusinessRuleViolation("vet_not_found");
                throw new BusinessRuleException($"Veterinário com ID {dto.VetId} não encontrado.");
            }

            if (!UserRoles.IsVeterinario(vet.Role))
            {
                scope.MarkAsBusinessRuleViolation("not_a_vet");
                throw new BusinessRuleException("O usuário informado não é um Veterinário.");
            }

            if (!ConsultationSchedulingRules.IsFutureOrPresent(dto.ConsultationDate, _clock.UtcNow))
            {
                scope.MarkAsBusinessRuleViolation("past_date");
                throw new BusinessRuleException("Não é possível agendar uma consulta em uma data passada.");
            }

            var hasConflict = await _consultations.HasScheduleConflictAsync(
                dto.VetId, dto.ConsultationDate, MinimumGapBetweenConsultations, excludeConsultationId: null, ct: ct);

            if (hasConflict)
            {
                scope.MarkAsBusinessRuleViolation("schedule_conflict");
                throw new BusinessRuleException("O veterinário já possui uma consulta agendada próxima a este horário.");
            }

            var consultation = new Consultation
            {
                PetId = dto.PetId,
                VetId = dto.VetId,
                ConsultationDate = dto.ConsultationDate,
                Description = dto.Description,
                Prescription = dto.Prescription,
                Status = ConsultationStatuses.Agendada,
                CreatedAt = _clock.UtcNow
            };

            await _consultations.AddAsync(consultation, ct);
            await _consultations.SaveChangesAsync(ct);

            return consultation;
        }

        public async Task<Consultation> UpdateAsync(int id, UpdateConsultationDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("ConsultationService.Update");

            var consultation = await _consultations.GetByIdAsync(id, ct)
                                ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");

            var newStatus = string.IsNullOrWhiteSpace(dto.Status) ? consultation.Status : dto.Status!;

            if (!ConsultationStatuses.IsValid(newStatus))
            {
                scope.MarkAsBusinessRuleViolation("invalid_status");
                throw new BusinessRuleException("Status inválido. Use 'Agendada', 'Realizada' ou 'Cancelada'.");
            }

            if (!ConsultationStatuses.CanTransition(consultation.Status, newStatus))
            {
                scope.MarkAsBusinessRuleViolation("invalid_status_transition");
                throw new BusinessRuleException($"Não é possível alterar uma consulta com status '{consultation.Status}'.");
            }

            if (!ConsultationSchedulingRules.IsFutureOrPresent(dto.ConsultationDate, _clock.UtcNow))
            {
                scope.MarkAsBusinessRuleViolation("past_date");
                throw new BusinessRuleException("Não é possível reagendar uma consulta para uma data passada.");
            }

            var hasConflict = await _consultations.HasScheduleConflictAsync(
                consultation.VetId, dto.ConsultationDate, MinimumGapBetweenConsultations, excludeConsultationId: id, ct: ct);

            if (hasConflict)
            {
                scope.MarkAsBusinessRuleViolation("schedule_conflict");
                throw new BusinessRuleException("O veterinário já possui uma consulta agendada próxima a este horário.");
            }

            consultation.ConsultationDate = dto.ConsultationDate;
            consultation.Description = dto.Description;
            consultation.Prescription = dto.Prescription;
            consultation.Status = newStatus;

            await _consultations.SaveChangesAsync(ct);
            return consultation;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var consultation = await _consultations.GetByIdAsync(id, ct)
                                ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");

            await _consultations.DeleteAsync(consultation, ct);
            await _consultations.SaveChangesAsync(ct);
        }
    }
}
