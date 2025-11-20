using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AgroManagement.Data;
using AgroManagement.Helper;
using AgroManagement.Models;

namespace AgroManagement.Repository
{
    public class AnimalRepository : BaseRepository<Animal>, IAnimalRepository
    {
        private readonly AgroContext _context;

        public AnimalRepository(AgroContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Animal>> GetAllAnimalsAsync(string searchTerm, int page = 1, int size = 5)
        {
            var dbHelper = new DbHelper(_context);

            var param1 = new SqlParameter("@Search", searchTerm);
            var param2 = new SqlParameter("@DisplayLength", size);
            var param3 = new SqlParameter("@DisplayStart", page);

            // Execute Stored Procedure (you will create this SP later)
            var animals = await dbHelper.ExecuteSPAsync<Animal>(
                "Get_All_Animals",       // your new SP name
                param1, param2, param3
            );

            return animals;
        }
    }
}
