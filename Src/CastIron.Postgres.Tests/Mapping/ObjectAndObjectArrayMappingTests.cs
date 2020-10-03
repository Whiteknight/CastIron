using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class ObjectAndObjectArrayMappingTests
    {
        [Test]
        public void TestQuery_MapToObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<object>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<Dictionary<string, object>>();

            var dict = result as Dictionary<string, object>;
            dict.Count.Should().Be(3);
            dict["testint"].Should().Be(5);
            dict["teststring"].Should().Be("TEST");
            dict["testbool"].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<object>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();
            result.Should().BeOfType<Dictionary<string, object>>();

            var dict = result as Dictionary<string, object>;
            dict.Count.Should().Be(2);
            dict["testint"].Should().Be(5);
            dict["teststring"].Should().BeOfType<object[]>();
            var array = dict["teststring"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void TestQuery_MapToObjectArray()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<object[]>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<object[]>();

            result.Length.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }

        [Test]
        public void TestQuery_dynamic()
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