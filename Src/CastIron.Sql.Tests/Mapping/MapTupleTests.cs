using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class MapTupleTests
    {
        public class GetValuesQuery : ISqlQuerySimple<Tuple<int, string, bool>>
        {
            public string GetSql()
            {
                return "SELECT 5, 'TEST', 1;";
            }

            public Tuple<int, string, bool> Read(IDataResults result)
            {
                return result.AsEnumerable<Tuple<int, string, bool>>().FirstOrDefault();
            }
        }

        [Test]
        public void MapTuple_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new GetValuesQuery());
            result.Item1.Should().Be(5);
            result.Item2.Should().Be("TEST");
            result.Item3.Should().Be(true);
        }
    }
}
