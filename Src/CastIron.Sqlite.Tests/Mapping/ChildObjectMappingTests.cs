using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class ChildObjectMappingTests
    {
        public class TestObject_WithChild
        {
            public int Id { get; set; }

            public class TestChild
            {
                public string Name { get; set; }
            }

            public TestChild Child { get; set; }
        }

        [Test]
        public void TestQuery_ObjectWithChild()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_WithChild>("SELECT 5 AS Id, 'TEST' AS Child_Name;").First();
            result.Id.Should().Be(5);
            result.Child.Name.Should().Be("TEST");
        }


        [Test]
        public void TestQuery_ObjectWithChildCustomSeparator()
        {
            var target = RunnerFactory.Create();
            var query = SqlQuery.FromString<TestObject_WithChild>("SELECT 5 AS Id, 'TEST' AS ChildXName;", setup: c => c.UseChildSeparator("X"));
            var result = target.Query(query).First();
            result.Id.Should().Be(5);
            result.Child.Name.Should().Be("TEST");
        }

        public class TestObject_WithChildObject
        {
            public int Id { get; set; }
            public object Value { get; set; }
        }

        [Test]
        public void TestQuery_ObjectPropertySingleValue()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_WithChildObject>("SELECT 6 AS Id, 'A' AS Value").First();
            result.Id.Should().Be(6);
            result.Value.Should().BeOfType<string>();
            (result.Value as string).Should().Be("A");
        }

        [Test]
        public void TestQuery_ObjectPropertyMultipleValues()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_WithChildObject>("SELECT 6 AS Id, 'A' AS Value, 'B' AS Value, 'C' AS value").First();
            result.Id.Should().Be(6);
            result.Value.Should().BeOfType<object[]>();
            var objArray = result.Value as object[];
            objArray[0].Should().Be("A");
            objArray[1].Should().Be("B");
            objArray[2].Should().Be("C");
        }

        [Test]
        public void TestQuery_ObjectWithChildNull()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_WithChildObject>("SELECT 5 AS Id;").First();
            result.Id.Should().Be(5);
            result.Value.Should().BeNull();
        }

        public class TestObject_WithNestedChildren
        {
            public class ChildB
            {
                public string Value { get; set; }
            }

            public class ChildA
            {
                public ChildB B { get; set; }
            }

            public ChildA A { get; set; }
        }

        [Test]
        public void TestQuery_NestedChildren()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_WithNestedChildren>("SELECT 'TEST' AS A_B_Value").Single();
            result?.A?.B?.Value.Should().Be("TEST");
        }

        [Test]
        public void TestQuery_NestedChildrenCustomSeparator()
        {
            var target = RunnerFactory.Create();
            var query = SqlQuery.FromString<TestObject_WithNestedChildren>("SELECT 'TEST' AS AXBXValue", setup: c => c.UseChildSeparator("X"));
            var result = target.Query(query).Single();
            result?.A?.B?.Value.Should().Be("TEST");
        }
    }
}