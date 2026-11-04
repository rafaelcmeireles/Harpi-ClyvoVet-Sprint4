using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClyvoVetApi.Models
{
    /// <summary>
    /// Representa um usuário do sistema — pode ser um Tutor (dono do pet) ou um Veterinário.
    /// </summary>
    [Table("USERS")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        [Column("NAME")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150)]
        [Column("EMAIL")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O papel (Role) é obrigatório. Use 'Tutor' ou 'Veterinario'.")]
        [StringLength(20)]
        [Column("ROLE")]
        public string Role { get; set; } = string.Empty; // "Tutor" ou "Veterinario"

        [StringLength(20)]
        [Column("PHONE")]
        public string? Phone { get; set; }

        [StringLength(14)]
        [Column("CPF")]
        public string? Cpf { get; set; }

        [StringLength(10)]
        [Column("CRMV")]
        public string? Crmv { get; set; } // Somente para veterinários

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Hash PBKDF2 da senha. Nunca é serializado nas respostas da API.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [StringLength(300)]
        [Column("PASSWORD_HASH")]
        public string? PasswordHash { get; set; }

        // Navegação: um Tutor pode ter vários Pets
        public ICollection<Pet>? Pets { get; set; }
    }
}