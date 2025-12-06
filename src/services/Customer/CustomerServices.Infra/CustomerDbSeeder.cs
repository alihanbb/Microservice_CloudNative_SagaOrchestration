using Microsoft.Extensions.Logging;

namespace CustomerServices.Infra;

public static class CustomerDbSeeder
{
    public static async Task SeedAsync(CustomerDbContext context, ILogger logger)
    {
        logger.LogInformation("Checking if seeding is needed...");
        
        var existingCount = await context.Customers.CountAsync();
        if (existingCount > 0)
        {
            logger.LogInformation("Customer database already seeded - {Count} customers exist", existingCount);
            return;
        }

        logger.LogInformation("Starting Customer database seeding...");

        try
        {
            var customers = GenerateCustomers();
            logger.LogInformation("Generated {Count} customers for seeding", customers.Count);
            
            var successCount = 0;
            
            // Save each customer individually to work around EF Core owned entity tracking
            foreach (var customer in customers)
            {
                try
                {
                    context.Customers.Add(customer);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear(); // Clear tracker for next insert
                    successCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to seed customer {Email}", customer.Email.Value);
                    context.ChangeTracker.Clear();
                }
            }

            logger.LogInformation("Successfully seeded {Count}/{Total} customers", successCount, customers.Count);
            
            // Log customer distribution
            LogSeedingDetails(customers, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding Customer database: {Message}", ex.Message);
            throw;
        }
    }

    private static List<Customer> GenerateCustomers()
    {
        var customers = new List<Customer>();

        
        // Customer 1 - Premium customer from USA
        var customer1 = Customer.CreateWithDetails(
            "John", "Doe",
            "john.doe@example.com",
            "+1", "5551234567",
            "123 Main Street", "New York", "NY", "USA", "10001");
        customer1.Verify();
        customers.Add(customer1);

        // Customer 2 - Premium customer from UK
        var customer2 = Customer.CreateWithDetails(
            "Emma", "Watson",
            "emma.watson@gmail.com",
            "+44", "7700900123",
            "10 Downing Street", "London", "Greater London", "United Kingdom", "SW1A 2AA");
        customer2.Verify();
        customers.Add(customer2);

        // Customer 3 - Business customer from Germany
        var customer3 = Customer.CreateWithDetails(
            "Hans", "Mueller",
            "hans.mueller@business.de",
            "+49", "1712345678",
            "Unter den Linden 77", "Berlin", "Berlin", "Germany", "10117");
        customer3.Verify();
        customers.Add(customer3);

        // Customer 4 - Premium customer from France
        var customer4 = Customer.CreateWithDetails(
            "Marie", "Dupont",
            "marie.dupont@orange.fr",
            "+33", "612345678",
            "15 Avenue des Champs-Elysees", "Paris", "Ile-de-France", "France", "75008");
        customer4.Verify();
        customers.Add(customer4);

        // Customer 5 - Tech industry customer from Japan
        var customer5 = Customer.CreateWithDetails(
            "Yuki", "Tanaka",
            "yuki.tanaka@tech.jp",
            "+81", "9012345678",
            "1-1-1 Shibuya", "Tokyo", "Tokyo", "Japan", "150-0002");
        customer5.Verify();
        customers.Add(customer5);

        // Customer 6 - Customer from Turkey
        var customer6 = Customer.CreateWithDetails(
            "Ahmet", "Yilmaz",
            "ahmet.yilmaz@email.com.tr",
            "+90", "5321234567",
            "Bagdat Caddesi No:123", "Istanbul", "Kadikoy", "Turkey", "34740");
        customer6.Verify();
        customers.Add(customer6);

        // Customer 7 - Customer from Brazil
        var customer7 = Customer.CreateWithDetails(
            "Carlos", "Silva",
            "carlos.silva@hotmail.com.br",
            "+55", "11987654321",
            "Avenida Paulista 1000", "Sao Paulo", "SP", "Brazil", "01310-100");
        customer7.Verify();
        customers.Add(customer7);

        // Customer 8 - Customer from Australia
        var customer8 = Customer.CreateWithDetails(
            "Sarah", "Johnson",
            "sarah.johnson@outlook.com.au",
            "+61", "412345678",
            "42 George Street", "Sydney", "NSW", "Australia", "2000");
        customer8.Verify();
        customers.Add(customer8);

        // Customer 9 - Customer from Canada
        var customer9 = Customer.CreateWithDetails(
            "Michael", "Brown",
            "michael.brown@yahoo.ca",
            "+1", "4161234567",
            "100 Queen Street West", "Toronto", "ON", "Canada", "M5H 2N2");
        customer9.Verify();
        customers.Add(customer9);

        // Customer 10 - Customer from India
        var customer10 = Customer.CreateWithDetails(
            "Priya", "Sharma",
            "priya.sharma@gmail.com",
            "+91", "9876543210",
            "MG Road, Sector 14", "Mumbai", "Maharashtra", "India", "400001");
        customer10.Verify();
        customers.Add(customer10);

        // ============================================
        // ACTIVE CUSTOMERS WITH MINIMAL INFO
        // Only basic info without phone/address
        // ============================================
        
        // Customer 11 - Basic profile
        var customer11 = Customer.Create("Robert", "Williams", "robert.williams@email.com");
        customer11.Verify();
        customers.Add(customer11);

        // Customer 12 - Basic profile
        var customer12 = Customer.Create("Jennifer", "Davis", "jennifer.davis@email.com");
        customer12.Verify();
        customers.Add(customer12);

        // Customer 13 - Basic profile
        var customer13 = Customer.Create("David", "Miller", "david.miller@company.com");
        customer13.Verify();
        customers.Add(customer13);

        // Customer 14 - Basic profile
        var customer14 = Customer.Create("Lisa", "Anderson", "lisa.anderson@mail.com");
        customer14.Verify();
        customers.Add(customer14);

        // Customer 15 - Basic profile
        var customer15 = Customer.Create("James", "Taylor", "james.taylor@work.org");
        customer15.Verify();
        customers.Add(customer15);

        // ============================================
        // PENDING VERIFICATION CUSTOMERS
        // Recently registered, awaiting email verification
        // ============================================
        
        // Customer 16 - Pending verification
        var customer16 = Customer.CreateWithDetails(
            "Sofia", "Rodriguez",
            "sofia.rodriguez@newmail.com",
            "+1", "3051234567",
            "456 Ocean Drive", "Miami", "FL", "USA", "33139");
        customers.Add(customer16);

        // Customer 17 - Pending verification
        var customer17 = Customer.Create("Alex", "Thompson", "alex.thompson@email.net");
        customers.Add(customer17);

        // Customer 18 - Pending verification with phone only
        var customer18 = Customer.Create("Maria", "Garcia", "maria.garcia@correo.es");
        customers.Add(customer18);

        // Customer 19 - Pending verification
        var customer19 = Customer.Create("Oliver", "Martin", "oliver.martin@proton.me");
        customers.Add(customer19);

        // Customer 20 - Pending verification from Italy
        var customer20 = Customer.CreateWithDetails(
            "Giuseppe", "Romano",
            "giuseppe.romano@libero.it",
            "+39", "3201234567",
            "Via Roma 50", "Rome", "Lazio", "Italy", "00184");
        customers.Add(customer20);

        // ============================================
        // INACTIVE CUSTOMERS
        // Deactivated accounts
        // ============================================
        
        // Customer 21 - Inactive customer
        var customer21 = Customer.CreateWithDetails(
            "William", "Clark",
            "william.clark@oldmail.com",
            "+1", "2125551234",
            "789 Park Avenue", "New York", "NY", "USA", "10021");
        customer21.Verify();
        customer21.Deactivate("User requested account deactivation");
        customers.Add(customer21);

        // Customer 22 - Inactive customer
        var customer22 = Customer.Create("Elizabeth", "Wright", "elizabeth.wright@mail.com");
        customer22.Verify();
        customer22.Deactivate("Account inactivity for 12 months");
        customers.Add(customer22);

        // Customer 23 - Inactive customer from Spain
        var customer23 = Customer.CreateWithDetails(
            "Pablo", "Hernandez",
            "pablo.hernandez@email.es",
            "+34", "623456789",
            "Calle Gran Via 100", "Madrid", "Madrid", "Spain", "28013");
        customer23.Verify();
        customer23.Deactivate("Migrated to new account");
        customers.Add(customer23);

        // ============================================
        // SUSPENDED CUSTOMERS
        // Accounts under review
        // ============================================
        
        // Customer 24 - Suspended for suspicious activity
        var customer24 = Customer.CreateWithDetails(
            "Ivan", "Petrov",
            "ivan.petrov@email.ru",
            "+7", "9161234567",
            "Tverskaya Street 10", "Moscow", "Moscow", "Russia", "125009");
        customer24.Verify();
        customer24.Suspend("Suspicious activity detected - under review");
        customers.Add(customer24);

        // Customer 25 - Suspended for policy violation
        var customer25 = Customer.Create("Anonymous", "User", "anon.user@temp.com");
        customer25.Verify();
        customer25.Suspend("Terms of service violation - multiple chargebacks");
        customers.Add(customer25);

        // ============================================
        // VIP/ENTERPRISE CUSTOMERS
        // Full profiles with complete information
        // ============================================
        
        // Customer 26 - VIP Enterprise from Singapore
        var customer26 = Customer.CreateWithDetails(
            "Wei", "Chen",
            "wei.chen@enterprise.sg",
            "+65", "91234567",
            "1 Raffles Place", "Singapore", "Central", "Singapore", "048616");
        customer26.Verify();
        customers.Add(customer26);

        // Customer 27 - VIP Enterprise from UAE
        var customer27 = Customer.CreateWithDetails(
            "Ahmed", "AlRashid",
            "ahmed.alrashid@business.ae",
            "+971", "501234567",
            "Business Bay Tower 1", "Dubai", "Dubai", "UAE", "00000");
        customer27.Verify();
        customers.Add(customer27);

        // Customer 28 - VIP Enterprise from Switzerland
        var customer28 = Customer.CreateWithDetails(
            "Stefan", "Berger",
            "stefan.berger@swiss.ch",
            "+41", "791234567",
            "Bahnhofstrasse 21", "Zurich", "Zurich", "Switzerland", "8001");
        customer28.Verify();
        customers.Add(customer28);

        // Customer 29 - VIP from Netherlands
        var customer29 = Customer.CreateWithDetails(
            "Pieter", "VanDerBerg",
            "pieter.vanderberg@mail.nl",
            "+31", "612345678",
            "Keizersgracht 100", "Amsterdam", "North Holland", "Netherlands", "1015 CV");
        customer29.Verify();
        customers.Add(customer29);

        // Customer 30 - VIP from South Korea
        var customer30 = Customer.CreateWithDetails(
            "MinJun", "Kim",
            "minjun.kim@company.kr",
            "+82", "1012345678",
            "Gangnam-daero 123", "Seoul", "Seoul", "South Korea", "06000");
        customer30.Verify();
        customers.Add(customer30);

        // ============================================
        // CUSTOMERS WITH UPDATED INFORMATION
        // Profiles that have been modified
        // ============================================
        
        // Customer 31 - Customer with name update
        var customer31 = Customer.CreateWithDetails(
            "Anna", "Smith",
            "anna.smith@email.com",
            "+1", "4155551234",
            "555 Market Street", "San Francisco", "CA", "USA", "94105");
        customer31.Verify();
        customer31.UpdateName("AnnaMarie", "SmithJohnson");
        customers.Add(customer31);

        // Customer 32 - Customer with address update
        var customer32 = Customer.CreateWithDetails(
            "Thomas", "Lee",
            "thomas.lee@gmail.com",
            "+1", "6505551234",
            "100 Main Street", "Palo Alto", "CA", "USA", "94301");
        customer32.Verify();
        customer32.ChangeAddress("200 University Avenue", "Palo Alto", "CA", "USA", "94301");
        customers.Add(customer32);

        // Customer 33 - Customer with phone update
        var customer33 = Customer.CreateWithDetails(
            "Rachel", "Green",
            "rachel.green@company.com",
            "+1", "2125559999",
            "90 Bedford Street", "New York", "NY", "USA", "10014");
        customer33.Verify();
        customer33.ChangePhone("+1", "2125558888");
        customers.Add(customer33);

        // ============================================
        // INTERNATIONAL DIVERSE CUSTOMERS
        // Various countries and cultures
        // ============================================
        
        // Customer 34 - Customer from Poland
        var customer34 = Customer.CreateWithDetails(
            "Malgorzata", "Kowalski",
            "malgorzata.kowalski@wp.pl",
            "+48", "501234567",
            "ul. Marszalkowska 100", "Warsaw", "Mazowieckie", "Poland", "00-001");
        customer34.Verify();
        customers.Add(customer34);

        // Customer 35 - Customer from Mexico
        var customer35 = Customer.CreateWithDetails(
            "Fernando", "Lopez",
            "fernando.lopez@email.mx",
            "+52", "5512345678",
            "Paseo de la Reforma 500", "Mexico City", "CDMX", "Mexico", "06600");
        customer35.Verify();
        customers.Add(customer35);

        // Customer 36 - Customer from Argentina
        var customer36 = Customer.CreateWithDetails(
            "Lucia", "Fernandez",
            "lucia.fernandez@gmail.com.ar",
            "+54", "1134567890",
            "Avenida 9 de Julio 1000", "Buenos Aires", "CABA", "Argentina", "C1043AAZ");
        customer36.Verify();
        customers.Add(customer36);

        // Customer 37 - Customer from Sweden
        var customer37 = Customer.CreateWithDetails(
            "Erik", "Johansson",
            "erik.johansson@mail.se",
            "+46", "701234567",
            "Drottninggatan 50", "Stockholm", "Stockholm", "Sweden", "111 21");
        customer37.Verify();
        customers.Add(customer37);

        // Customer 38 - Customer from Norway
        var customer38 = Customer.CreateWithDetails(
            "Ingrid", "Hansen",
            "ingrid.hansen@online.no",
            "+47", "91234567",
            "Karl Johans gate 20", "Oslo", "Oslo", "Norway", "0154");
        customer38.Verify();
        customers.Add(customer38);

        // Customer 39 - Customer from Greece
        var customer39 = Customer.CreateWithDetails(
            "Nikos", "Papadopoulos",
            "nikos.papadopoulos@email.gr",
            "+30", "6971234567",
            "Ermou Street 100", "Athens", "Attica", "Greece", "105 63");
        customer39.Verify();
        customers.Add(customer39);

        // Customer 40 - Customer from Portugal
        var customer40 = Customer.CreateWithDetails(
            "Joao", "Santos",
            "joao.santos@sapo.pt",
            "+351", "912345678",
            "Avenida da Liberdade 200", "Lisbon", "Lisboa", "Portugal", "1250-147");
        customer40.Verify();
        customers.Add(customer40);

        // ============================================
        // CUSTOMERS FOR TESTING EDGE CASES
        // ============================================
        
        // Customer 41 - Long names
        var customer41 = Customer.CreateWithDetails(
            "AlexanderChristopher", "MontgomeryWilliamson",
            "alexander.montgomery@longemaildomain.com",
            "+1", "8001234567",
            "12345 Very Long Street Name Boulevard", "Los Angeles", "CA", "USA", "90001");
        customer41.Verify();
        customers.Add(customer41);

        // Customer 42 - Unicode name (simplified for EF compatibility)
        var customer42 = Customer.CreateWithDetails(
            "Bjork", "Gudmundsdottir",
            "bjork.g@iceland.is",
            "+354", "6123456",
            "Laugavegur 50", "Reykjavik", "Capital Region", "Iceland", "101");
        customer42.Verify();
        customers.Add(customer42);

        // Customer 43 - Minimal email domain
        var customer43 = Customer.Create("Test", "User", "test@t.co");
        customer43.Verify();
        customers.Add(customer43);

        // Customer 44 - Plus addressing email
        var customer44 = Customer.Create("Plus", "Address", "user+tag@example.com");
        customer44.Verify();
        customers.Add(customer44);

        // Customer 45 - Subdomain email
        var customer45 = Customer.Create("Subdomain", "Test", "user@mail.subdomain.example.com");
        customer45.Verify();
        customers.Add(customer45);

        // ============================================
        // RECENTLY REGISTERED BATCH
        // For testing pagination and sorting
        // ============================================
        
        var customer46 = Customer.Create("Alice", "Wonder", "alice.wonder@email.com");
        customers.Add(customer46);

        var customer47 = Customer.Create("Bob", "Builder", "bob.builder@email.com");
        customers.Add(customer47);

        var customer48 = Customer.Create("Charlie", "Chocolate", "charlie.chocolate@email.com");
        customers.Add(customer48);

        var customer49 = Customer.Create("Diana", "Prince", "diana.prince@email.com");
        customers.Add(customer49);

        var customer50 = Customer.Create("Eve", "Online", "eve.online@email.com");
        customers.Add(customer50);

        return customers;
    }

    private static void LogSeedingDetails(List<Customer> customers, ILogger logger)
    {
        var activeCount = customers.Count(c => c.IsActive());
        var pendingCount = customers.Count(c => c.Status == CustomerStatus.PendingVerification);
        var inactiveCount = customers.Count(c => c.Status == CustomerStatus.Inactive);
        var suspendedCount = customers.Count(c => c.IsSuspended());

        logger.LogInformation("Customer Seeding Summary:");
        logger.LogInformation("   Active: {Count}", activeCount);
        logger.LogInformation("   Pending Verification: {Count}", pendingCount);
        logger.LogInformation("   Inactive: {Count}", inactiveCount);
        logger.LogInformation("   Suspended: {Count}", suspendedCount);
        logger.LogInformation("   Total: {Count}", customers.Count);

        var withAddress = customers.Count(c => c.Address != null);
        var withPhone = customers.Count(c => c.Phone != null);
        
        logger.LogInformation("Profile Completeness:");
        logger.LogInformation("   With Address: {Count}", withAddress);
        logger.LogInformation("   With Phone: {Count}", withPhone);
        logger.LogInformation("   Full Profile: {Count}", customers.Count(c => c.Address != null && c.Phone != null));

        // Log unique countries
        var countries = customers
            .Where(c => c.Address != null)
            .Select(c => c.Address!.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
            
        logger.LogInformation("Countries Represented: {Countries}", string.Join(", ", countries));
    }
}
