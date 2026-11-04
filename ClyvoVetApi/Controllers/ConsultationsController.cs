using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;

namespace ClyvoVetApi.Controllers
{
    /// <summary>
    /// Controller responsável pelo CRUD de Consultas Veterinárias. Toda a regra de negócio
    /// (validação de agenda, conflitos de horário, transições de status) vive em
    /// <see cref="IConsultationService"/>.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class ConsultationsController : ApiControllerBase
    {
        private readonly IConsultationService _consultationService;

        public ConsultationsController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }

        // ==================== GET ====================

        /// <summary>
        /// Lista consultas com paginação, ordenação e filtros.
        /// Ordenação (<c>sortBy</c>): id, consultationDate, status, createdAt.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<Consultation>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<Consultation>>> GetAll(
            [FromQuery] PageQuery paging,
            [FromQuery] int? petId,
            [FromQuery] int? vetId,
            [FromQuery] string? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
        {
            var result = await _consultationService.GetPagedAsync(
                new ConsultationFilter(petId, vetId, status, from, to), paging, ct);

            var filters = new Dictionary<string, string?>
            {
                ["petId"] = petId?.ToString(),
                ["vetId"] = vetId?.ToString(),
                ["status"] = status,
                ["from"] = from?.ToString("o"),
                ["to"] = to?.ToString("o")
            };
            return Ok(ToPaged(result, paging, "/api/consultations", filters, LinksFor));
        }

        /// <summary>
        /// Retorna uma consulta pelo seu ID, com links HATEOAS.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Resource<Consultation>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Resource<Consultation>>> GetById(int id, CancellationToken ct)
        {
            var consultation = await _consultationService.GetByIdAsync(id, ct);
            return Ok(Wrap(consultation, LinksFor(consultation)));
        }

        private IEnumerable<Link> LinksFor(Consultation consultation)
        {
            var self = Abs($"/api/consultations/{consultation.Id}");
            yield return new Link("self", self, "GET");
            yield return new Link("update", self, "PUT");
            yield return new Link("delete", self, "DELETE");
            yield return new Link("pet", Abs($"/api/pets/{consultation.PetId}"), "GET");
            yield return new Link("vet", Abs($"/api/users/{consultation.VetId}"), "GET");
            yield return new Link("messages", Abs($"/api/consultations/{consultation.Id}/messages"), "GET");
        }

        /// <summary>
        /// Retorna todas as consultas de um pet específico.
        /// </summary>
        [HttpGet("pet/{petId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Consultation>>> GetByPet(int petId, CancellationToken ct)
        {
            var consultations = await _consultationService.GetByPetAsync(petId, ct);
            return Ok(consultations);
        }

        /// <summary>
        /// Retorna todas as consultas de um veterinário específico.
        /// </summary>
        [HttpGet("vet/{vetId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Consultation>>> GetByVet(int vetId, CancellationToken ct)
        {
            var consultations = await _consultationService.GetByVetAsync(vetId, ct);
            return Ok(consultations);
        }

        /// <summary>
        /// Retorna todas as consultas filtradas pelo status (Agendada, Realizada, Cancelada).
        /// </summary>
        [HttpGet("status/{status}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Consultation>>> GetByStatus(string status, CancellationToken ct)
        {
            var consultations = await _consultationService.GetByStatusAsync(status, ct);
            return Ok(consultations);
        }

        /// <summary>
        /// Retorna todas as consultas dentro de um intervalo de datas.
        /// </summary>
        [HttpGet("period")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Consultation>>> GetByPeriod(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            CancellationToken ct)
        {
            var consultations = await _consultationService.GetByPeriodAsync(startDate, endDate, ct);
            return Ok(consultations);
        }

        // ==================== POST ====================

        /// <summary>
        /// Agenda uma nova consulta veterinária.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Consultation>> Create([FromBody] CreateConsultationDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = consultation.Id }, consultation);
        }

        // ==================== PUT ====================

        /// <summary>
        /// Atualiza os dados de uma consulta existente (incluindo mudança de status).
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Consultation>> Update(int id, [FromBody] UpdateConsultationDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.UpdateAsync(id, dto, ct);
            return Ok(consultation);
        }

        // ==================== DELETE ====================

        /// <summary>
        /// Remove uma consulta pelo ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Veterinario")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _consultationService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
