using System;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class PrimitiveMappingTests
    {
        public class TestObject1
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }
        }

        public class TestQuery1 : ISqlQuerySimple<TestObject1>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
            }

            public TestObject1 Read(IDataResults result)
            {
                return result.AsEnumerable<TestObject1>().FirstOrDefault();
            }
        }

        [Test]
        public void TestQuery1_Properties()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery1_ConstructorParams_Test()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        public class Map_Unbox : ISqlQuerySimple<TestObject1>
        {
            public string GetSql()
            {
                return "SELECT CAST(5 AS BIGINT) AS TestInt, 6 AS TestString, 1 AS TestBool;";
            }

            public TestObject1 Read(IDataResults result)
            {
                return result.AsEnumerable<TestObject1>().FirstOrDefault();
            }
        }

        [Test]
        public void Map_UnboxProperties()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_Unbox());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("6");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void Map_MapStringToDateTime()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<DateTime>("SELECT '2018-12-11 17:01:02'").First();
            result.Should().Be(new DateTime(2018, 12, 11, 17, 1, 2));
        }

        public class TestQuery<T> : ISqlQuerySimple<T>
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

            public T Read(IDataResults result)
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
        public void Primitive_intToLong()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<long>("SELECT 5 AS TestInt;"));
            result.Should().Be(5L);
        }

        [Test]
        public void Primitive_longToint()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int>("SELECT CAST(5 AS BIGINT) AS TestInt;"));
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
        public void Primitive_int_Nullable()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<int?>("SELECT 5 AS TestInt;"));
            result.Should().Be(5);

            result = target.Query(new TestQuery<int?>("SELECT NULL AS TestInt;"));
            result.Should().BeNull();
        }

        [Test]
        public void Primitive_double()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<double>("SELECT 5.67 AS Test;"));
            result.Should().Be(5.67);
        }

        [Test]
        public void Primitive_decimal()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<decimal>("SELECT 5.67 AS TestDecimal;"));
            result.Should().Be(5.67M);
        }

        [Test]
        public void Primitive_binary_ByteArray()
        {

            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<byte[]>("SELECT CAST(E'\\\\x0102030405' AS BYTEA) AS TestBinary"));
            result.Should().NotBeNull();
            result.Length.Should().Be(5);
            result.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void Primitive_string_Guid()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery<Guid>("SELECT '12345678-1234-1234-1234-123456789012' AS TestId;"));
            result.Should().NotBe(Guid.Empty);

            var result2 = target.Query(new TestQuery<Guid?>("SELECT '12345678-1234-1234-1234-123456789012' AS TestId;"));
            result2.Should().NotBeNull();
            result2.Should().NotBe(Guid.Empty);
        }
    }
}
