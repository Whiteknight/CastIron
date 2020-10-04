using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class ConcreteCollectionMappingTests
    {
        public class StringListQuery<T> : ISqlQuerySimple<T>
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
        public void Map_ObjectWithStringListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringList>());
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
        public void Map_ObjectWithExistingStringListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectExistingStringList>());
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
        public void Map_ObjectWithStringListCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        [Test]
        public void Map_ListOfInt()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<int>>("SELECT 5, 6, '7'").First();
            result.Count.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be(6);
            result[2].Should().Be(7);
        }

        [Test]
        public void Map_MapStringToListOfDateTime()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<DateTime>>("SELECT '2018-12-11 17:01:02'").First();
            result.Count.Should().Be(1);
            result[0].Should().Be(new DateTime(2018, 12, 11, 17, 1, 2));
        }

        private class TestObjectSimple
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void Map_ListOfCustomObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<TestObjectSimple>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0].Id.Should().Be(1);
            result[0].Name.Should().Be("TEST1");
            result[1].Id.Should().Be(2);
            result[1].Name.Should().Be("TEST2");
            result[2].Id.Should().Be(3);
            result[2].Name.Should().Be("TEST3");
        }

        [Test]
        public void Map_ListOfTuple2()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<Tuple<int, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0].Item1.Should().Be(1);
            result[0].Item2.Should().Be("TEST1");
            result[1].Item1.Should().Be(2);
            result[1].Item2.Should().Be("TEST2");
            result[2].Item1.Should().Be(3);
            result[2].Item2.Should().Be("TEST3");
        }

        [Test]
        public void Map_ListOfDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<Dictionary<string, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0]["ID"].Should().Be("1");
            result[0]["Name"].Should().Be("TEST1");
            result[1]["ID"].Should().Be("2");
            result[1]["Name"].Should().Be("TEST2");
            result[2]["ID"].Should().Be("3");
            result[2]["Name"].Should().Be("TEST3");
        }

        [Test]
        public void Map_ListOfIDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<IDictionary<string, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0]["ID"].Should().Be("1");
            result[0]["Name"].Should().Be("TEST1");
            result[1]["ID"].Should().Be("2");
            result[1]["Name"].Should().Be("TEST2");
            result[2]["ID"].Should().Be("3");
            result[2]["Name"].Should().Be("TEST3");
        }
    }
}