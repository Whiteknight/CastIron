using System;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests.Mapping
{
    [TestFixture]
    public class FactoryMethodMappingTests
    {
        private class ResultRecord
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class FactoryMethodReturnsNullQuery : ISqlQuerySimple<ResultRecord>
        {
            public string GetSql()
            {
                return @"SELECT 1 AS Id, 'TEST' AS Name;";
            }

            public ResultRecord Read(IDataResults reader)
            {
                return reader.AsEnumerable<ResultRecord>(b => b.UseFactoryMethod(() => null)).FirstOrDefault();
            }
        }

        [Test]
        public void FactoryMethod_ReturnsNull()
        {
            var target = RunnerFactory.Create();
            Action func = () =>
            {
                var result = target.Query(new FactoryMethodReturnsNullQuery());
            };
            func.Should().Throw<Exception>();
        }

        private class FactoryMethodReturnsNonNullQuery : ISqlQuerySimple<ResultRecord>
        {
            public string GetSql()
            {
                return @"SELECT 1 AS Id, 'TEST' AS Name;";
            }

            public ResultRecord Read(IDataResults reader)
            {
                return reader.AsEnumerable<ResultRecord>(b => b.UseFactoryMethod(() => new ResultRecord())).FirstOrDefault();
            }
        }

        [Test]
        public void FactoryMethod_ReturnsNonNull()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new FactoryMethodReturnsNonNullQuery());
            result.Id.Should().Be(1);
            result.Name.Should().Be("TEST");
        }
    }
}
