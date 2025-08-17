# Test Configuration Guide

This guide provides configuration patterns for Entity Framework tests that require Azure services, showing how to use local emulators like Azurite for testing.

## Azure Storage Configuration for Tests

### Using Azurite (Azure Storage Emulator)

For tests that require Azure Storage connections, use Azurite instead of production storage accounts:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;MultipleActiveResultSets=true",
    "AzureStorage": "UseDevelopmentStorage=true"
  },
  "AzureStorage": {
    "BlobAccountName": "devstoreaccount1",
    "AccountKey": "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==",
    "EndpointSuffix": "core.windows.net"
  }
}
```

### Entity Configuration with Required Properties

When testing entities with required properties like `Tenant`, provide default test values:

```csharp
public class TenantTestDataBuilder
{
    public static Tenant CreateValidTenant(int id = 1)
    {
        return new Tenant
        {
            Id = id,
            BlobAccountName = "devstoreaccount1", // Azurite default account
            DbConnectionStringName = "DefaultConnection",
            IconUrl = "https://localhost:10000/devstoreaccount1/icons/default.png", // Azurite blob URL
            // Add other required properties as needed
        };
    }
}
```

### Test Initialization Pattern

```csharp
[TestInitialize]
public void Init()
{
    // Configure test database
    var options = new DbContextOptionsBuilder<YourDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    using var context = new YourDbContext(options);
    
    // Seed with valid test data
    context.Tenants.Add(TenantTestDataBuilder.CreateValidTenant());
    context.SaveChanges();
}
```

## Running Azurite for Local Testing

### Installation and Setup

1. Install Azurite globally:
```bash
npm install -g azurite
```

2. Start Azurite services:
```bash
# Start all services (Blob, Queue, Table)
azurite --silent --location ./azurite --debug ./azurite/debug.log

# Or start only specific services
azurite-blob --location ./azurite --debug ./azurite/debug.log
```

3. Default endpoints:
- Blob Service: `http://127.0.0.1:10000/devstoreaccount1`
- Queue Service: `http://127.0.0.1:10001/devstoreaccount1`  
- Table Service: `http://127.0.0.1:10002/devstoreaccount1`

### Environment Variables for Tests

Set these environment variables for consistent test configuration:

```bash
export AZURE_STORAGE_CONNECTION_STRING="UseDevelopmentStorage=true"
export AZURE_STORAGE_ACCOUNT="devstoreaccount1"
export TEST_DATABASE_CONNECTION="Data Source=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true"
```

### Docker Compose for Azurite

For team consistency, use Docker Compose:

```yaml
version: '3.8'
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    hostname: azurite
    restart: always
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
    volumes:
      - ./azurite:/data
    command: "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --location /data"
```

## Troubleshooting

### Common Issues

1. **Required properties missing**: Ensure all required entity properties have default test values
2. **Connection string not found**: Verify test configuration includes all required connection strings
3. **Azurite not running**: Start Azurite before running tests that require Azure Storage
4. **Port conflicts**: Use different ports if defaults are occupied

### Validation Script

```bash
#!/bin/bash
# validate-test-environment.sh

echo "🔍 Validating test environment..."

# Check if Azurite is running
if curl -s http://localhost:10000 > /dev/null; then
    echo "✅ Azurite blob service is running"
else
    echo "❌ Azurite not running. Start with: azurite --silent"
    exit 1
fi

# Check LocalDB
if sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT 1" > /dev/null 2>&1; then
    echo "✅ LocalDB is available"
else
    echo "❌ LocalDB not available"
fi

echo "✅ Test environment ready"
```

This configuration ensures tests run reliably in local environments without requiring production Azure services.