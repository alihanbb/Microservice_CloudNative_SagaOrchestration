using Microsoft.EntityFrameworkCore;
using CustomerServices.Infra;
using CustomerServices.Domain.Aggregate;

var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
optionsBuilder.UseSqlServer(\"Server=localhost,1473;Database=CustomerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True\");

using var context = new CustomerDbContext(optionsBuilder.Options);

// Check existing
Console.WriteLine($\"Current count: {context.Customers.Count()}\");

// Create one test customer
var customer = Customer.Create(\"Test\", \"Seeder\", \"test.seeder@example.com\");
customer.Verify();

context.Customers.Add(customer);
var saved = await context.SaveChangesAsync();

Console.WriteLine($\"Saved: {saved}\");
Console.WriteLine($\"New count: {context.Customers.Count()}\");
