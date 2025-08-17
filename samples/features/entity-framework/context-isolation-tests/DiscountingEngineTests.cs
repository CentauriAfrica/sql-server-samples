using DiscountingEngine.Models;
using DiscountingEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiscountingEngine.Tests
{
    public class DiscountingEngineTests
    {
        private DiscountingContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: "DiscountingTestDb") // Problematic: shared database name
                .Options;

            var context = new DiscountingContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task A_InvoiceValueDiscount_WithOfferProduct_ShouldUseSellingPrice()
        {
            // Arrange
            using var context = CreateContext();
            var service = new DiscountingService(context);

            // Create an OfferProduct with SellingPrice = 150
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
        public async Task B_InvoiceValueDiscount_WithNullOfferProduct_ShouldResultInZeroUnitPriceAndGenericDescription()
        {
            // Arrange
            using var context = CreateContext();
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

            // Act - Using the method that demonstrates the state sharing issue
            var result = await service.CalculateInvoiceDiscountWithExplicitLoadAsync(2);

            // Assert - This will fail when run as part of test suite due to context state sharing
            // The method will find OfferProducts from the previous test and use 150 instead of 0
            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }
    }
}