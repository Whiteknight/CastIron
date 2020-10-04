using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class ValueTupleMappingTests
    {
        [Test]
        public void Map_Tuple()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<(int, string, bool)>("SELECT 5, 'TEST', 1;").First();
            result.Item1.Should().Be(5);
            result.Item2.Should().Be("TEST");
            result.Item3.Should().Be(true);
        }

        public class TestObjectWithTuple
        {
            public int Id { get; set; }
            public (string, int, float) Items { get; set; }
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
            var result = target.Query<(int, string, int?)>("SELECT 5, 'TEST';").First();
            result.Item1.Should().Be(5);
            result.Item2.Should().Be("TEST");
            result.Item3.Should().Be(null);
        }

        public class TestObject1
        {
            public string A { get; set; }
            public string B { get; set; }
        }

        [Test]
        public void Map_Tuple_CustomObject1()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<(int, TestObject1)>("SELECT 5 AS Id, 'TEST1' AS A, 'TEST2' AS B").First();
            result.Item1.Should().Be(5);
            result.Item2.A.Should().Be("TEST1");
            result.Item2.B.Should().Be("TEST2");
        }

        [Test]
        public void Map_Tuple_CustomObject2()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<(TestObject1 first, int second, TestObject1 third)>("SELECT 5 AS Id, 'TEST1' AS A, 'TEST2' AS B, 'TEST3' AS A, 'TEST4' AS B").First();
            result.first.A.Should().Be("TEST1");
            result.first.B.Should().Be("TEST2");
            result.second.Should().Be(5);
            result.third.A.Should().Be("TEST3");
            result.third.B.Should().Be("TEST4");
        }

        [Test]
        public void Map_Tuple_object()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<(int, object)>("SELECT 5 AS Id, 'TEST1' AS A, 'TEST2' AS B").First();
            result.Item1.Should().Be(5);
            var item2 = result.Item2 as Dictionary<string, object>;
            item2["A"].Should().Be("TEST1");
            item2["B"].Should().Be("TEST2");
        }
    }
}
