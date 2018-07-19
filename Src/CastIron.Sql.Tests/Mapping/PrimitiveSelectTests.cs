using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class PrimitiveSelectTests
    {
        public class TestQuery<T> : ISqlQuery<T>
        {
            private readonly string _sql;

            public TestQuery(string sql)
            {
                _sql = sql;
            }

            public string GetSql()
            {
                return _sql;
            }

            public T Read(SqlResultSet result)
            {
                return result.AsEnumerable<T>().First();
            }
        }

        [Test]
        public void Primitive_String()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<string>("SELECT 'TEST' AS TestString;"));
            result.Should().Be("TEST");
        }

        [Test]
        public void Primitive_StringNull()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<string>("SELECT NULL AS TestString;"));
            result.Should().BeNull();
        }

        [Test]
        public void Primitive_int()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int>("SELECT 5 AS TestInt;"));
            result.Should().Be(5);
        }

        [Test]
        public void Primitive_int_NoColumnName()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int>("SELECT 5;"));
            result.Should().Be(5);
        }

        [Test]
        public void Primitive_int_Null()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int>("SELECT NULL AS TestInt;"));
            result.Should().Be(0);
        }

        [Test]
        public void Primitive_intNullable()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int?>("SELECT 5 AS TestInt;"));
            result.Should().Be(5);

            result = target.Query(new TestQuery<int?>("SELECT NULL AS TestInt;"));
            result.Should().BeNull();
        }
    }
}