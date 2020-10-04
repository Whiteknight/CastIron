using System.Dynamic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    public class ExpandObjectMappingTests
    {

        [Test]
        public void Map_dynamic()
        {
            var target = RunnerFactory.Create();
            dynamic result = target.Query<ExpandoObject>("SELECT 5 AS TestInt, 'TEST' AS TestString;").First();
            string testString = result.teststring;
            testString.Should().Be("TEST");

            int testInt = (int)result.testint;
            testInt.Should().Be(5);
        }
    }
}