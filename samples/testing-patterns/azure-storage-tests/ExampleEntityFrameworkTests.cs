using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace SqlServerSamples.Testing.Patterns
{
    /// <summary>
    /// Example test class demonstrating how to set up Entity Framework tests 
    /// with Azure services using local emulators (azurite)
    /// </summary>
    [TestClass]
    public class ExampleEntityFrameworkTests
    {
        private IConfiguration _configuration;

        [TestInitialize]
        public async Task Init()
        {
            // Get test configuration with azurite settings
            _configuration = TestConfigurationHelper.GetTestConfiguration();
            
            // Ensure azurite is running before proceeding with tests
            await TestConfigurationHelper.EnsureTestEnvironmentAsync();

            // If you have a DbContext, you would set it up here:
            /*
            var options = new DbContextOptionsBuilder<YourDbContext>()
                .UseSqlServer(TestConfigurationHelper.GetTestDatabaseConnectionString(_configuration))
                .Options;

            using var context = new YourDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            // Seed with test data to avoid "Required properties missing" errors
            var testTenant = TestConfigurationHelper.CreateTestTenant(1, _configuration);
            
            // Add the tenant to your context (adapt to your actual entity)
            // context.Tenants.Add(new Tenant 
            // { 
            //     Id = testTenant.Id,
            //     BlobAccountName = testTenant.BlobAccountName,
            //     DbConnectionStringName = testTenant.DbConnectionStringName,
            //     IconUrl = testTenant.IconUrl,
            //     Name = testTenant.Name,
            //     CreatedDate = testTenant.CreatedDate
            // });
            
            // await context.SaveChangesAsync();
            */
        }

        [TestMethod]
        public void TestConfiguration_ShouldHaveAzuriteSettings()
        {
            // Verify configuration is loaded correctly
            var connectionString = TestConfigurationHelper.GetAzuriteConnectionString(_configuration);
            Assert.AreEqual("UseDevelopmentStorage=true", connectionString);

            var blobAccountName = _configuration["Tenant:DefaultBlobAccountName"];
            Assert.AreEqual("devstoreaccount1", blobAccountName);

            var iconUrl = _configuration["Tenant:DefaultIconUrl"];
            Assert.IsTrue(iconUrl.StartsWith("http://127.0.0.1:10000"));
        }

        [TestMethod]
        public async Task Azurite_ShouldBeRunning()
        {
            // Verify azurite is accessible
            var isRunning = await TestConfigurationHelper.IsAzuriteRunningAsync();
            Assert.IsTrue(isRunning, "Azurite should be running on localhost:10000. Start it with: azurite --silent");
        }

        [TestMethod]
        public void CreateTestTenant_ShouldHaveAllRequiredProperties()
        {
            // Demonstrate creating a test tenant with all required properties
            dynamic testTenant = TestConfigurationHelper.CreateTestTenant(1, _configuration);
            
            Assert.IsNotNull(testTenant.BlobAccountName);
            Assert.IsNotNull(testTenant.DbConnectionStringName);
            Assert.IsNotNull(testTenant.IconUrl);
            Assert.AreEqual(1, testTenant.Id);
        }

        [TestMethod]
        public void FileImporter_Test_Example()
        {
            // This is an example of how your T07_FileImporterTests might be structured
            // to avoid the "Required properties missing" error
            
            // Arrange: Create test tenant with all required properties
            dynamic testTenant = TestConfigurationHelper.CreateTestTenant(1, _configuration);
            
            // If you have a DbContext, seed it with the test tenant:
            /*
            using var context = new YourDbContext(options);
            context.Tenants.Add(new Tenant 
            { 
                Id = testTenant.Id,
                BlobAccountName = testTenant.BlobAccountName,
                DbConnectionStringName = testTenant.DbConnectionStringName,
                IconUrl = testTenant.IconUrl,
                Name = testTenant.Name,
                CreatedDate = testTenant.CreatedDate
            });
            context.SaveChanges();
            */

            // Act: Your file importer logic here
            // var fileImporter = new FileImporter(context, azureStorageConfig);
            // var result = fileImporter.ImportFile(testFile);

            // Assert: Verify the import worked
            // Assert.IsTrue(result.Success);
            
            // For now, just verify we can create valid test data
            Assert.IsNotNull(testTenant);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test data if needed
            // If using in-memory database, it will be automatically disposed
        }
    }
}