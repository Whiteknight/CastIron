using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryTests
    {
        public class Query1 : ISqlQuery<string>
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

        [Test]
        public void SqlQuery_UseReadOnlyConnection()
        {
            var runner = RunnerFactory.Create();
            // The ApplicationIntent=ReadOnly flag is only for routing, and our test server isn't clustered. 
            // We can't test that this connection actually enforces RO access, only that the UseReadOnlyConnection()
            // method doesn't cause a failure
            var result = runner.Query(new Query1(), b => b.UseReadOnlyConnection());
            result.Should().Be("TEST");
        }

        public class QueryReadOnly : ISqlQuery<string>, ISqlReadOnly
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
        public void SqlQuery_SqlReadOnly()
        {
            var runner = RunnerFactory.Create();
            // The ApplicationIntent=ReadOnly flag is only for routing, and our test server isn't clustered. 
            // We can't test that this connection actually enforces RO access, only that the ISqlReadOnly
            // tag doesn't cause a failure
            var result = runner.Query(new QueryReadOnly());
            result.Should().Be("TEST");
        }
    }
}
