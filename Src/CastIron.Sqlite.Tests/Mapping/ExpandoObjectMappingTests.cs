using System.Dynamic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class ExpandoObjectMappingTests
    {
        [Test]
        public void TestQuery_dynamic()
        {
            var target = RunnerFactory.Create();
            dynamic result = target.Query<ExpandoObject>("SELECT 5 AS TestInt, 'TEST' AS TestString;").First();
            string testString = result.TestString;
            testString.Should().Be("TEST");

            int testInt = (int)result.TestInt;
            testInt.Should().Be(5);
        }
    }
}