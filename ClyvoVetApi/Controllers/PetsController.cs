using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;

namespace ClyvoVetApi.Controllers
{
    /// <summary>
    /// Controller responsável pelo CRUD de Pets. Toda a regra de negócio vive em
    /// <see cref="IPetService"/>.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class PetsController : ApiControllerBase
    {
        private readonly IPetService _petService;

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        // ==================== GET ====================

        /// <summary>
        /// Lista pets com paginação, ordenação e filtros.
        /// Ordenação (<c>sortBy</c>): id, name, species, breed, weightKg.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<Pet>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<Pet>>> GetAll(
            [FromQuery] PageQuery paging,
            [FromQuery] int? tutorId,
            [FromQuery] string? species,
            [FromQuery] string? breed,
            [FromQuery] string? search,
            CancellationToken ct)
        {
            var result = await _petService.GetPagedAsync(new PetFilter(tutorId, species, breed, search), paging, ct);
            var filters = new Dictionary<string, string?>
            {
                ["tutorId"] = tutorId?.ToString(),
                ["species"] = species,
                ["breed"] = breed,
                ["search"] = search
            };
            return Ok(ToPaged(result, paging, "/api/pets", filters, LinksFor));
        }

        /// <summary>
        /// Retorna um pet pelo seu ID, com links HATEOAS.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Resource<Pet>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Resource<Pet>>> GetById(int id, CancellationToken ct)
        {
            var pet = await _petService.GetByIdAsync(id, ct);
            return Ok(Wrap(pet, LinksFor(pet)));
        }

        private IEnumerable<Link> LinksFor(Pet pet)
        {
            var self = Abs($"/api/pets/{pet.Id}");
            yield return new Link("self", self, "GET");
            yield return new Link("update", self, "PUT");
            yield return new Link("delete", self, "DELETE");
            yield return new Link("tutor", Abs($"/api/users/{pet.TutorId}"), "GET");
            yield return new Link("consultations", Abs($"/api/consultations?petId={pet.Id}"), "GET");
        }

        /// <summary>
        /// Retorna todos os pets de um Tutor específico.
        /// </summary>
        [HttpGet("tutor/{tutorId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Pet>>> GetByTutor(int tutorId, CancellationToken ct)
        {
            var pets = await _petService.GetByTutorAsync(tutorId, ct);
            return Ok(pets);
        }

        /// <summary>
        /// Retorna todos os pets filtrados por espécie (ex: Cachorro, Gato).
        /// </summary>
        [HttpGet("species/{species}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Pet>>> GetBySpecies(string species, CancellationToken ct)
        {
            var pets = await _petService.GetBySpeciesAsync(species, ct);
            return Ok(pets);
        }

        /// <summary>
        /// Retorna todos os pets filtrados por raça.
        /// </summary>
        [HttpGet("breed/{breed}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Pet>>> GetByBreed(string breed, CancellationToken ct)
        {
            var pets = await _petService.GetByBreedAsync(breed, ct);
            return Ok(pets);
        }

        /// <summary>
        /// Busca pets pelo nome (pesquisa parcial).
        /// </summary>
        [HttpGet("search/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Pet>>> SearchByName(string name, CancellationToken ct)
        {
            var pets = await _petService.SearchByNameAsync(name, ct);
            return Ok(pets);
        }

        // ==================== POST ====================

        /// <summary>
        /// Cadastra um novo pet vinculado a um Tutor.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Pet>> Create([FromBody] CreatePetDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pet = await _petService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
        }

        // ==================== PUT ====================

        /// <summary>
        /// Atualiza os dados de um pet existente.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> Update(int id, [FromBody] UpdatePetDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pet = await _petService.UpdateAsync(id, dto, ct);
            return Ok(pet);
        }

        // ==================== DELETE ====================

        /// <summary>
        /// Remove um pet pelo ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _petService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
