using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryTests
    {
        public class Query1 : ISqlQuerySimple<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(SqlResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQuery_Tests()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_PerformanceReport()
        {
            string report = null;
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query1(), b => b.MonitorPerformance(s => report = s));
            result.Should().Be("TEST");
            report.Should().NotBeNull();
        }
    }
}
