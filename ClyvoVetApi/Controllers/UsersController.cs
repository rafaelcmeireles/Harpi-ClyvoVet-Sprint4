using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;

namespace ClyvoVetApi.Controllers
{
    /// <summary>
    /// Controller responsável pelo CRUD de Usuários (Tutores e Veterinários).
    /// Toda a regra de negócio vive em <see cref="IUserService"/>; o Controller apenas
    /// traduz HTTP em chamadas de caso de uso.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // ==================== GET ====================

        /// <summary>
        /// Lista usuários com paginação, ordenação e filtros.
        /// Ordenação (<c>sortBy</c>): id, name, email, role, createdAt.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<User>>> GetAll(
            [FromQuery] PageQuery paging,
            [FromQuery] string? role,
            [FromQuery] string? search,
            CancellationToken ct)
        {
            var result = await _userService.GetPagedAsync(new UserFilter(role, search), paging, ct);
            var filters = new Dictionary<string, string?> { ["role"] = role, ["search"] = search };
            return Ok(ToPaged(result, paging, "/api/users", filters, LinksFor));
        }

        /// <summary>
        /// Retorna um usuário pelo seu ID, com links HATEOAS.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Resource<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Resource<User>>> GetById(int id, CancellationToken ct)
        {
            var user = await _userService.GetByIdAsync(id, ct);
            return Ok(Wrap(user, LinksFor(user)));
        }

        private IEnumerable<Link> LinksFor(User user)
        {
            var self = Abs($"/api/users/{user.Id}");
            yield return new Link("self", self, "GET");
            yield return new Link("update", self, "PUT");
            yield return new Link("delete", self, "DELETE");

            if (string.Equals(user.Role, "Tutor", StringComparison.OrdinalIgnoreCase))
                yield return new Link("pets", Abs($"/api/pets?tutorId={user.Id}"), "GET");
            else
                yield return new Link("consultations", Abs($"/api/consultations?vetId={user.Id}"), "GET");
        }

        /// <summary>
        /// Retorna todos os usuários filtrados pelo papel (Role): 'Tutor' ou 'Veterinario'.
        /// </summary>
        [HttpGet("role/{role}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<User>>> GetByRole(string role, CancellationToken ct)
        {
            var users = await _userService.GetByRoleAsync(role, ct);
            return Ok(users);
        }

        /// <summary>
        /// Retorna todos os usuários cujo nome contenha o termo informado.
        /// </summary>
        [HttpGet("search/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<User>>> SearchByName(string name, CancellationToken ct)
        {
            var users = await _userService.SearchByNameAsync(name, ct);
            return Ok(users);
        }

        /// <summary>
        /// Retorna um usuário pelo e-mail.
        /// </summary>
        [HttpGet("email/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetByEmail(string email, CancellationToken ct)
        {
            var user = await _userService.GetByEmailAsync(email, ct);
            return Ok(user);
        }

        // ==================== POST ====================

        /// <summary>
        /// Cria um novo usuário (Tutor ou Veterinário).
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<User>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // ==================== PUT ====================

        /// <summary>
        /// Atualiza os dados de um usuário existente.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.UpdateAsync(id, dto, ct);
            return Ok(user);
        }

        // ==================== DELETE ====================

        /// <summary>
        /// Remove um usuário pelo ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _userService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
