using System.Linq;
using CastIron.Sql;
using CastIron.Sql.Mapping;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class CustomObjectMappingTests
    {
        public class CustomObject1
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Value { get; set; }
        }

        [Test]
        public void CustomObject_Test()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<CustomObject1>("SELECT 1 AS Id, 'TEST' AS Name, 3.14 AS Value").Single();
            result.Id.Should().Be(1);
            result.Name.Should().Be("TEST");
            result.Value.Should().Be(3.14);
        }

        [Test]
        public void CustomObject_IgnorePrefixes()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<CustomObject1>(
                    "SELECT 1 AS XXXId, 'TEST' AS YYYName, 3.14 AS XXXValue",
                    s => s.IgnorePrefixes("XXX")
                )
                .Single();
            result.Id.Should().Be(1);
            result.Name.Should().BeNull();
            result.Value.Should().Be(3.14);
        }
    }
}
