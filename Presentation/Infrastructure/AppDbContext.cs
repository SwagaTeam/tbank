using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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
            u.Property(x => x.Id).HasColumnName("id");
            u.Property(x => x.Email).HasColumnName("email").IsRequired();
            u.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            u.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
            
            u.Property(x => x.FinancialSegment)
                .HasColumnName("financial_segment")
                .HasConversion<string>();

            u.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Accounts>(a =>
        {
            a.HasKey(x => x.AccountId);
            a.Property(x => x.AccountId).HasColumnName("account_id");
            a.Property(x => x.UserId).HasColumnName("user_id");
            a.Property(x => x.LoyaltyProgramId).HasColumnName("loyalty_program_id");
            a.Property(x => x.CurrentBalance)
                .HasColumnName("current_balance")
                .HasPrecision(18, 2);

            a.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<LoyaltyHistory>(h =>
        {
            h.HasKey(x => x.TransactionId);
            h.Property(x => x.TransactionId).HasColumnName("transaction_id");
            h.Property(x => x.AccountId).HasColumnName("account_id");
            h.Property(x => x.CashbackAmount).HasColumnName("cashback_amount");
            h.Property(x => x.PayoutDate).HasColumnName("payout_date");

            h.HasIndex(x => x.AccountId);
            h.HasIndex(x => x.PayoutDate); 
        });

        modelBuilder.Entity<LoyaltyPrograms>(p =>
        {
            p.HasKey(x => x.LoyaltyProgramId);
            p.Property(x => x.LoyaltyProgramId).HasColumnName("loyalty_program_id");
            
            p.Property(x => x.LoyaltyProgramName).HasColumnName("loyalty_program_name").HasConversion<string>();
            p.Property(x => x.CashbackCurrency).HasColumnName("cashback_currency").HasConversion<string>();
        });

        modelBuilder.Entity<Offers>(o =>
        {
            o.HasKey(x => x.PartnerId);
            o.Property(x => x.PartnerId).HasColumnName("partner_id");
            o.Property(x => x.PartnerName).HasColumnName("partner_name");
            o.Property(x => x.ShortDescription).HasColumnName("short_description");
            o.Property(x => x.LogoUrl).HasColumnName("logo_url");
            o.Property(x => x.BrandColorHex).HasColumnName("brand_color_hex").HasMaxLength(7);
            o.Property(x => x.CashbackPercent).HasColumnName("cashback_percent").HasPrecision(5, 2);
            o.Property(x => x.FinancialSegment).HasColumnName("financial_segment").HasConversion<string>();

            o.HasIndex(x => x.FinancialSegment); 
        });

        modelBuilder.Entity<Accounts>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoyaltyHistory>()
            .HasOne<Accounts>()
            .WithMany()
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}