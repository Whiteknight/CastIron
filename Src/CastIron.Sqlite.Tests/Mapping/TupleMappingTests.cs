using System;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class TupleMappingTests
    {
        [Test]
        public void Map_Tuple()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Tuple<int, string, bool>>("SELECT 5, 'TEST', 1;").First();
            result.Item1.Should().Be(5);
            result.Item2.Should().Be("TEST");
            result.Item3.Should().Be(true);
        }

        public class TestObjectWithTuple
        {
            public int Id { get; set; }
            public Tuple<string, int, float> Items { get; set; }
        }

        [Test]
        public void Map_ObjectWithTupleProperty()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObjectWithTuple>("SELECT 5 AS Id, 'TEST' AS Items, 6 AS Items, 3.14 AS Items").First();
            result.Id.Should().Be(5);
            result.Items.Item1.Should().Be("TEST");
            result.Items.Item2.Should().Be(6);
            result.Items.Item3.Should().Be(3.14f);
        }

        [Test]
        public void Map_TupleTooFewColumns()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Tuple<int, string, int?>>("SELECT 5, 'TEST';").First();
            result.Item1.Should().Be(5);
            result.Item2.Should().Be("TEST");
            result.Item3.Should().Be(null);
        }
    }
}
