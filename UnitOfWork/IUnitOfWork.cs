using AgroManagement.Repository;

namespace AgroManagement.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAnimalRepository Animal { get; }
        Task<int> CompleteAsync(); // Save changes
    }
}
