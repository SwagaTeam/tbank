using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Accounts> Accounts => Set<Accounts>();
    public DbSet<LoyaltyHistory> LoyaltyHistories => Set<LoyaltyHistory>();
    public DbSet<LoyaltyPrograms> LoyaltyPrograms => Set<LoyaltyPrograms>();
    public DbSet<Offers> Offers => Set<Offers>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email).IsUnique();
            u.Property(x => x.FullName).IsRequired();
            
            u.Property(x => x.FinancialSegment).HasConversion<int>();

            u.HasMany<Accounts>()
                .WithOne()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Accounts>(a =>
        {
            a.HasKey(x => x.Id);
            a.HasIndex(x => x.UserId);
            
            a.Property(x => x.CurrentBalance).HasPrecision(18, 2);

            a.HasMany<LoyaltyHistory>()
                .WithOne()
                .HasForeignKey(h => h.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoyaltyHistory>(h =>
        {
            h.HasKey(x => x.Id);
            h.HasIndex(x => x.AccountId);
            h.HasIndex(x => x.PayoutDate); 
        });

        modelBuilder.Entity<LoyaltyPrograms>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.Name).HasConversion<string>();
            p.Property(x => x.Currency).HasConversion<string>();
        });

        modelBuilder.Entity<Offers>(o =>
        {
            o.HasKey(x => x.Id);
            o.HasIndex(x => x.FinancialSegment); 
            
            o.Property(x => x.CashbackPercent).HasPrecision(5, 2);
            o.Property(x => x.BrandColorHex).HasMaxLength(7); 
        });
    }
}