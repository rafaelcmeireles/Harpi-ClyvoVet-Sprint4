using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Domain;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Services
{
    /// <summary>Casos de uso do chat interno de uma consulta (persistido em MongoDB).</summary>
    public interface IChatService
    {
        Task<ChatMessage> SendAsync(int consultationId, int senderId, string content, CancellationToken ct = default);
        Task<PagedResult<ChatMessage>> GetMessagesAsync(int consultationId, PageQuery page, CancellationToken ct = default);
    }

    public class ChatService : IChatService
    {
        private readonly IChatMessageRepository _messages;
        private readonly IConsultationRepository _consultations;
        private readonly IPetRepository _pets;
        private readonly IUserRepository _users;
        private readonly IDateTimeProvider _clock;

        public ChatService(
            IChatMessageRepository messages,
            IConsultationRepository consultations,
            IPetRepository pets,
            IUserRepository users,
            IDateTimeProvider clock)
        {
            _messages = messages;
            _consultations = consultations;
            _pets = pets;
            _users = users;
            _clock = clock;
        }

        public async Task<ChatMessage> SendAsync(int consultationId, int senderId, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new BusinessRuleException("A mensagem não pode ser vazia.");

            var consultation = await GetConsultationAsync(consultationId, ct);
            var sender = await _users.GetByIdAsync(senderId, ct)
                         ?? throw new NotFoundException($"Usuário com ID {senderId} não encontrado.");

            await EnsureParticipantAsync(consultation, sender, ct);

            var message = new ChatMessage
            {
                ConsultationId = consultationId,
                SenderId = senderId,
                SenderRole = sender.Role,
                Content = content.Trim(),
                SentAt = _clock.UtcNow
            };

            await _messages.AddAsync(message, ct);
            return message;
        }

        public async Task<PagedResult<ChatMessage>> GetMessagesAsync(int consultationId, PageQuery page, CancellationToken ct = default)
        {
            await GetConsultationAsync(consultationId, ct);
            return await _messages.GetByConsultationAsync(consultationId, page, ct);
        }

        private async Task<Consultation> GetConsultationAsync(int consultationId, CancellationToken ct) =>
            await _consultations.GetByIdAsync(consultationId, ct)
            ?? throw new NotFoundException($"Consulta com ID {consultationId} não encontrada.");

        /// <summary>Somente o veterinário da consulta ou o tutor do pet podem conversar nela.</summary>
        private async Task EnsureParticipantAsync(Consultation consultation, User sender, CancellationToken ct)
        {
            if (sender.Id == consultation.VetId)
                return;

            var pet = await _pets.GetByIdAsync(consultation.PetId, ct);
            if (pet is not null && sender.Id == pet.TutorId)
                return;

            throw new ForbiddenAppException("Apenas o tutor do pet e o veterinário da consulta podem trocar mensagens.");
        }
    }
}