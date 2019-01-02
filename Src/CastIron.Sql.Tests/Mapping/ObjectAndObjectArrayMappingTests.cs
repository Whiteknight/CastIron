using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class ObjectAndObjectArrayMappingTests
    {
        public class TestQuery_Object : ISqlQuerySimple<object>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public object Read(IDataResults result)
            {
                return result.AsEnumerable<object>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery_MapToObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_Object());
            result.Should().BeOfType<object[]>();

            var array = result as object[];
            array.Length.Should().Be(3);
            array[0].Should().Be(5);
            array[1].Should().Be("TEST");
            array[2].Should().Be(true);
        }
    }
}