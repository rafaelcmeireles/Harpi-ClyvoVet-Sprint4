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
    /// Casos de uso relacionados a Usuários (Tutores e Veterinários).
    /// </summary>
    public interface IUserService
    {
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task<User> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<User>> GetByRoleAsync(string role, CancellationToken ct = default);
        Task<List<User>> SearchByNameAsync(string name, CancellationToken ct = default);
        Task<User> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<PagedResult<User>> GetPagedAsync(UserFilter filter, PageQuery page, CancellationToken ct = default);
        Task<User> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
        Task<User> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IDateTimeProvider _clock;
        private readonly IPasswordHasher _hasher;

        public static readonly string[] SortableFields = { "id", "name", "email", "role", "createdAt" };

        public UserService(IUserRepository users, IDateTimeProvider clock, IPasswordHasher hasher)
        {
            _users = users;
            _clock = clock;
            _hasher = hasher;
        }

        public Task<PagedResult<User>> GetPagedAsync(UserFilter filter, PageQuery page, CancellationToken ct = default)
        {
            SortValidator.Ensure(page, SortableFields);

            if (!string.IsNullOrWhiteSpace(filter.Role) && !UserRoles.IsValid(filter.Role))
                throw new BusinessRuleException("Role inválido. Use 'Tutor' ou 'Veterinario'.");

            return _users.GetPagedAsync(filter, page, ct);
        }

        public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
            _users.GetAllAsync(ct);

        public async Task<User> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(id, ct);
            return user ?? throw new NotFoundException($"Usuário com ID {id} não encontrado.");
        }

        public Task<List<User>> GetByRoleAsync(string role, CancellationToken ct = default) =>
            _users.GetByRoleAsync(role, ct);

        public Task<List<User>> SearchByNameAsync(string name, CancellationToken ct = default) =>
            _users.SearchByNameAsync(name, ct);

        public async Task<User> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var user = await _users.GetByEmailAsync(email, ct);
            return user ?? throw new NotFoundException($"Usuário com e-mail '{email}' não encontrado.");
        }

        public async Task<User> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("UserService.Create");

            if (!UserRoles.IsValid(dto.Role))
            {
                scope.MarkAsBusinessRuleViolation("invalid_role");
                throw new BusinessRuleException("Role inválido. Use 'Tutor' ou 'Veterinario'.");
            }

            if (await _users.EmailExistsAsync(dto.Email, ct: ct))
            {
                scope.MarkAsBusinessRuleViolation("duplicate_email");
                throw new BusinessRuleException("Já existe um usuário com este e-mail.");
            }

            if (!UserValidationRules.HasValidCrmv(dto.Role, dto.Crmv))
            {
                scope.MarkAsBusinessRuleViolation("missing_crmv");
                throw new BusinessRuleException("Veterinários precisam informar o CRMV.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                Phone = dto.Phone,
                Cpf = dto.Cpf,
                Crmv = dto.Crmv,
                PasswordHash = string.IsNullOrEmpty(dto.Password) ? null : _hasher.Hash(dto.Password),
                CreatedAt = _clock.UtcNow
            };

            await _users.AddAsync(user, ct);
            await _users.SaveChangesAsync(ct);

            return user;
        }

        public async Task<User> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("UserService.Update");

            var user = await _users.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Usuário com ID {id} não encontrado.");

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase) &&
                await _users.EmailExistsAsync(dto.Email, excludeUserId: id, ct: ct))
            {
                scope.MarkAsBusinessRuleViolation("duplicate_email");
                throw new BusinessRuleException("Já existe um usuário com este e-mail.");
            }

            var effectiveCrmv = dto.Crmv ?? user.Crmv;
            if (!UserValidationRules.HasValidCrmv(user.Role, effectiveCrmv))
            {
                scope.MarkAsBusinessRuleViolation("missing_crmv");
                throw new BusinessRuleException("Veterinários precisam informar o CRMV.");
            }

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.Cpf = dto.Cpf;
            user.Crmv = effectiveCrmv;

            await _users.SaveChangesAsync(ct);
            return user;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Usuário com ID {id} não encontrado.");

            await _users.DeleteAsync(user, ct);
            await _users.SaveChangesAsync(ct);
        }
    }
}
