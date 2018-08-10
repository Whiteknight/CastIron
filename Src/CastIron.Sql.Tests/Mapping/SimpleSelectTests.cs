using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class SimpleSelectTests
    {
        public class TestObject1
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }
        }

        public class TestQuery1 : ISqlQuerySimple<TestObject1>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public TestObject1 Read(SqlResultSet result)
            {
                return result.AsEnumerable<TestObject1>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery1_Properties()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery1_ConstructorParams_Test()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        public class TestQuery_ObjectArray : ISqlQuerySimple<object[]>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public object[] Read(SqlResultSet result)
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

        public class TestQuery_Object : ISqlQuerySimple<object>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public object Read(SqlResultSet result)
            {
                return result.AsEnumerable<object>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery_MapToObject()
        {
            var target = RunnerFactory.Create();
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
