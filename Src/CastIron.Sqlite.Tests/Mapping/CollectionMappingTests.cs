using System;
using System.Collections;
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
        public void Map_StringListProperty()
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
        public void Map_ExistingStringListProperty()
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
        public void Map_StringListCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringListCtor>());
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
        public void Map_StringArrayProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringArray>());
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
        public void Map_StringArrayCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringArrayCtor>());
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
        public void Map_StringIListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringIList>());
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
        public void Map_ExistingStringIListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectExistingStringIList>());
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
        public void Map_StringIListCtorParam()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new StringListQuery<TestObjectStringIListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
        }

        [Test]
        public void Map_RawIntArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<int>>("SELECT 5, 6, '7'").First();
            result.Count.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be(6);
            result[2].Should().Be(7);
        }

        [Test]
        public void Map_MapStringToDateTimeList()
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
        public void Map_MultipleCustomObjectList()
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
        public void Map_MultipleCustomObjectIList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<TestObjectSimple>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0].Id.Should().Be(1);
            result[0].Name.Should().Be("TEST1");
            result[1].Id.Should().Be(2);
            result[1].Name.Should().Be("TEST2");
            result[2].Id.Should().Be(3);
            result[2].Name.Should().Be("TEST3");
        }

        [Test]
        public void Map_MultipleTupleList()
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
        public void Map_MultipleDictionaryList()
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
        public void Map_MultipleIDictionaryList()
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

        [Test]
        public void Map_MultipleTupleIList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<Tuple<int, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0].Item1.Should().Be(1);
            result[0].Item2.Should().Be("TEST1");
            result[1].Item1.Should().Be(2);
            result[1].Item2.Should().Be("TEST2");
            result[2].Item1.Should().Be(3);
            result[2].Item2.Should().Be("TEST3");
        }

        [Test]
        public void Map_MultipleDictionaryIList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<Dictionary<string, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0]["ID"].Should().Be("1");
            result[0]["Name"].Should().Be("TEST1");
            result[1]["ID"].Should().Be("2");
            result[1]["Name"].Should().Be("TEST2");
            result[2]["ID"].Should().Be("3");
            result[2]["Name"].Should().Be("TEST3");
        }

        [Test]
        public void Map_MultipleIDictionaryIList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<IDictionary<string, string>>>("SELECT 1 AS ID, 'TEST1' AS Name, 2 AS ID, 'TEST2' AS Name, 3 AS ID, 'TEST3' AS Name").First();
            result.Count.Should().Be(3);
            result[0]["ID"].Should().Be("1");
            result[0]["Name"].Should().Be("TEST1");
            result[1]["ID"].Should().Be("2");
            result[1]["Name"].Should().Be("TEST2");
            result[2]["ID"].Should().Be("3");
            result[2]["Name"].Should().Be("TEST3");
        }

        public class InvalidCollectionType : IEnumerable<int>
        {
            public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        [Test]
        public void EnumerableMustHaveAddMethod_Test()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<IEnumerable<int>>("SELECT 1 AS A, 2 AS B, 3 AS C", c => c
                    .For<IEnumerable<int>>(d => d
                        .UseClass<InvalidCollectionType>()
                    )
                )
                .First();
            // If the configured collection type doesn't have a .Add() method, the compiler falls
            // back to using List<T> to satisfy the interface
            result.Should().BeOfType<List<int>>();
            var asList = result.ToList();
            asList[0].Should().Be(1);
            asList[1].Should().Be(2);
            asList[2].Should().Be(3);
        }

        [Test]
        public void Map_IList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();

            result.Count.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }

        [Test]
        public void Map_ICollection()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<ICollection>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            var list = result.Cast<object>().ToList();
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }

        [Test]
        public void Map_IEnumerable()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IEnumerable>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            var list = result.Cast<object>().ToList();
            list.Count.Should().Be(3);
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }

        [Test]
        public void Map_IEnumerableOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IEnumerable<object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<List<object>>();

            var list = result.ToList();
            list.Count.Should().Be(3);
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }

        [Test]
        public void Map_IEnumerableT()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IEnumerable<int>>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            var resultAsList = result.ToList();
            resultAsList.Count.Should().Be(3);
            resultAsList[0].Should().Be(5);
            resultAsList[1].Should().Be(6);
            resultAsList[2].Should().Be(7);
        }

        [Test]
        public void Map_ICollectionT()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<ICollection<int>>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            var resultAsList = result.ToList();
            resultAsList.Count.Should().Be(3);
            resultAsList[0].Should().Be(5);
            resultAsList[1].Should().Be(6);
            resultAsList[2].Should().Be(7);
        }

        [Test]
        public void Map_IReadOnlyCollectionT()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IReadOnlyCollection<int>>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            var resultAsList = result.ToList();
            resultAsList.Count.Should().Be(3);
            resultAsList[0].Should().Be(5);
            resultAsList[1].Should().Be(6);
            resultAsList[2].Should().Be(7);
        }

        [Test]
        public void Map_IListT()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<int>>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            var resultAsList = result.ToList();
            resultAsList.Count.Should().Be(3);
            resultAsList[0].Should().Be(5);
            resultAsList[1].Should().Be(6);
            resultAsList[2].Should().Be(7);
        }

        [Test]
        public void Map_IReadOnlyListT()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IReadOnlyList<int>>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            var resultAsList = result.ToList();
            resultAsList.Count.Should().Be(3);
            resultAsList[0].Should().Be(5);
            resultAsList[1].Should().Be(6);
            resultAsList[2].Should().Be(7);
        }
    }
}