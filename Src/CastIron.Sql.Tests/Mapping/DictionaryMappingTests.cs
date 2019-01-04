using System.Collections.Generic;
using System.Linq;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class DictionaryMappingTests
    {
        [Test]
        public void Map_DictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<Dictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void Map_DictionaryOfObjectWithDuplicates([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var dict = target.Query(new SqlQuery<Dictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;")).First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_IDictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<IDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void Map_IDictionaryOfObjectWithDuplicates([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var dict = target.Query(new SqlQuery<IDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;")).First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        public class TestObject_ChildDict
        {
            public int Id { get; set; }
            public Dictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObjectWithDuplicates([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var dict = target.Query(new SqlQuery<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;")).First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_CustomObjectWithChildDictionary([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject_ChildDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;")).First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }

        public class TestObject_ChildIDict
        {
            public int Id { get; set; }
            public IDictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_CustomObjectWithChildIDictionary([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject_ChildIDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;")).First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }
    }
}