using System.Linq;
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
    }
}