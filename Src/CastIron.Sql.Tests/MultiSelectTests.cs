using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class MultiSelectTests
    {
        public class TestObject
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
        }

        public class TestQuery1 : ISqlQuery<TestObject>
        {
            public string GetSql()
            {
                return @"
                    SELECT 5 AS TestInt;
                    SELECT 'TEST' AS TestString;";
            }

            public TestObject Read(SqlQueryResult result)
            {
                var mapper = result.AsResultMapper();
                var obj1 = mapper.AsEnumerable<TestObject>().First();

                // TODO: This part should get better, we should be able to map onto an existing object by key
                mapper.NextResultSet();
                var obj2 = mapper.AsEnumerable<TestObject>().First();
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