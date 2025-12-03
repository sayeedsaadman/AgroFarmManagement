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

        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeTask> EmployeeTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Animal unique TagNumber
            modelBuilder.Entity<Animal>()
                .HasIndex(a => a.TagNumber)
                .IsUnique();

            // EmployeeTask -> Employee (EmployeeCode PK/FK)
            modelBuilder.Entity<EmployeeTask>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.EmployeeCode)
                .OnDelete(DeleteBehavior.Cascade);

            // EmployeeTask -> Animal
            modelBuilder.Entity<EmployeeTask>()
                .HasOne(t => t.Animal)
                .WithMany()
                .HasForeignKey(t => t.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
