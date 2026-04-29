using Domain;
using Domain.Entities;
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
    public async Task<ICollection<Offers>> GetPartnersAsync(FinancialSegment target)
    {
        var query = target switch
        {
            FinancialSegment.HIGH => DbSet.OrderBy(p => p.FinancialSegment == FinancialSegment.HIGH ? 0 :
                p.FinancialSegment == FinancialSegment.MEDIUM ? 1 : 2),
            FinancialSegment.MEDIUM => DbSet.OrderBy(p => p.FinancialSegment == FinancialSegment.MEDIUM ? 0 :
                p.FinancialSegment == FinancialSegment.HIGH ? 1 : 2),
            _ => DbSet.OrderBy(p => p.FinancialSegment == FinancialSegment.LOW ? 0 :
                p.FinancialSegment == FinancialSegment.MEDIUM ? 1 : 2)
        };

        return await query
            .ThenByDescending(p => p.CashbackPercent)
            .ToListAsync();
    }
}

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<int?> GetUserIdByPhoneNumber(string phoneNumber)
    {
        var result = await DbSet.Where(u => u.PhoneNumber == phoneNumber)
            .FirstOrDefaultAsync();

        return result?.Id;
    }
}

public class LoyaltyProgramsRepository(AppDbContext context) 
    : Repository<LoyaltyPrograms>(context), ILoyaltyProgramsRepository;

public class TransactionRepository(AppDbContext context): Repository<Transaction>(context), ITransactionRepository
{
    public async Task<ICollection<Transaction>> GetByAccountIdsAsync(IEnumerable<int> accountIds)
    {
        return await DbSet
            .Where(t => accountIds.Contains(t.AccountId))
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
}