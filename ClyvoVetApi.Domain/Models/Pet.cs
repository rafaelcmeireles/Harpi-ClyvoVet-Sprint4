using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ClyvoVetApi.Models
{
    /// <summary>
    /// Representa um animal de estimação vinculado a um Tutor.
    /// </summary>
    [Table("PETS")]
    public class Pet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do pet é obrigatório.")]
        [StringLength(100)]
        [Column("NAME")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A espécie é obrigatória (ex: Cachorro, Gato).")]
        [StringLength(50)]
        [Column("SPECIES")]
        public string Species { get; set; } = string.Empty;

        [StringLength(80)]
        [Column("BREED")]
        public string? Breed { get; set; }

        [Column("BIRTH_DATE")]
        public DateTime? BirthDate { get; set; }

        [Column("WEIGHT_KG")]
        public double? WeightKg { get; set; }

        [Required]
        [Column("TUTOR_ID")]
        public int TutorId { get; set; }

        [JsonIgnore]
        [ForeignKey("TutorId")]
        public User? Tutor { get; set; }

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}