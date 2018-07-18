using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace CastIron.Sql.Tests
{
    public static class RunnerFactory
    {
        private static readonly string _connectionString;

        static RunnerFactory()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                builder = builder.AddJsonFile("appsettings.linux.json");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                builder = builder.AddJsonFile("appsettings.windows.json");

            var configuration = builder.Build();
            _connectionString = configuration["ConnectionString"];
        }

        public static SqlRunner Create()
        {
            return new SqlRunner(_connectionString);
        }
    }
}
