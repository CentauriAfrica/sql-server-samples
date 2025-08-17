using DiscountingEngine.Models;
using DiscountingEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiscountingEngine.Tests
{
    public class DiscountingEngineTestsFixed
    {
        private DiscountingContext CreateContext()
        {
            // SOLUTION 1: Use unique database names for each test
            var databaseName = $"DiscountingTestDb_{Guid.NewGuid()}";
            
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task InvoiceValueDiscount_WithOfferProduct_ShouldUseSellingPrice_Fixed()
        {
            // Arrange
            using var context = CreateContext();
            var service = new DiscountingService(context);

            var offerProduct = new OfferProduct
            {
                Id = 1,
                Name = "Premium Product",
                SellingPrice = 150
            };
            context.OfferProducts.Add(offerProduct);

            var invoice = new Invoice
            {
                Id = 1,
                Description = "Original Description",
                UnitPrice = 0,
                OfferProductId = 1
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CalculateInvoiceDiscountAsync(1);

            // Assert
            Assert.Equal(150, result.UnitPrice);
            Assert.Equal("Premium Product", result.Description);
        }

        [Fact]
        public async Task InvoiceValueDiscount_WithNullOfferProduct_ShouldResultInZeroUnitPriceAndGenericDescription_Fixed()
        {
            // Arrange
            using var context = CreateContext(); // Fresh, isolated context
            var service = new DiscountingService(context);

            var invoice = new Invoice
            {
                Id = 2,
                Description = "Original Description",
                UnitPrice = 100,
                OfferProductId = null // No OfferProduct
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CalculateInvoiceDiscountAsync(2);

            // Assert - This now passes because context is isolated
            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }
    }

    // SOLUTION 2: Alternative approach using TestFixture pattern
    public class DiscountingEngineTestsWithFixture : IDisposable
    {
        private readonly DiscountingContext _context;
        private readonly string _databaseName;

        public DiscountingEngineTestsWithFixture()
        {
            // Create a unique database for this test class
            _databaseName = $"DiscountingTestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            _context = new DiscountingContext(options);
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task FixtureApproach_WithCleanup_ShouldWork()
        {
            // Clean database before test
            _context.Invoices.RemoveRange(_context.Invoices);
            _context.OfferProducts.RemoveRange(_context.OfferProducts);
            await _context.SaveChangesAsync();

            var service = new DiscountingService(_context);

            var invoice = new Invoice
            {
                Id = 1,
                Description = "Test",
                UnitPrice = 100,
                OfferProductId = null
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var result = await service.CalculateInvoiceDiscountAsync(1);

            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // SOLUTION 3: Using test collections for proper isolation
    [CollectionDefinition("Database collection")]
    public class DatabaseTestCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class DatabaseFixture : IDisposable
    {
        public DiscountingContext CreateContext()
        {
            var databaseName = $"DiscountingTestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            // Cleanup code here if needed
        }
    }

    [Collection("Database collection")]
    public class DiscountingEngineTestsWithCollection
    {
        private readonly DatabaseFixture _fixture;

        public DiscountingEngineTestsWithCollection(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CollectionApproach_WithIsolation_ShouldWork()
        {
            using var context = _fixture.CreateContext();
            var service = new DiscountingService(context);

            var invoice = new Invoice
            {
                Id = 1,
                Description = "Test",
                UnitPrice = 100,
                OfferProductId = null
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            var result = await service.CalculateInvoiceDiscountAsync(1);

            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }
    }
}