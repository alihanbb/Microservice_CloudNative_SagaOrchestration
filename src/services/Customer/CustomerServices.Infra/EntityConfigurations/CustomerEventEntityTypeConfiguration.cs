using CustomerServices.Infra.EventSourcing;

namespace CustomerServices.Infra.EntityConfigurations;

/// <summary>
/// EF Core configuration for CustomerEventEntity (Event Store)
/// </summary>
public class CustomerEventEntityTypeConfiguration : IEntityTypeConfiguration<CustomerEventEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEventEntity> builder)
    {
        builder.ToTable("customer_events", CustomerDbContext.DEFAULT_SCHEMA);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.CustomerId)
            .IsRequired();

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.EventData)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.OccurredOn)
            .IsRequired();

        builder.Property(e => e.StoredAt)
            .IsRequired();

        // Indexes for querying events
        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("IX_CustomerEvents_CustomerId");

        builder.HasIndex(e => new { e.CustomerId, e.Version })
            .IsUnique()
            .HasDatabaseName("IX_CustomerEvents_CustomerId_Version");

        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("IX_CustomerEvents_EventId");

        builder.HasIndex(e => e.OccurredOn)
            .HasDatabaseName("IX_CustomerEvents_OccurredOn");
    }
}
