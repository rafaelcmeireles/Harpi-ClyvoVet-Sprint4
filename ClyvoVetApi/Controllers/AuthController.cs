using ClyvoVetApi.Application.Services;
using ClyvoVetApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers
{
    /// <summary>Autenticação: emite o token JWT usado nos demais endpoints.</summary>
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Autentica com e-mail e senha e retorna um token JWT (use no header
        /// <c>Authorization: Bearer {token}</c>).
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            var token = await _authService.LoginAsync(dto, ct);
            return Ok(token);
        }
    }
}