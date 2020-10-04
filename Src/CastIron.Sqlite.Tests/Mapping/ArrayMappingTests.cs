using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class ArrayMappingTests
    {
        public class StringArrayQuery : ISqlQuerySimple<string[]>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public string[] Read(IDataResults result)
            {
                return result.AsEnumerable<string[]>().FirstOrDefault();
            }
        }

        [Test]
        public void Map_StringArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringArrayQuery());
            result.Length.Should().Be(3);
            result[0].Should().Be("5");
            result[1].Should().Be("TEST");
            result[2].Should().Be("1");
        }

        public class TestObject1
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }
        }

        [Test]
        public void Map_ArrayOfCustomObject()
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
        public void Map_MapToObjectArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_ObjectArray());
            result.Length.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }

        public class TestObjectStringArray
        {
            public string[] TestString { get; set; }
        }

        [Test]
        public void Map_ObjectWithStringArrayProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObjectStringArray>("SELECT 5 AS TestString, 'TEST' AS TestString, 3.14 AS TestString;").First();
            result.TestString.Length.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectStringArrayCtor
        {
            public TestObjectStringArrayCtor(string[] testString)
            {
                TestString = testString;
            }

            public string[] TestString { get; }
        }

        [Test]
        public void Map_ObjectWithStringArrayCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObjectStringArrayCtor>("SELECT 5 AS TestString, 'TEST' AS TestString, 3.14 AS TestString;").First();
            result.TestString.Length.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }
    }
}