using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgroManagement.Data
{
    public class AgroContextFactory : IDesignTimeDbContextFactory<AgroContext>
    {
        public AgroContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgroContext>();

            // 🔥 PUT YOUR REAL SQL SERVER NAME HERE
            optionsBuilder.UseSqlServer(
                "Server=localhost\\SQLEXPRESS;Database=AgroManagementDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new AgroContext(optionsBuilder.Options);
        }
    }
}
