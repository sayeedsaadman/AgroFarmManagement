using Microsoft.EntityFrameworkCore;
using AgroManagement.Models;

namespace AgroManagement.Data
{
    public class AgroContext : DbContext
    {
        public AgroContext(DbContextOptions<AgroContext> options) : base(options)
        {
        }

        public DbSet<Animal> Animals { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Animal>()
                .HasIndex(a => a.TagNumber)
                .IsUnique();
        }

    }
}
