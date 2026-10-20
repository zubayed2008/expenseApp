using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core mapping for the schema defined by the numbered SQL migrations in
/// ExpenseTracker.Domain (snake_case table/column names). No EF migrations
/// are generated — the SQL scripts are the source of truth for the schema.
/// </summary>
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
        entity.ToTable("users");

        entity.HasKey(user => user.Id);

        entity.Property(user => user.Id).HasColumnName("id");
        entity.Property(user => user.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
        entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(200);
        entity.Property(user => user.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);
        entity.Property(user => user.CreatedAt).HasColumnName("created_at");
        entity.Property(user => user.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(user => user.Email).HasDatabaseName("ix_users_email").IsUnique();
    }

    private static void ConfigureCategory(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("categories");

        entity.HasKey(category => category.Id);

        entity.Property(category => category.Id).HasColumnName("id");
        entity.Property(category => category.UserId).HasColumnName("user_id");
        entity.Property(category => category.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        entity.Property(category => category.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(10);
        entity.Property(category => category.CreatedAt).HasColumnName("created_at");

        entity.HasIndex(category => new { category.UserId, category.Kind, category.Name })
            .HasDatabaseName("ix_categories_user_id_kind_name")
            .IsUnique();

        entity.HasOne(category => category.User)
            .WithMany(user => user.Categories)
            .HasForeignKey(category => category.UserId)
            .HasConstraintName("fk_categories_user")
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTransaction(EntityTypeBuilder<Transaction> entity)
    {
        entity.ToTable("transactions");

        entity.HasKey(transaction => transaction.Id);

        entity.Property(transaction => transaction.Id).HasColumnName("id");
        entity.Property(transaction => transaction.UserId).HasColumnName("user_id");
        entity.Property(transaction => transaction.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(10);
        entity.Property(transaction => transaction.Date).HasColumnName("date");
        entity.Property(transaction => transaction.Amount).HasColumnName("amount").HasPrecision(18, 2);
        entity.Property(transaction => transaction.CategoryId).HasColumnName("category_id");
        entity.Property(transaction => transaction.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(transaction => transaction.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100);
        entity.Property(transaction => transaction.CreatedAt).HasColumnName("created_at");
        entity.Property(transaction => transaction.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(transaction => new { transaction.UserId, transaction.Date })
            .HasDatabaseName("ix_transactions_user_id_date");
        entity.HasIndex(transaction => new { transaction.UserId, transaction.Type })
            .HasDatabaseName("ix_transactions_user_id_type");
        entity.HasIndex(transaction => transaction.CategoryId)
            .HasDatabaseName("ix_transactions_category_id");

        entity.HasOne(transaction => transaction.User)
            .WithMany(user => user.Transactions)
            .HasForeignKey(transaction => transaction.UserId)
            .HasConstraintName("fk_transactions_user")
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(transaction => transaction.Category)
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .HasConstraintName("fk_transactions_category")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
