using System;
using System.Linq;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
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
        public void TestQuery1_Properties([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery1_ConstructorParams_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery1());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("TEST");
            result.TestBool.Should().Be(true);
        }

        public class TestQuery_Unbox : ISqlQuerySimple<TestObject1>
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
        public void TestQuery_UnboxProperties([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery_Unbox());
            result.TestInt.Should().Be(5);
            result.TestString.Should().Be("6");
            result.TestBool.Should().Be(true);
        }

        [Test]
        public void TestQuery_MapStringToDateTime([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new SqlQuery<DateTime>("SELECT '2018-12-11 17:01:02'")).First();
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
        public void Primitive_String([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<string>("SELECT 'TEST' AS TestString;"));
            result.Should().Be("TEST");
        }

        [Test]
        public void Primitive_StringNull([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<string>("SELECT NULL AS TestString;"));
            result.Should().BeNull();
        }

        [Test]
        public void Primitive_int([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<int>("SELECT 5 AS TestInt;"));
            result.Should().Be(5);
        }

        [Test]
        public void Primitive_intToLong([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<long>("SELECT 5 AS TestInt;"));
            result.Should().Be(5L);
        }

        [Test]
        public void Primitive_longToint([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<int>("SELECT CAST(5 AS BIGINT) AS TestInt;"));
            result.Should().Be(5);
        }

        [Test]
        public void Primitive_int_NoColumnName([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<int>("SELECT 5;"));
            result.Should().Be(5);
        }

        [Test]
        public void Primitive_int_Null([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<int>("SELECT NULL AS TestInt;"));
            result.Should().Be(0);
        }

        [Test]
        public void Primitive_int_Nullable([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<int?>("SELECT 5 AS TestInt;"));
            result.Should().Be(5);

            result = target.Query(new TestQuery<int?>("SELECT NULL AS TestInt;"));
            result.Should().BeNull();
        }

        [Test]
        public void Primitive_double([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<double>("SELECT 5.67 AS Test;"));
            result.Should().Be(5.67);
        }

        [Test]
        public void Primitive_decimal([Values("MSSQL", "SQLITE")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<decimal>("SELECT 5.67 AS TestDecimal;"));
            result.Should().Be(5.67M);
        }

        // TODO SQLite doesn't handle hex literals for binary(n), we need a new way to test it
        [Test]
        public void Primitive_binary_ByteArray([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<byte[]>("SELECT CAST(0x0102030405 AS Binary(5)) AS TestBinary"));
            result.Should().NotBeNull();
            result.Length.Should().Be(5);
            result.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
        }

        // SQLite doesn't support UUID natively
        [Test]
        public void Primitive_Guid_Null([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<Guid>("SELECT NEWID() AS TestId;"));
            result.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void Primitive_Guid_Nullable([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Query(new TestQuery<Guid?>("SELECT NEWID() AS TestId;"));
            result.Should().NotBe(Guid.Empty);

            result = target.Query(new TestQuery<Guid?>("SELECT NULL AS TestId;"));
            result.Should().BeNull();
        }

        //[Test]
        //public void TestQuery_dynamic([Values("MSSQL", "SQLITE")] string provider)
        //{
        //    var target = RunnerFactory.Create(provider);
        //    var result = target.Query(new SqlQuery<dynamic>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;")).First();
        //    result.TestString.Should().Be("TEST");
        //    result.TestInt.Should().Be(5);
        //    result.TestBool.Should().Be(true);
        //}
    }
}
