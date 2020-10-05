using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    public class SubclassMappingTests
    {
        public abstract class TestNumber
        {
            public int NumberValue { get; set; }
        }

        public class TestSmallNumber : TestNumber
        {

        }

        public class TestBigNumber : TestNumber
        {
        }

        public class MSSQLTestNumberQuery : ISqlQuerySimple<IReadOnlyList<TestNumber>>
        {
            public string GetSql()
            {
                return @"
                    CREATE TEMP TABLE v (NumberValue INT);
                    INSERT INTO v(NumberValue) VALUES (1), (2), (3), (4), (5);
                    SELECT NumberValue from v;";
            }

            public IReadOnlyList<TestNumber> Read(IDataResults result)
            {
                return result
                    .AsEnumerable<TestNumber>(b => b
                        .For<TestNumber>(c => c
                            .UseType<TestSmallNumber>()
                            .UseSubtype<TestBigNumber>(r => r.GetInt32(0) > 3)
                        )
                    )
                    .ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new MSSQLTestNumberQuery());

            result.Count.Should().Be(5);
            result[0].Should().BeOfType<TestSmallNumber>();
            result[0].NumberValue.Should().Be(1);

            result[1].Should().BeOfType<TestSmallNumber>();
            result[1].NumberValue.Should().Be(2);

            result[2].Should().BeOfType<TestSmallNumber>();
            result[2].NumberValue.Should().Be(3);

            result[3].Should().BeOfType<TestBigNumber>();
            result[3].NumberValue.Should().Be(4);

            result[4].Should().BeOfType<TestBigNumber>();
            result[4].NumberValue.Should().Be(5);
        }

        public class SQLiteTestNumberQuery : ISqlQuerySimple<IReadOnlyList<TestNumber>>
        {
            public string GetSql()
            {
                return @"
                    WITH cte(NumberValue) AS (
                        VALUES (1),(2),(3),(4),(5)
                    ) 
                    SELECT * from cte;";
            }

            public IReadOnlyList<TestNumber> Read(IDataResults result)
            {
                return result
                    .AsEnumerable<TestNumber>(b => b
                        .For<TestNumber>(c => c
                            .UseType<TestSmallNumber>()
                            .UseSubtype<TestBigNumber>(r => r.GetInt32(0) > 3)
                        )
                    )
                    .ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test_Sqlite()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new SQLiteTestNumberQuery());

            result.Count.Should().Be(5);
            result[0].Should().BeOfType<TestSmallNumber>();
            result[0].NumberValue.Should().Be(1);

            result[1].Should().BeOfType<TestSmallNumber>();
            result[1].NumberValue.Should().Be(2);

            result[2].Should().BeOfType<TestSmallNumber>();
            result[2].NumberValue.Should().Be(3);

            result[3].Should().BeOfType<TestBigNumber>();
            result[3].NumberValue.Should().Be(4);

            result[4].Should().BeOfType<TestBigNumber>();
            result[4].NumberValue.Should().Be(5);
        }
    }
}