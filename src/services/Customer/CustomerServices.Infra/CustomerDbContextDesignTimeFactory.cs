using Microsoft.EntityFrameworkCore.Design;

namespace CustomerServices.Infra;

public class CustomerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
        
        optionsBuilder.UseSqlServer(
            "Server=localhost,1473;Database=CustomerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.DEFAULT_SCHEMA);
            });

        return new CustomerDbContext(optionsBuilder.Options);
    }
}
