using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AgroManagement.Data;

namespace AgroManagement.Helper
{
    public class DbHelper
    {
        private readonly AgroContext _context;

        public DbHelper(AgroContext context)
        {
            _context = context;
        }

        public async Task<List<T>> ExecuteSPAsync<T>(string procedureName, params SqlParameter[] parameters) where T : class
        {
            try
            {
                string paramNames = string.Join(", ", parameters.Select(p => p.ParameterName));
                string sql = $"EXEC {procedureName} {paramNames}";

                return await _context.Set<T>()
                                     .FromSqlRaw(sql, parameters)
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing stored procedure '{procedureName}': {ex.Message}", ex);
            }
        }

    }
}
