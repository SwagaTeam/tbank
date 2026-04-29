using Domain;

namespace Infrastructure.Repositories.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
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
    Task<int?> GetUserIdByPhoneNumber(string phoneNumber);
}

public interface IAccountRepository : IRepository<Accounts> 
{
    Task<ICollection<Accounts>> GetByUserIdAsync(int userId);
}

public interface ILoyaltyHistoryRepository : IRepository<LoyaltyHistory> 
{
    Task<ICollection<LoyaltyHistory>> GetByAccountIdsAsync(IEnumerable<int> accountIds);
}

public interface IOfferRepository : IRepository<Offers>
{
    Task<ICollection<Offers>> GetPartnersAsync(FinancialSegment target);
}

public interface ILoyaltyProgramsRepository : IRepository<LoyaltyPrograms>
{
    
}