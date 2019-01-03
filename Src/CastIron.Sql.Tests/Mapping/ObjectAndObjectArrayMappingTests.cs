using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class ObjectAndObjectArrayMappingTests
    {
        [Test]
        public void TestQuery_MapToObject([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<object>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Should().BeOfType<Dictionary<string, object>>();

            var array = result as Dictionary<string, object>;
            array.Count.Should().Be(3);
            array["TestInt"].Should().Be(5);
            array["TestString"].Should().Be("TEST");
            array["TestBool"].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToObjectArray([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<object[]>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
            result.Should().BeOfType<object[]>();

            result.Length.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }

        [Test]
        public void TestQuery_dynamic([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            dynamic result = target.Query(new SqlQuery<ExpandoObject>("SELECT 5 AS TestInt, 'TEST' AS TestString;")).First();
            string testString = result.TestString;
            testString.Should().Be("TEST");

            int testInt = (int)result.TestInt;
            testInt.Should().Be(5);
        }
    }
}