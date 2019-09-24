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
        public void StreamingResults_Test()
        {
            var runner = RunnerFactory.Create();
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
        public void StreamingResults_Query()
        {
            var runner = RunnerFactory.Create();
            var stream = runner.QueryStream(new Query());
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        [Test]
        public async Task StreamingResultsSimpleAsync()
        {
            var runner = RunnerFactory.Create();
            var stream = await runner.QueryStreamAsync("SELECT 1 UNION SELECT 2 UNION SELECT 3");
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        [Test]
        public async Task StreamingResults_QueryAsync()
        {
            var runner = RunnerFactory.Create();
            var stream = await runner.QueryStreamAsync(new Query());
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }
    }

    [TestFixture]
    public class SqlQueryAsyncStreamingTests
    {
    }
}
