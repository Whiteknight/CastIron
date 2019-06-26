using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class MappingExtensionsTests
    {
        private const string Query = @"
            SELECT 1;
            SELECT 2 UNION ALL SELECT 3;
            SELECT 4;
            SELECT 5 UNION ALL SELECT 6;";

        [Test]
        public void AsEnumerableAll_Test()
        {
            var target = RunnerFactory.Create();
            using (var stream = target.QueryStream(Query))
            {
                var results = stream.AsEnumerableAll<int>().ToList();
                results.Count.Should().Be(6);
                results.Should().ContainInOrder(1, 2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void AsEnumerableNextSeveral_Test()
        {
            var target = RunnerFactory.Create();
            using (var stream = target.QueryStream(Query))
            {
                var results = stream.AsEnumerableNextSeveral<int>(3).ToList();
                results.Count.Should().Be(4);
                results.Should().ContainInOrder(1, 2, 3, 4);
            }
        }
    }
}