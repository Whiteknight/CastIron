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
            public int NumberValue { get; set; }
        }

        public class TestSmallNumber : TestNumber
        {

        }

        public class TestBigNumber : TestNumber
        {
        }

        public class TestNumberQuery : ISqlQuerySimple<IReadOnlyList<TestNumber>>
        {
            public string GetSql()
            {
                return @"
                    DECLARE @values TABLE (NumberValue INT);
                    INSERT INTO @values(NumberValue) VALUES (1), (2), (3), (4), (5);
                    SELECT NumberValue from @values;";
            }

            public IReadOnlyList<TestNumber> Read(IDataResults result)
            {
                return result
                    .AsEnumerable<TestNumber>(c => c
                        .UseClass<TestSmallNumber>()
                        .UseSubclass<TestBigNumber>(r => r.GetInt32(0) > 3))
                    .ToList();
            }
        }

        [Test]
        public void CanInstantiateSubclasses_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new TestNumberQuery());

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