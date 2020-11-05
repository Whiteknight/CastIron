using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    public class SqlQueryTests
    {
        [Test]
        public void SqlQuery_Combine_string()
        {
            var runner = RunnerFactory.Create();
            var query = SqlQuery.Combine("SELECT 'TEST'", r => r.AsEnumerable<string>().First());
            var result = runner.Query(query);
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_Combine_string_Cached()
        {
            // Quick test for cacheMappings=true
            // We can't really test at this level that the cache was correctly utilized, but we can
            // test that repeated invocations don't break. Evidence that, at least, the cache
            // isn't making things stop working.
            var runner = RunnerFactory.Create();
            var query = SqlQuery.Combine("SELECT 'TEST'", r => r.AsEnumerable<string>().First(), true);

            var result = runner.Query(query);
            result.Should().Be("TEST");

            // Run it again, to make sure it still works
            result = runner.Query(query);
            result.Should().Be("TEST");
        }
    }
}
