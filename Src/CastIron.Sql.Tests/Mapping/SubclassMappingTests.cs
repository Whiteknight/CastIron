using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    public class SubclassMappingTests
    {
        public abstract class TestNumber
        {
            public int Number { get; set; }
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
                    DECLARE @values TABLE (Number INT);
                    INSERT INTO @values(Number) VALUES (1), (2), (3), (4), (5);
                    SELECT Number from @values;";
            }

            public IReadOnlyList<TestNumber> Read(SqlResultSet result)
            {
                return result
                    .AsEnumerable<TestNumber>(c => c
                        .UseSubclass<TestBigNumber>(r => r.GetInt32(0) > 3)
                        .Otherwise<TestSmallNumber>())
                    .ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new MSSQLTestNumberQuery());

            result.Count.Should().Be(5);
            result[0].Should().BeOfType<TestSmallNumber>();
            result[0].Number.Should().Be(1);

            result[1].Should().BeOfType<TestSmallNumber>();
            result[1].Number.Should().Be(2);

            result[2].Should().BeOfType<TestSmallNumber>();
            result[2].Number.Should().Be(3);

            result[3].Should().BeOfType<TestBigNumber>();
            result[3].Number.Should().Be(4);

            result[4].Should().BeOfType<TestBigNumber>();
            result[4].Number.Should().Be(5);
        }

        public class SQLiteTestNumberQuery : ISqlQuerySimple<IReadOnlyList<TestNumber>>
        {
            public string GetSql()
            {
                // TODO: There must be a more concise syntax for sqlite
                return @"                    
                    SELECT 1 AS NUMBER
                    UNION ALL
                    SELECT 2 AS NUMBER
                    UNION ALL
                    SELECT 3 AS NUMBER
                    UNION ALL
                    SELECT 4 AS NUMBER
                    UNION ALL
                    SELECT 5 AS NUMBER
                    ;";
            }

            public IReadOnlyList<TestNumber> Read(SqlResultSet result)
            {
                return result
                    .AsEnumerable<TestNumber>(c => c
                        .UseSubclass<TestBigNumber>(r => r.GetInt32(0) > 3)
                        .Otherwise<TestSmallNumber>())
                    .ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test_Sqlite([Values("SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new SQLiteTestNumberQuery());

            result.Count.Should().Be(5);
            result[0].Should().BeOfType<TestSmallNumber>();
            result[0].Number.Should().Be(1);

            result[1].Should().BeOfType<TestSmallNumber>();
            result[1].Number.Should().Be(2);

            result[2].Should().BeOfType<TestSmallNumber>();
            result[2].Number.Should().Be(3);

            result[3].Should().BeOfType<TestBigNumber>();
            result[3].Number.Should().Be(4);

            result[4].Should().BeOfType<TestBigNumber>();
            result[4].Number.Should().Be(5);
        }
    }
}