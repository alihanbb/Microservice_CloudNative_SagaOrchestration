namespace OrderServices.Infra;

public class OrderItemEntityTypeConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("orderitems", OrderDbContext.DEFAULT_SCHEMA);

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .UseHiLo("orderitemseq", OrderDbContext.DEFAULT_SCHEMA);

        builder.Ignore(oi => oi.DomainEvents);

        builder.Property(oi => oi.ProductId)
            .IsRequired();

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(oi => oi.OrderId)
            .IsRequired();

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");
    }
}
