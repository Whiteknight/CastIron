using System.Threading.Tasks;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    [TestFixture]
    public class SqlQueryAsyncStreamingTests
    {
        [Test]
        public async Task AsyncStreamResults_Tests()
        {
            var runner = RunnerFactory.Create();
            var stream = runner.QueryStream("SELECT 1 UNION SELECT 2 UNION SELECT 3");
            var result = stream.AsEnumerableAsync<int>();
            var sum = 0;
            await foreach (int i in result)
            {
                sum += i;
            }

            sum.Should().Be(6);
        }
    }
}