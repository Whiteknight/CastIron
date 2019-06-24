using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    public static class RunnerFactory
    {
        private static readonly IConfigurationRoot _configuration;

        static RunnerFactory()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                builder = builder.AddJsonFile("appsettings.linux.json");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                builder = builder.AddJsonFile("appsettings.windows.json");

            _configuration = builder.Build();
        }

        public static ISqlRunner Create(string provider = "MSSQL")
        {
            switch (provider)
            {
                case "SQLITE":
                    return Sqlite.RunnerFactory.Create(_configuration["SQLITE"]);

                case "MSSQL":
                    return Sql.RunnerFactory.Create(_configuration["MSSQL"]);

                case "POSTGRES":
                    return Postgres.RunnerFactory.Create(_configuration["POSTGRES"]);

                default:
                    Assert.Inconclusive("Unknown or unsupported provider " + provider);
                    break;
            }

            return null;
        }
    }
}
