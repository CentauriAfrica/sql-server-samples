# Entity Framework Context State Sharing Issue

## Problem Description

This project demonstrates a common Entity Framework test isolation issue where tests pass individually but fail when run as part of a test suite. The problem occurs due to shared database state between tests when using EF Core's InMemory provider.

## The Issue

The test `InvoiceValueDiscount_WithNullOfferProduct_ShouldResultInZeroUnitPriceAndGenericDescription` expects:
- UnitPrice = 0 when OfferProduct is null
- Description = "Generic Product"

However, when run after other tests, it finds an unexpected OfferProduct with SellingPrice = 150, causing the test to fail.

## Root Cause

1. **Shared Database Name**: All tests use the same InMemory database name ("DiscountingTestDb")
2. **Entity Framework Context State**: The `Include(x => x.OfferProduct)` query finds entities from previous tests
3. **Lack of Test Isolation**: Database state persists between test executions

## How to Reproduce

1. Run individual test: `dotnet test --filter Test2_ThisFailsBecauseOfStateSharing` ✅ Passes
2. Run all tests: `dotnet test --filter DiscountingEngineStateIssueTests` ❌ Fails due to context state sharing

Example output:
```
Failed DiscountingEngine.Tests.DiscountingEngineStateIssueTests.Test2_ThisFailsBecauseOfStateSharing [43 ms]
Error Message:
 Assert.Equal() Failure
Expected: 0
Actual:   150
```

## Solutions

### Solution 1: Unique Database Names (Recommended)

Use unique database names for each test to ensure complete isolation:

```csharp
private DiscountingContext CreateContext()
{
    var databaseName = $"DiscountingTestDb_{Guid.NewGuid()}";
    
    var options = new DbContextOptionsBuilder<DiscountingContext>()
        .UseInMemoryDatabase(databaseName: databaseName)
        .Options;

    return new DiscountingContext(options);
}
```

### Solution 2: Test Fixture with Manual Cleanup

Use a shared context but manually clean data between tests:

```csharp
public class DiscountingEngineTestsWithFixture : IDisposable
{
    private readonly DiscountingContext _context;

    [Fact]
    public async Task TestMethod()
    {
        // Clean database before test
        _context.Invoices.RemoveRange(_context.Invoices);
        _context.OfferProducts.RemoveRange(_context.OfferProducts);
        await _context.SaveChangesAsync();
        
        // Test logic...
    }
}
```

### Solution 3: xUnit Collection Fixtures

Use xUnit's collection fixtures for managing shared resources:

```csharp
[CollectionDefinition("Database collection")]
public class DatabaseTestCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database collection")]
public class DiscountingEngineTestsWithCollection
{
    // Each test gets a fresh context
}
```

## Files in This Project

- `DiscountingEngineStateIssueTests.cs` - Demonstrates the failing scenario
- `DiscountingEngineTestsFixed.cs` - Shows all three solution approaches
- `DiscountingService.cs` - Contains the business logic with state sharing issues
- `Models.cs` - Entity Framework models and context

## Running the Tests

```bash
# See the problem in action
dotnet test --filter "DiscountingEngineStateIssueTests"

# See the solutions working
dotnet test --filter "Fixed"
dotnet test --filter "DiscountingEngineTestsWithFixture"
dotnet test --filter "DiscountingEngineTestsWithCollection"
```

## Key Takeaways

1. **Always isolate test databases** when using EF Core InMemory provider
2. **Use unique database names** to prevent state sharing
3. **Consider manual cleanup** for scenarios requiring shared context
4. **Test execution order matters** - design tests to be order-independent
5. **Include queries can find unexpected entities** from previous test runs