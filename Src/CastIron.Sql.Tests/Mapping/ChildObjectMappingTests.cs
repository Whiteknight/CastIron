using System.Linq;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
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
        public void TestQuery_ObjectWithChild([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<TestObject_WithChild>("SELECT 5 AS Id, 'TEST' AS Child_Name;")).First();
            result.Id.Should().Be(5);
            result.Child.Name.Should().Be("TEST");
        }
    }
}