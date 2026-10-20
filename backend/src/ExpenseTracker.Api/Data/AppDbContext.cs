using ExpenseTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder.Entity<User>());
        ConfigureCategory(modelBuilder.Entity<Category>());
        ConfigureTransaction(modelBuilder.Entity<Transaction>());
    }

    private static void ConfigureUser(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(user => user.Id);

        entity.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        entity.HasIndex(user => user.Email)
            .IsUnique();

        entity.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(user => user.Currency)
            .IsRequired()
            .HasMaxLength(3);
    }

    private static void ConfigureCategory(EntityTypeBuilder<Category> entity)
    {
        entity.HasKey(category => category.Id);

        entity.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(category => category.Kind)
            .HasConversion<string>()
            .HasMaxLength(10);

        // One category name per kind per account.
        entity.HasIndex(category => new { category.UserId, category.Kind, category.Name })
            .IsUnique();

        entity.HasOne(category => category.User)
            .WithMany(user => user.Categories)
            .HasForeignKey(category => category.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTransaction(EntityTypeBuilder<Transaction> entity)
    {
        entity.HasKey(transaction => transaction.Id);

        entity.Property(transaction => transaction.Type)
            .HasConversion<string>()
            .HasMaxLength(10);

        // Money is numeric(18,2) — never floating point (C-2).
        entity.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2);

        entity.ToTable(table =>
            table.HasCheckConstraint("ck_transactions_amount_positive", "\"Amount\" > 0"));

        entity.Property(transaction => transaction.Description)
            .HasMaxLength(500);

        entity.Property(transaction => transaction.PaymentMethod)
            .HasMaxLength(100);

        // Common query paths: per-account listings and month filters (FR-226).
        entity.HasIndex(transaction => new { transaction.UserId, transaction.Date });
        entity.HasIndex(transaction => new { transaction.UserId, transaction.Type });

        entity.HasOne(transaction => transaction.User)
            .WithMany(user => user.Transactions)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(transaction => transaction.Category)
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
