using System.Linq;
using System.Threading.Tasks;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    [TestFixture]
    public class SqlQueryAsyncTests
    {
        [Test]
        public async Task QueryAsync_Simple()
        {
            var runner = RunnerFactory.Create();
            var result = await runner.QueryAsync<string>("SELECT 'TEST'");
            result[0].Should().Be("TEST");
        }

        private class Query : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT 'TEST'");
                return interaction.IsValid;
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().First();
            }
        }

        [Test]
        public async Task QueryAsync_QueryObject()
        {
            var runner = RunnerFactory.Create();
            var result = await runner.QueryAsync(new Query());
            result.Should().Be("TEST");
        }
    }
}