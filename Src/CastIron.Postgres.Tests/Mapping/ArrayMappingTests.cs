using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class ArrayMappingTests
    {
        public class TestObject1
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }
        }

        [Test]
        public void TestQuery_ArrayOfCustomObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(SqlQuery.FromString<TestObject1[]>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Length.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }

        public class TestQuery_ObjectArray : ISqlQuerySimple<object[]>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public object[] Read(IDataResults result)
            {
                return result.AsEnumerable<object[]>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery_MapToObjectArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_ObjectArray());
            result.Length.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }
    }
}