using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class EnumMappingTests
    {
        private enum IntBasedEnum
        {
            ValueA,
            ValueB,
            ValueC
        }

        [Test]
        public void Map_CaseSensitiveString()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query<IntBasedEnum>("SELECT 'ValueA'").Single();
            result.Should().Be(IntBasedEnum.ValueA);
        }

        [Test]
        public void Map_CaseInsensitiveString()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query<IntBasedEnum>("SELECT 'valueb'").Single();
            result.Should().Be(IntBasedEnum.ValueB);
        }

        [Test]
        public void Map_int_IntBasedEnum()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query<IntBasedEnum>("SELECT 1").Single();
            result.Should().Be(IntBasedEnum.ValueB);
        }


        private enum ByteBasedEnum : byte
        {
            ValueA,
            ValueB,
            ValueC
        }

        [Test]
        public void Map_int_ByteBasedEnum()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query<ByteBasedEnum>("SELECT 1").Single();
            result.Should().Be(ByteBasedEnum.ValueB);
        }
    }
}
