using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs
{
    // ======================== USER DTOs ========================

    /// <summary>DTO para criação de um novo usuário.</summary>
    public class CreateUserDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O papel (Role) é obrigatório. Use 'Tutor' ou 'Veterinario'.")]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(14)]
        public string? Cpf { get; set; }

        [StringLength(10)]
        public string? Crmv { get; set; }

        /// <summary>Senha de acesso (opcional no cadastro; necessária para fazer login).</summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        public string? Password { get; set; }
    }

    /// <summary>DTO para atualização de um usuário existente.</summary>
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(14)]
        public string? Cpf { get; set; }

        [StringLength(10)]
        public string? Crmv { get; set; }
    }

    // ======================== PET DTOs ========================

    /// <summary>DTO para criação de um novo pet.</summary>
    public class CreatePetDto
    {
        [Required(ErrorMessage = "O nome do pet é obrigatório.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A espécie é obrigatória (ex: Cachorro, Gato).")]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Breed { get; set; }

        public DateTime? BirthDate { get; set; }

        public double? WeightKg { get; set; }

        [Required(ErrorMessage = "O ID do tutor é obrigatório.")]
        public int TutorId { get; set; }
    }

    /// <summary>DTO para atualização de um pet existente.</summary>
    public class UpdatePetDto
    {
        [Required(ErrorMessage = "O nome do pet é obrigatório.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A espécie é obrigatória.")]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Breed { get; set; }

        public DateTime? BirthDate { get; set; }

        public double? WeightKg { get; set; }
    }

    // ======================== CONSULTATION DTOs ========================

    /// <summary>DTO para criação de uma nova consulta.</summary>
    public class CreateConsultationDto
    {
        [Required(ErrorMessage = "O ID do pet é obrigatório.")]
        public int PetId { get; set; }

        [Required(ErrorMessage = "O ID do veterinário é obrigatório.")]
        public int VetId { get; set; }

        [Required(ErrorMessage = "A data da consulta é obrigatória.")]
        public DateTime ConsultationDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Prescription { get; set; }
    }

    /// <summary>DTO para atualização de uma consulta existente.</summary>
    public class UpdateConsultationDto
    {
        [Required(ErrorMessage = "A data da consulta é obrigatória.")]
        public DateTime ConsultationDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Prescription { get; set; }

        [StringLength(30)]
        public string? Status { get; set; }
    }
}