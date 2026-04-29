using Domain;

namespace Infrastructure.Repositories.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<ICollection<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> SaveChangesAsync();
}

public interface IUserRepository : IRepository<User>
{
}

public interface IAccountRepository : IRepository<Accounts> 
{
    Task<ICollection<Accounts>> GetByUserIdAsync(Guid userId);
}

public interface ILoyaltyHistoryRepository : IRepository<LoyaltyHistory> 
{
    Task<ICollection<LoyaltyHistory>> GetByAccountIdsAsync(IEnumerable<Guid> accountIds);
}

public interface IOfferRepository : IRepository<Offers> 
{
    Task<ICollection<Offers>> GetBySegmentAsync(FinancialSegment segment);
}

public interface ILoyaltyProgramsRepository : IRepository<LoyaltyPrograms>
{
    
}