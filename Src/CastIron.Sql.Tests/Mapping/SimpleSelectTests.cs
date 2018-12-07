using System.Collections.Generic;
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

            public TestObject1 Read(IDataResults result)
            {
                return result.AsEnumerable<TestObject1>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery1_Properties([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery1_ConstructorParams_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
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

            public object[] Read(IDataResults result)
            {
                return result.AsEnumerable<object[]>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery_MapToObjectArray([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
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

        public class TestQuery_Unbox : ISqlQuerySimple<TestObject1>
        {
            public string GetSql()
            {
                return "SELECT CAST(5 AS BIGINT) AS TestInt, 6 AS TestString, 1 AS TestBool;";
            }

            public TestObject1 Read(IDataResults result)
            {
                return result.AsEnumerable<TestObject1>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery_UnboxProperties([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_Unbox());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("6");
            result.TestBool.Should().Be(true);
        }

        public class TestQuery_StringArray : ISqlQuerySimple<string[]>
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
        public void TestQuery_MapToStringArray_MSSQL([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringArray());
            result.Length.Should().Be(3);
            result[0].Should().Be("5");
            result[1].Should().Be("TEST");
            result[2].Should().Be("True");
        }

        [Test]
        public void TestQuery_MapToStringArray_SQLITE([Values("SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringArray());
            result.Length.Should().Be(3);
            result[0].Should().Be("5");
            result[1].Should().Be("TEST");
            // TODO: Can we (and should we) try to force BIT->Boolean?
            result[2].Should().Be("1");
        }

        public class TestQuery_StringList<T> : ISqlQuerySimple<T>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestString, 'TEST' AS TestString, 3.14 AS TestString;";
            }

            public T Read(IDataResults result)
            {
                return result.AsEnumerable<T>().FirstOrDefault();
            }
        }

        public class TestObjectStringList
        {
            public List<string> TestString { get; set; }
        }

        [Test]
        public void TestQuery_MapToStringListProperty([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringList>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectStringListCtor
        {
            public TestObjectStringListCtor(List<string> testString)
            {
                TestString = testString;
            }

            public List<string> TestString { get; }
        }

        [Test]
        public void TestQuery_MapToStringListCtorParam([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectStringArray
        {
            public string[] TestString { get; set; }
        }

        [Test]
        public void TestQuery_MapToStringArrayProperty([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringArray>());
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
        public void TestQuery_MapToStringArrayCtorParam([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringArrayCtor>());
            result.TestString.Length.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectStringIList
        {
            public IList<string> TestString { get; set; }
        }

        [Test]
        public void TestQuery_MapToStringIListProperty([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringIList>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectStringIListCtor
        {
            public TestObjectStringIListCtor(IList<string> testString)
            {
                TestString = testString;
            }

            public IList<string> TestString { get; }
        }

        [Test]
        public void TestQuery_MapToStringIListCtorParam([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_StringList<TestObjectStringIListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }
    }
}
