using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Data
{
    /// <summary>
    /// Contexto do Entity Framework Core para acesso ao banco de dados Oracle.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Consultation> Consultations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasMaxLength(20);
            });

            // Configuração da entidade Pet
            modelBuilder.Entity<Pet>(entity =>
            {
                entity.HasOne(p => p.Tutor)
                      .WithMany(u => u.Pets)
                      .HasForeignKey(p => p.TutorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração da entidade Consultation
            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.HasOne(c => c.Pet)
                      .WithMany()
                      .HasForeignKey(c => c.PetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Vet)
                      .WithMany()
                      .HasForeignKey(c => c.VetId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}