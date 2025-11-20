using AgroManagement.Models;

namespace AgroManagement.Repository
{
    public interface IAnimalRepository : IBaseRepository<Animal>
    {
        Task<List<Animal>> GetAllAnimalsAsync(string searchTerm, int page = 1, int size = 5);
    }
}
