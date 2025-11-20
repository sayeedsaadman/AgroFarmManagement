using AgroManagement.Data;
using AgroManagement.Repository;

namespace AgroManagement.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AgroContext _context;

        public UnitOfWork(AgroContext context)
        {
            _context = context;
            Animal = new AnimalRepository(_context);
        }

        public IAnimalRepository Animal { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
