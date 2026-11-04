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
    /// Casos de uso relacionados a Pets.
    /// </summary>
    public interface IPetService
    {
        Task<List<Pet>> GetAllAsync(CancellationToken ct = default);
        Task<Pet> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Pet>> GetByTutorAsync(int tutorId, CancellationToken ct = default);
        Task<List<Pet>> GetBySpeciesAsync(string species, CancellationToken ct = default);
        Task<List<Pet>> GetByBreedAsync(string breed, CancellationToken ct = default);
        Task<List<Pet>> SearchByNameAsync(string name, CancellationToken ct = default);
        Task<PagedResult<Pet>> GetPagedAsync(PetFilter filter, PageQuery page, CancellationToken ct = default);
        Task<Pet> CreateAsync(CreatePetDto dto, CancellationToken ct = default);
        Task<Pet> UpdateAsync(int id, UpdatePetDto dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }

    public class PetService : IPetService
    {
        private readonly IPetRepository _pets;
        private readonly IUserRepository _users;
        private readonly IDateTimeProvider _clock;

        public PetService(IPetRepository pets, IUserRepository users, IDateTimeProvider clock)
        {
            _pets = pets;
            _users = users;
            _clock = clock;
        }

        public static readonly string[] SortableFields = { "id", "name", "species", "breed", "weightKg" };

        public Task<List<Pet>> GetAllAsync(CancellationToken ct = default) => _pets.GetAllAsync(ct);

        public Task<PagedResult<Pet>> GetPagedAsync(PetFilter filter, PageQuery page, CancellationToken ct = default)
        {
            SortValidator.Ensure(page, SortableFields);
            return _pets.GetPagedAsync(filter, page, ct);
        }

        public async Task<Pet> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var pet = await _pets.GetByIdAsync(id, ct);
            return pet ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");
        }

        public Task<List<Pet>> GetByTutorAsync(int tutorId, CancellationToken ct = default) =>
            _pets.GetByTutorAsync(tutorId, ct);

        public Task<List<Pet>> GetBySpeciesAsync(string species, CancellationToken ct = default) =>
            _pets.GetBySpeciesAsync(species, ct);

        public Task<List<Pet>> GetByBreedAsync(string breed, CancellationToken ct = default) =>
            _pets.GetByBreedAsync(breed, ct);

        public Task<List<Pet>> SearchByNameAsync(string name, CancellationToken ct = default) =>
            _pets.SearchByNameAsync(name, ct);

        public async Task<Pet> CreateAsync(CreatePetDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("PetService.Create");

            var tutor = await _users.GetByIdAsync(dto.TutorId, ct);
            if (tutor == null)
            {
                scope.MarkAsBusinessRuleViolation("tutor_not_found");
                throw new BusinessRuleException($"Tutor com ID {dto.TutorId} não encontrado.");
            }

            if (!UserRoles.IsTutor(tutor.Role))
            {
                scope.MarkAsBusinessRuleViolation("not_a_tutor");
                throw new BusinessRuleException("O usuário informado não é um Tutor.");
            }

            if (!PetValidationRules.IsBirthDateValid(dto.BirthDate, _clock.UtcNow))
            {
                scope.MarkAsBusinessRuleViolation("invalid_birth_date");
                throw new BusinessRuleException("A data de nascimento não pode ser no futuro.");
            }

            if (!PetValidationRules.IsWeightValid(dto.WeightKg))
            {
                scope.MarkAsBusinessRuleViolation("invalid_weight");
                throw new BusinessRuleException("O peso deve ser maior que zero.");
            }

            var pet = new Pet
            {
                Name = dto.Name,
                Species = dto.Species,
                Breed = dto.Breed,
                BirthDate = dto.BirthDate,
                WeightKg = dto.WeightKg,
                TutorId = dto.TutorId,
                CreatedAt = _clock.UtcNow
            };

            await _pets.AddAsync(pet, ct);
            await _pets.SaveChangesAsync(ct);

            return pet;
        }

        public async Task<Pet> UpdateAsync(int id, UpdatePetDto dto, CancellationToken ct = default)
        {
            using var scope = ClyvoTelemetry.StartOperation("PetService.Update");

            var pet = await _pets.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");

            if (!PetValidationRules.IsBirthDateValid(dto.BirthDate, _clock.UtcNow))
            {
                scope.MarkAsBusinessRuleViolation("invalid_birth_date");
                throw new BusinessRuleException("A data de nascimento não pode ser no futuro.");
            }

            if (!PetValidationRules.IsWeightValid(dto.WeightKg))
            {
                scope.MarkAsBusinessRuleViolation("invalid_weight");
                throw new BusinessRuleException("O peso deve ser maior que zero.");
            }

            pet.Name = dto.Name;
            pet.Species = dto.Species;
            pet.Breed = dto.Breed;
            pet.BirthDate = dto.BirthDate;
            pet.WeightKg = dto.WeightKg;

            await _pets.SaveChangesAsync(ct);
            return pet;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var pet = await _pets.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");

            await _pets.DeleteAsync(pet, ct);
            await _pets.SaveChangesAsync(ct);
        }
    }
}
