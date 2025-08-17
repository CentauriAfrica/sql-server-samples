using DiscountingEngine.Models;
using DiscountingEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiscountingEngine.Tests
{
    [Collection("StateSharing")]
    public class DiscountingEngineStateIssueTests
    {
        private static readonly string SharedDbName = "SharedDbForIssueDemo";

        private DiscountingContext CreateSharedContext()
        {
            var options = new DbContextOptionsBuilder<DiscountingContext>()
                .UseInMemoryDatabase(databaseName: SharedDbName) // Problematic: same DB name
                .Options;

            return new DiscountingContext(options);
        }

        [Fact]
        public async Task Test1_SetupDataWithOfferProduct()
        {
            // This test sets up data that will pollute subsequent tests
            using var context = CreateSharedContext();
            context.Database.EnsureCreated();
            
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
                Description = "With Product",
                UnitPrice = 0,
                OfferProductId = 1
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            var result = await service.CalculateInvoiceDiscountAsync(1);
            Assert.Equal(150, result.UnitPrice);
        }

        [Fact]
        public async Task Test2_ThisFailsBecauseOfStateSharing()
        {
            // This test should pass individually but fails after Test1 due to shared context
            using var context = CreateSharedContext();
            var service = new DiscountingService(context);

            var invoice = new Invoice
            {
                Id = 2,
                Description = "Without Product",
                UnitPrice = 100,
                OfferProductId = null // No OfferProduct reference
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // This demonstrates the state sharing issue using the explicit load method
            var result = await service.CalculateInvoiceDiscountWithExplicitLoadAsync(2);

            // This assertion will fail when run after Test1 because anyOfferProducts will be true
            // The method will incorrectly use 150 instead of 0
            Assert.Equal(0, result.UnitPrice);
            Assert.Equal("Generic Product", result.Description);
        }
    }
}