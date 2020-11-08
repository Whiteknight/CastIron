using System;
using System.Linq;
using CastIron.Sql;
using CastIron.Sql.Mapping;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class MappingTests
    {
        [Test]
        public void AdvanceEndsExistingEnumerators()
        {
            var target = RunnerFactory.Create();
            using var stream = target.QueryStream("SELECT 5; SELECT 6");

            // We start an enumerable on result set 1, but then immediately advance to result
            // set 2. This forwards past all remaining items in result set 1 so the first 
            // enumerable will be empty
            var first = stream.AsEnumerable<int>();
            stream.AdvanceToNextResultSet();
            var second = stream.AsEnumerable<int>();

            var firstResults = first.ToList();
            firstResults.Count.Should().Be(0);

            var secondResults = second.ToList();
            secondResults.Count().Should().Be(1);
            secondResults[0].Should().Be(6);
        }

        [Test]
        public void AdvanceEndsEnumerateNextSeveral()
        {
            var target = RunnerFactory.Create();
            using var stream = target.QueryStream("SELECT 5; SELECT 6");

            // We start an enumerable which should cover the next two result sets, but then manually
            // advance the result set before the first enumerable is complete. This puts the 
            // reader into an unknown state, because the first two result sets were supposed to
            // be handled by the first enumerable, but now we're already at result set 2. This
            // should throw an exception
            var first = stream.AsEnumerableNextSeveral<int>(2).GetEnumerator();
            first.MoveNext();
            stream.AdvanceToNextResultSet();
            var second = stream.AsEnumerable<int>();

            Action act = () => first.MoveNext();
            act.Should().Throw<DataReaderException>();
        }
    }
}
