using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    /// <summary>
    /// Basic tests for SELECT queries returning values. More depth in mapping logic
    /// is included in the Mapping tests.
    /// </summary>
    [TestFixture]
    public class SelectTests
    {
        public class Query : ISqlQuerySimple<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQuery_Tests([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query());
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_PerformanceReport([Values("MSSQL", "SQLITE")] string provider)
        {
            string report = null;
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query(), b => b.MonitorPerformance(s => report = s));
            result.Should().Be("TEST");
            report.Should().NotBeNull();
        }

        [Test]
        public void SqlQuery_Combine([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var query = SqlQuery.Combine("SELECT 'TEST'", r => r.AsEnumerable<string>().First());
            var result = runner.Query(query);
            result.Should().Be("TEST");
        }
    }
}
