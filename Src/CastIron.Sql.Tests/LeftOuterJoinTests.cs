using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    public class LeftOuterJoinTests
    {
        [Test]
        public void GroupedLeftOuterJoin_Test()
        {
            var left = new int[]
            {
                1, 2, 3, 4
            };
            var right = new string[]
            {
                "a",
                "bc",
                "de",
                "fgh"
            };
            var results = left
                .GroupedLeftOuterJoin(right, i => i, s => s.Length)
                .ToDictionary(x => x.Left, x => x.Right.ToArray());

            results.Count.Should().Be(4);
            results[1].Should().Contain("a");
            results[2].Should().Contain("bc", "de");
            results[3].Should().Contain("fgh");
            results[4].Should().BeEmpty();
        }

        [Test]
        public void LeftOuterJoin_Test()
        {
            var left = new int[]
            {
                1, 2, 3, 4
            };
            var right = new string[]
            {
                "a",
                "bc",
                "de",
                "fgh"
            };
            var results = left
                .LeftOuterJoin(right, i => i, s => s.Length)
                .Select(x => $"{x.Left}:{x.Right ?? ""}")
                .ToList();

            results.Count.Should().Be(5);
            results.Should().Contain("1:a", "2:bc", "2:de", "3:fgh", "4:");
        }
    }
}
