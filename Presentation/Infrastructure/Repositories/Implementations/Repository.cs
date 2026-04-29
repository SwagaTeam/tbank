using Domain;
using Infrastructure.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Implementations;

public class Repository<T>(AppDbContext context) : IRepository<T>
    where T : class
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);
    public async Task<ICollection<T>> GetAllAsync() => await DbSet.ToListAsync();
    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);
    public async Task AddRangeAsync(IEnumerable<T> entities) => await DbSet.AddRangeAsync(entities);
    public void Update(T entity) => DbSet.Update(entity);
    public void Remove(T entity) => DbSet.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);
    public async Task<int> SaveChangesAsync() => await context.SaveChangesAsync();
}

public class AccountRepository(AppDbContext context) : Repository<Accounts>(context), IAccountRepository
{
    public async Task<ICollection<Accounts>> GetByUserIdAsync(int userId)
    {
        return await DbSet.Where(a => a.UserId == userId).ToListAsync();
    }
}

public class LoyaltyHistoryRepository(AppDbContext context)
    : Repository<LoyaltyHistory>(context), ILoyaltyHistoryRepository
{
    public async Task<ICollection<LoyaltyHistory>> GetByAccountIdsAsync(IEnumerable<int> accountIds)
    {
        return await DbSet
            .Where(h => accountIds.Contains(h.AccountId))
            .OrderByDescending(h => h.PayoutDate)
            .ToListAsync();
    }
}

public class OfferRepository(AppDbContext context) : Repository<Offers>(context), IOfferRepository
{
    public async Task<ICollection<Offers>> GetBySegmentAsync(FinancialSegment segment)
    {
        return await DbSet
            .Where(p => p.FinancialSegment <= segment) 
            .ToListAsync();
    }
}

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository;

public class LoyaltyProgramsRepository(AppDbContext context) 
    : Repository<LoyaltyPrograms>(context), ILoyaltyProgramsRepository;