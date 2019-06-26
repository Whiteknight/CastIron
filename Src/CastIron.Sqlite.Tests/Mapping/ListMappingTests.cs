using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class ListMappingTests
    {
        public class TestObject1
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }
        }

        [Test]
        public void TestQuery_ListOfCustomObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<List<TestObject1>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery_IListOfCustomObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList<TestObject1>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(1);
            result[0].TestString.Should().Be("TEST");
            result[0].TestInt.Should().Be(5);
            result[0].TestBool.Should().Be(true);
        }
    }
}