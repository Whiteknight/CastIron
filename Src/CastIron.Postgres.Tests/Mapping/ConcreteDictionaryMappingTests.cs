using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class ConcreteDictionaryMappingTests
    {
        [Test]
        public void Map_DictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Dictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["teststring"].Should().Be("TEST");
            result["testint"].Should().Be(5);
            result["testbool"].Should().Be(true);
        }

        [Test]
        public void Map_DictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<Dictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["testint"].Should().Be(5);
            dict["teststring"].Should().BeOfType<object[]>();
            var array = dict["teststring"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        public class TestObject_ChildDict
        {
            public int Id { get; set; }
            public Dictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_CustomObjectWithChildDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["string"].Should().Be("TEST");
            result.Child["int"].Should().Be("6");
        }

        [Test]
        public void Map_DictionaryOfListOfInt()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<Dictionary<string, List<int>>>("SELECT 1 AS A, 2 AS A, 3 AS B, 4 AS B, 5 AS C;").First();

            dict.Count.Should().Be(3);
            dict["a"].Should().ContainInOrder(1, 2);
            dict["b"].Should().ContainInOrder(3, 4);
            dict["c"].Should().ContainInOrder(5);
        }
    }
}