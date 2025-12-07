namespace CustomerServices.Infra.EntityConfigurations;

public class CustomerSnapshotEntityTypeConfiguration : IEntityTypeConfiguration<CustomerSnapshot>
{
    public void Configure(EntityTypeBuilder<CustomerSnapshot> builder)
    {
        builder.ToTable("customer_snapshots", CustomerDbContext.DEFAULT_SCHEMA);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .UseIdentityColumn();

        builder.Property(s => s.CustomerId)
            .IsRequired();

        builder.Property(s => s.Version)
            .IsRequired();

        builder.Property(s => s.SnapshotData)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => new { s.CustomerId, s.Version })
            .HasDatabaseName("IX_CustomerSnapshots_CustomerId_Version");
    }
}
