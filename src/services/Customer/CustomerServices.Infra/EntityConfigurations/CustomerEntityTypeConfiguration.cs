namespace CustomerServices.Infra.EntityConfigurations;

/// <summary>
/// EF Core configuration for Customer entity
/// Maps aggregate root with value objects
/// </summary>
public class CustomerEntityTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", CustomerDbContext.DEFAULT_SCHEMA);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .UseHiLo("customerseq", CustomerDbContext.DEFAULT_SCHEMA);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);

        // CustomerName Value Object (Owned Entity)
        builder.OwnsOne(c => c.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            nameBuilder.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Email Value Object (Owned Entity)
        builder.OwnsOne(c => c.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            emailBuilder.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Customers_Email");
        });

        // Phone Value Object (Owned Entity - Optional)
        builder.OwnsOne(c => c.Phone, phoneBuilder =>
        {
            phoneBuilder.Property(p => p.CountryCode)
                .HasColumnName("PhoneCountryCode")
                .HasMaxLength(5);

            phoneBuilder.Property(p => p.Number)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20);
        });

        // Address Value Object (Owned Entity - Optional)
        builder.OwnsOne(c => c.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("Street")
                .HasMaxLength(200);

            addressBuilder.Property(a => a.City)
                .HasColumnName("City")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.State)
                .HasColumnName("State")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("Country")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.ZipCode)
                .HasColumnName("ZipCode")
                .HasMaxLength(20);
        });

        // CustomerStatus Value Object (Owned Entity)
        builder.OwnsOne(c => c.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("StatusId")
                .IsRequired();

            statusBuilder.Property(s => s.Name)
                .HasColumnName("StatusName")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Scalar properties
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.VerifiedAt);

        builder.Property(c => c.DeletedAt);

        builder.Property(c => c.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Customers_CreatedAt");

        // Query filter for soft delete
        builder.HasQueryFilter(c => c.Status.Id != 4); // 4 = Deleted
    }
}
