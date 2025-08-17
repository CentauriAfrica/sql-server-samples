using DiscountingEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscountingEngine.Services
{
    public class DiscountingService
    {
        private readonly DiscountingContext _context;

        public DiscountingService(DiscountingContext context)
        {
            _context = context;
        }

        public async Task<Invoice> CalculateInvoiceDiscountAsync(int invoiceId)
        {
            // This is the problematic query that demonstrates the context state sharing issue
            var invoice = await _context.Invoices
                .Include(x => x.OfferProduct)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new ArgumentException($"Invoice with ID {invoiceId} not found");

            // Business logic: when OfferProduct is null, UnitPrice should be 0 and description should be generic
            if (invoice.OfferProduct == null)
            {
                // The issue: in a shared context, OfferProduct might not be null due to previous test data
                // Even though OfferProductId is null, EF might still return an OfferProduct from the context
                invoice.UnitPrice = 0;
                invoice.Description = "Generic Product";
            }
            else
            {
                // When OfferProduct exists, use its selling price
                invoice.UnitPrice = invoice.OfferProduct.SellingPrice;
                invoice.Description = invoice.OfferProduct.Name;
            }

            await _context.SaveChangesAsync();
            return invoice;
        }

        // This method demonstrates the issue more clearly
        public async Task<Invoice> CalculateInvoiceDiscountWithExplicitLoadAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
                throw new ArgumentException($"Invoice with ID {invoiceId} not found");

            // The problem: even with null OfferProductId, if there are OfferProducts in the context,
            // the navigation property might still get populated depending on EF's behavior
            if (invoice.OfferProductId.HasValue)
            {
                await _context.Entry(invoice).Reference(i => i.OfferProduct).LoadAsync();
            }

            // In a shared context scenario, check if any OfferProducts exist
            var anyOfferProducts = await _context.OfferProducts.AnyAsync();
            
            if (invoice.OfferProduct == null && !anyOfferProducts)
            {
                invoice.UnitPrice = 0;
                invoice.Description = "Generic Product";
            }
            else if (invoice.OfferProduct == null && anyOfferProducts)
            {
                // This is the problematic case - we have OfferProducts in context but this invoice has none
                // In a real scenario, this might cause unexpected behavior
                var defaultPrice = await _context.OfferProducts.Select(op => op.SellingPrice).FirstOrDefaultAsync();
                invoice.UnitPrice = defaultPrice; // This is the bug - using 150 instead of 0
                invoice.Description = "Incorrect Product Assignment";
            }
            else
            {
                invoice.UnitPrice = invoice.OfferProduct.SellingPrice;
                invoice.Description = invoice.OfferProduct.Name;
            }

            await _context.SaveChangesAsync();
            return invoice;
        }
    }
}