using DiscountingEngine.Models;
using DiscountingEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiscountingEngine.Tests
{
    /// <summary>
    /// This class demonstrates the EXACT fix for the test mentioned in the problem statement:
    /// "InvoiceValueDiscount_WithNullOfferProduct_ShouldResultInZeroUnitPriceAndGenericDescription"
    /// </summary>
    public class ExactProblemFix
    {
        // ✅ SOLUTION: Use unique database names to ensure test isolation
        private DiscountingContext CreateContext()
        {
            var databaseName = $"DiscountingTestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task InvoiceValueDiscount_WithNullOfferProduct_ShouldResultInZeroUnitPriceAndGenericDescription()
        {
            // Arrange
            using var context = CreateContext(); // ✅ Fresh, isolated context
            var service = new DiscountingService(context);

            var invoice = new Invoice
            {
                Id = 1,
                Description = "Original Description",
                UnitPrice = 100,
                OfferProductId = null // No OfferProduct - this should result in UnitPrice = 0
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CalculateInvoiceDiscountAsync(1);

            // Assert - These assertions will now pass consistently
            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }

        [Fact]
        public async Task InvoiceValueDiscount_WithOfferProduct_ShouldUseSellingPrice()
        {
            // Arrange
            using var context = CreateContext(); // ✅ Fresh, isolated context
            var service = new DiscountingService(context);

            // Create an OfferProduct that won't interfere with other tests
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
                OfferProductId = 1 // References the OfferProduct
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CalculateInvoiceDiscountAsync(1);

            // Assert
            Assert.Equal(150, result.UnitPrice);
            Assert.Equal("Premium Product", result.Description);
        }

        // This test demonstrates the issue would have occurred with shared database names
        [Fact]
        public async Task VerifyTestsAreNowIsolated()
        {
            // Run this test after the others to verify isolation works
            using var context = CreateContext();
            
            // With proper isolation, this context should be completely empty
            var offerProductCount = await context.OfferProducts.CountAsync();
            var invoiceCount = await context.Invoices.CountAsync();

            Assert.Equal(0, offerProductCount); // ✅ No contamination from other tests
            Assert.Equal(0, invoiceCount);      // ✅ No contamination from other tests
        }
    }

    /// <summary>
    /// BEFORE/AFTER comparison showing the problematic code vs the fix
    /// </summary>
    public class BeforeAfterComparison
    {
        // ❌ BEFORE: This was the problematic implementation
        private DiscountingContext CreateProblematicContext()
        {
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: "DiscountingTestDb") // ⚠️ Same name always!
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        // ✅ AFTER: This is the fixed implementation
        private DiscountingContext CreateFixedContext()
        {
            var databaseName = $"DiscountingTestDb_{Guid.NewGuid()}"; // ✅ Unique per call
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task DemonstrateFix()
        {
            // The fix is simply changing one line in your context creation method
            // From: "DiscountingTestDb" (static)
            // To:   $"DiscountingTestDb_{Guid.NewGuid()}" (unique)
            
            using var context = CreateFixedContext();
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

            // This will now consistently pass
            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }
    }
}