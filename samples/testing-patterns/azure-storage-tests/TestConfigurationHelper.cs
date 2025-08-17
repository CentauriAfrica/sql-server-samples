using Microsoft.Extensions.Configuration;
using System.IO;

namespace SqlServerSamples.Testing.Patterns
{
    /// <summary>
    /// Helper class for configuring Entity Framework tests with Azure services using local emulators
    /// </summary>
    public static class TestConfigurationHelper
    {
        /// <summary>
        /// Gets test configuration with azurite and localdb settings
        /// </summary>
        public static IConfiguration GetTestConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Creates a Tenant entity with default test values to avoid missing required properties
        /// </summary>
        public static object CreateTestTenant(int id = 1, IConfiguration configuration = null)
        {
            configuration ??= GetTestConfiguration();
            
            // Return an anonymous object that can be used to seed the database
            // Adapt this to match your actual Tenant entity structure
            return new
            {
                Id = id,
                BlobAccountName = configuration["Tenant:DefaultBlobAccountName"] ?? "devstoreaccount1",
                DbConnectionStringName = configuration["Tenant:DefaultDbConnectionStringName"] ?? "DefaultConnection",
                IconUrl = configuration["Tenant:DefaultIconUrl"] ?? "http://127.0.0.1:10000/devstoreaccount1/icons/default.png",
                Name = $"Test Tenant {id}",
                CreatedDate = System.DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets the azurite blob storage connection string
        /// </summary>
        public static string GetAzuriteConnectionString(IConfiguration configuration = null)
        {
            configuration ??= GetTestConfiguration();
            return configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
        }

        /// <summary>
        /// Gets the test database connection string
        /// </summary>
        public static string GetTestDatabaseConnectionString(IConfiguration configuration = null)
        {
            configuration ??= GetTestConfiguration();
            return configuration.GetConnectionString("DefaultConnection") ?? 
                   "Data Source=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;MultipleActiveResultSets=true";
        }

        /// <summary>
        /// Validates that azurite is running by checking the blob endpoint
        /// </summary>
        public static async Task<bool> IsAzuriteRunningAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync("http://127.0.0.1:10000");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Setup method for Entity Framework tests that require Azure services
        /// </summary>
        public static async Task EnsureTestEnvironmentAsync()
        {
            if (!await IsAzuriteRunningAsync())
            {
                throw new InvalidOperationException(
                    "Azurite is not running. Please start azurite before running tests:\n" +
                    "npm install -g azurite\n" +
                    "azurite --silent --location ./azurite");
            }
        }
    }
}