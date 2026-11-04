using System.Security.Claims;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers
{
    /// <summary>
    /// Chat interno de uma consulta (tutor ↔ veterinário). As mensagens são
    /// armazenadas em MongoDB; o restante do domínio fica no banco relacional.
    /// </summary>
    [Route("api/consultations/{consultationId:int}/messages")]
    [Authorize]
    public class ConsultationMessagesController : ApiControllerBase
    {
        private readonly IChatService _chatService;

        public ConsultationMessagesController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Lista as mensagens da consulta em ordem cronológica, com paginação.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ChatMessage>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResponse<ChatMessage>>> GetMessages(
            int consultationId, [FromQuery] PageQuery paging, CancellationToken ct)
        {
            var result = await _chatService.GetMessagesAsync(consultationId, paging, ct);
            var basePath = $"/api/consultations/{consultationId}/messages";

            return Ok(ToPaged(result, paging, basePath, new Dictionary<string, string?>(), _ => new[]
            {
                new Link("consultation", Abs($"/api/consultations/{consultationId}"), "GET")
            }));
        }

        /// <summary>
        /// Envia uma mensagem. O remetente é o usuário autenticado (claim <c>sub</c> do JWT) e
        /// precisa ser o veterinário da consulta ou o tutor do pet.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Resource<ChatMessage>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Resource<ChatMessage>>> Send(
            int consultationId, [FromBody] SendMessageDto dto, CancellationToken ct)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!int.TryParse(sub, out var senderId))
                return Unauthorized();

            var message = await _chatService.SendAsync(consultationId, senderId, dto.Content, ct);

            var resource = Wrap(message, new[]
            {
                new Link("consultation", Abs($"/api/consultations/{consultationId}"), "GET"),
                new Link("messages", Abs($"/api/consultations/{consultationId}/messages"), "GET")
            });

            return Created(Abs($"/api/consultations/{consultationId}/messages"), resource);
        }
    }
}