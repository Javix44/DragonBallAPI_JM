using DragonBallAPI_JM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        // Propiedades DbSet para las entidades "Character" y "Transformation", que representan las tablas en la base de datos
        public DbSet<Character> Characters { get; set; }
        public DbSet<Transformation> Transformations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            // Configuramos la relación entre "Character" y "Transformation"
            modelBuilder.Entity<Transformation>()
                .HasOne<Character>() 
                .WithMany(c => c.Transformations) 
                .HasForeignKey(t => t.CharacterId) // La clave foránea que enlaza con la tabla "Character"
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina un personaje, se eliminan las transformaciones asociadas
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Usamos la configuración de la cadena de conexión almacenada en "DefaultConnection" en appsettings.json
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
            }
        }
    }
}
