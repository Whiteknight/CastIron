using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryAsyncTests
    {
        [Test]
        public async Task QueryAsync_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = await runner.QueryAsync<string>("SELECT 'TEST'");
            result[0].Should().Be("TEST");
        }
    }
}