using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class CollectionMappingTests
    {
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
        public void TestQuery_MapToStringArray()
        {
            var target = RunnerFactory.Create();
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
        public void TestQuery_MapToStringListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_StringList<TestObjectStringList>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectExistingStringList
        {
            public TestObjectExistingStringList()
            {
                TestString = new List<string>();
            }

            public List<string> TestString { get; }
        }

        [Test]
        public void TestQuery_MapToExistingStringListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_StringList<TestObjectExistingStringList>());
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
        public void TestQuery_MapToStringListCtorParam()
        {
            var target = RunnerFactory.Create();
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
        public void TestQuery_MapToStringArrayProperty()
        {
            var target = RunnerFactory.Create();
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
        public void TestQuery_MapToStringArrayCtorParam()
        {
            var target = RunnerFactory.Create();
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
        public void TestQuery_MapToStringIListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_StringList<TestObjectStringIList>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        public class TestObjectExistingStringIList
        {
            public TestObjectExistingStringIList()
            {
                TestString = new List<string>();
            }

            public IList<string> TestString { get; }
        }

        [Test]
        public void TestQuery_MapToExistingStringIListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_StringList<TestObjectExistingStringIList>());
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
        public void TestQuery_MapToStringIListCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_StringList<TestObjectStringIListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        [Test]
        public void TestQuery_MapToRawIntArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<int>>("SELECT 5, 6, '7'").First();
            result.Count.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be(6);
            result[2].Should().Be(7);
        }

        [Test]
        public void TestQuery_MapStringToDateTimeList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<DateTime>>("SELECT '2018-12-11 17:01:02'").First();
            result.Count.Should().Be(1);
            result[0].Should().Be(new DateTime(2018, 12, 11, 17, 1, 2));
        }
    }
}