using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
{
    [TestFixture]
    public class MultiSelectTests
    {
        public class TestObject
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
        }

        public class TestQuery1 : ISqlQuerySimple<TestObject>
        {
            public string GetSql()
            {
                return @"
                    SELECT 5 AS TestInt;
                    SELECT 'TEST' AS TestString;";
            }

            public TestObject Read(IDataResults result)
            {
                var obj1 = result.AsEnumerable<TestObject>().First();
                var obj2 = result.GetNextEnumerable<TestObject>().First();
                obj1.TestString = obj2.TestString;
                return obj1;
            }
        }

        [Test]
        public void TestQuery1_Test()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
        }
    }
}