# Entity Framework Context State Sharing - Quick Fix Guide

## The Problem
When using Entity Framework with InMemory databases in tests, you may encounter this issue:
- ✅ Tests pass when run individually
- ❌ Tests fail when run as part of a test suite
- 🐛 Error: Expected value X, but got Y (usually from a previous test)

## Root Cause
```csharp
// PROBLEMATIC CODE
private DiscountingContext CreateContext()
{
    var options = new DbContextOptionsBuilder<DiscountingContext>()
        .UseInMemoryDatabase(databaseName: "TestDb") // ⚠️ Same name for all tests!
        .Options;
    return new DiscountingContext(options);
}
```

## Quick Fix (Recommended)
```csharp
// FIXED CODE
private DiscountingContext CreateContext()
{
    var options = new DbContextOptionsBuilder<DiscountingContext>()
        .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}") // ✅ Unique per test
        .Options;
    return new DiscountingContext(options);
}
```

## Alternative Solutions

### Option 1: Test Class Scoped Database
```csharp
public class MyTests : IDisposable
{
    private readonly DiscountingContext _context;
    
    public MyTests()
    {
        var options = new DbContextOptionsBuilder<DiscountingContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{GetType().Name}_{Guid.NewGuid()}")
            .Options;
        _context = new DiscountingContext(options);
    }
    
    [Fact]
    public async Task MyTest()
    {
        // Clean state before each test
        _context.MyEntities.RemoveRange(_context.MyEntities);
        await _context.SaveChangesAsync();
        
        // Test logic...
    }
    
    public void Dispose() => _context?.Dispose();
}
```

### Option 2: xUnit Collection Fixture
```csharp
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IDisposable
{
    public DiscountingContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DiscountingContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        return new DiscountingContext(options);
    }
    
    public void Dispose() { /* cleanup */ }
}

[Collection("Database collection")]
public class MyTests
{
    private readonly DatabaseFixture _fixture;
    
    public MyTests(DatabaseFixture fixture) => _fixture = fixture;
    
    [Fact]
    public async Task MyTest()
    {
        using var context = _fixture.CreateContext();
        // Test logic...
    }
}
```

## How to Apply to Your Existing Code

1. **Find problematic test methods**:
   ```bash
   grep -r "UseInMemoryDatabase.*\".*\"" --include="*.cs" .
   ```

2. **Replace with unique names**:
   ```csharp
   // Before
   .UseInMemoryDatabase(databaseName: "MyTestDb")
   
   // After
   .UseInMemoryDatabase(databaseName: $"MyTestDb_{Guid.NewGuid()}")
   ```

3. **Test the fix**:
   ```bash
   # Run all tests to ensure they pass
   dotnet test
   
   # Run individual tests to ensure they still work
   dotnet test --filter "YourSpecificTest"
   ```

## When This Fix Is Needed

- Using EF Core with `UseInMemoryDatabase()`
- Tests fail only when run together, pass individually
- Error messages show unexpected data from other tests
- Tests that create entities that affect other tests

## What This Fix Prevents

- Cross-test data contamination
- False test failures due to state sharing
- Inconsistent test results
- Hard-to-debug test issues

## Performance Considerations

- ✅ Unique databases provide complete isolation
- ⚠️ Slightly slower due to multiple database instances
- 💡 Consider using shared databases with manual cleanup for performance-critical scenarios

Apply this pattern wherever you use Entity Framework InMemory databases in tests to ensure reliable, isolated test execution.