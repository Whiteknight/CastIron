using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class AbstractCollectionMappingTests
    {
        public class Map_StringList<T> : ISqlQuerySimple<T>
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

        public class TestObjectStringIList
        {
            public IList<string> TestString { get; set; }
        }

        [Test]
        public void Map_ObjectWithIListProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_StringList<TestObjectStringIList>());
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
        public void Map_ObjectWithIListCtorParameter()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_StringList<TestObjectStringIListCtor>());
            result.TestString.Count.Should().Be(3);
            result.TestString[0].Should().Be("5");
            result.TestString[1].Should().Be("TEST");
            result.TestString[2].Should().Be("3.14");
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