using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql.Statements;
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

        [Test]
        public void TestQuery_MapStringToDateTime([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<DateTime>("SELECT '2018-12-11 17:01:02'")).First();
            result.Should().Be(new DateTime(2018, 12, 11, 17, 1, 2));
        }

        public class TestObject_WithChild
        {
            public int Id { get; set; }

            public class TestChild
            {
                public string Name { get; set; }
            }

            public TestChild Child { get; set; }
        }

        [Test]
        public void TestQuery_ObjectWithChild([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject_WithChild>("SELECT 5 AS Id, 'TEST' AS Child_Name;")).First();
            result.Id.Should().Be(5);
            result.Child.Name.Should().Be("TEST");
        }

     
        [Test]
        public void TestQuery_ListOfCustomObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<List<TestObject1>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery_IListOfCustomObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<IList<TestObject1>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery_ArrayOfCustomObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject1[]>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Length.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }
    }
}
