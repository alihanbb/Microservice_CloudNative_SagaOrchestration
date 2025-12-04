namespace OrderServices.Infra;

public class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", OrderDbContext.DEFAULT_SCHEMA);

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .UseHiLo("orderseq", OrderDbContext.DEFAULT_SCHEMA);

        builder.Ignore(o => o.DomainEvents);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.OrderDate)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.OwnsOne(o => o.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("StatusId")
                .IsRequired();

            statusBuilder.Property(s => s.Name)
                .HasColumnName("StatusName")
                .HasMaxLength(50)
                .IsRequired();
        });

        var navigation = builder.Metadata.FindNavigation(nameof(Order.OrderItems));
        
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(o => o.CustomerId)
            .HasDatabaseName("IX_Orders_CustomerId");

        builder.HasIndex(o => o.OrderDate)
            .HasDatabaseName("IX_Orders_OrderDate");
    }
}
