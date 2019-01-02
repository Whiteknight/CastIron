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
        public void TestQuery_DictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<Dictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void TestQuery_IDictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<IDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void TestQuery_IReadOnlyDictionaryOfObject([Values("MSSQL", "SQLITE")] string provider)
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
        public void TestQuery_CustomObjectWithChildDictionary([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject_ChildDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;")).First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }
    }
}