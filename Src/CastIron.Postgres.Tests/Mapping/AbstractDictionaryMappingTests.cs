using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{

    public class AbstractDictionaryMappingTests
    {
        [Test]
        public void Map_IDictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["teststring"].Should().Be("TEST");
            result["testint"].Should().Be(5);
            result["testbool"].Should().Be(true);
        }

        [Test]
        public void Map_IDictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<IDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["testint"].Should().Be(5);
            dict["teststring"].Should().BeOfType<object[]>();
            var array = dict["teststring"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["teststring"].Should().Be("TEST");
            result["testint"].Should().Be(5);
            result["testbool"].Should().Be(true);
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["testint"].Should().Be(5);
            dict["teststring"].Should().BeOfType<object[]>();
            var array = dict["teststring"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        public class TestObject_ChildIDict
        {
            public int Id { get; set; }
            public IDictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_CustomObjectWithChildIDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildIDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["string"].Should().Be("TEST");
            result.Child["int"].Should().Be("6");
        }
    }
}