using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryStreamingTests
    {

        [Test]
        public void StreamingResults_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var stream = runner.QueryStream("SELECT 1 UNION SELECT 2 UNION SELECT 3");
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        private class Query : ISqlQuery
        {
            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT 1 UNION SELECT 2 UNION SELECT 3");
                return interaction.IsValid;
            }
        }

        [Test]
        public void StreamingResults_Query([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var stream = runner.QueryStream(new Query());
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        [Test]
        public async Task StreamingResultsSimpleAsync([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var stream = await runner.QueryStreamAsync("SELECT 1 UNION SELECT 2 UNION SELECT 3");
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        [Test]
        public async Task StreamingResults_QueryAsync([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var stream = await runner.QueryStreamAsync(new Query());
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }
    }
}