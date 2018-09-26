using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class SubclassMappingTests
    {
        public class TestNumber
        {
            public int Number { get; set; }
        }

        public class TestBigNumber : TestNumber
        {
        }

        public class TestNumberQuery : ISqlQuerySimple<IReadOnlyList<TestNumber>>
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
                return result.AsEnumerable<TestNumber>(c => c.HandleSubclass<TestBigNumber>(r => r.GetInt32(0) > 3)).ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new TestNumberQuery());

            result.Count.Should().Be(5);
            result[0].Should().NotBeOfType<TestBigNumber>();
            result[0].Number.Should().Be(1);

            result[1].Should().NotBeOfType<TestBigNumber>();
            result[1].Number.Should().Be(2);

            result[2].Should().NotBeOfType<TestBigNumber>();
            result[2].Number.Should().Be(3);

            result[3].Should().BeOfType<TestBigNumber>();
            result[3].Number.Should().Be(4);

            result[4].Should().BeOfType<TestBigNumber>();
            result[4].Number.Should().Be(5);
        }
    }
}