using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
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
        public void SqlQuery_Tests()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query());
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_PerformanceReport()
        {
            string report = null;
            var runner = RunnerFactory.Create(b => b.MonitorPerformance(s => report = s));
            var result = runner.Query(new Query());
            result.Should().Be("TEST");
            report.Should().NotBeNull();
        }

        [Test]
        public void SqlQuery_Combine()
        {
            var runner = RunnerFactory.Create();
            var query = SqlQuery.Combine("SELECT 'TEST'", r => r.AsEnumerable<string>().First());
            var result = runner.Query(query);
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_ViewCommandBeforeExecution()
        {
            string report = null;
            var runner = RunnerFactory.Create(b => b.ViewCommandBeforeExecution(s =>
            {
                report = s.Trim();
            }));
            var result = runner.Query(new Query());
            result.Should().Be("TEST");
            report.Should().Be("SELECT 'TEST';");
        }
    }
}
