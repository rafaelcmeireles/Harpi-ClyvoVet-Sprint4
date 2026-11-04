using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ClyvoVetApi.Models
{
    /// <summary>
    /// Representa uma consulta veterinária, vinculando um Pet, seu Tutor e o Veterinário responsável.
    /// </summary>
    [Table("CONSULTATIONS")]
    public class Consultation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("PET_ID")]
        public int PetId { get; set; }

        [JsonIgnore]
        [ForeignKey("PetId")]
        public Pet? Pet { get; set; }

        [Required]
        [Column("VET_ID")]
        public int VetId { get; set; }

        [JsonIgnore]
        [ForeignKey("VetId")]
        public User? Vet { get; set; }

        [Required(ErrorMessage = "A data da consulta é obrigatória.")]
        [Column("CONSULTATION_DATE")]
        public DateTime ConsultationDate { get; set; }

        [StringLength(500)]
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Column("PRESCRIPTION")]
        public string? Prescription { get; set; }

        [StringLength(30)]
        [Column("STATUS")]
        public string Status { get; set; } = "Agendada"; // Agendada, Realizada, Cancelada

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}