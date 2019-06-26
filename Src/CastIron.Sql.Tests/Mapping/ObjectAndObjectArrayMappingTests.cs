using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
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
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().Be("TEST");
            dict["TestBool"].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<object>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();
            result.Should().BeOfType<Dictionary<string, object>>();

            var dict = result as Dictionary<string, object>;
            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
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
            string testString = result.TestString;
            testString.Should().Be("TEST");

            int testInt = (int)result.TestInt;
            testInt.Should().Be(5);
        }

        [Test]
        public void TestQuery_MapToIList()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IList>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<object[]>();

            result.Count.Should().Be(3);
            result[0].Should().Be(5);
            result[1].Should().Be("TEST");
            result[2].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToICollection()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<ICollection>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<object[]>();

            result.Count.Should().Be(3);
            var list = result.Cast<object>().ToList();
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToIEnumerable()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IEnumerable>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<object[]>();

            var list = result.Cast<object>().ToList();
            list.Count.Should().Be(3);
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }

        [Test]
        public void TestQuery_MapToIEnumerableOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IEnumerable<object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Should().BeOfType<List<object>>();

            var list = result.ToList();
            list.Count.Should().Be(3);
            list[0].Should().Be(5);
            list[1].Should().Be("TEST");
            list[2].Should().Be(true);
        }
    }
}