using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
{
    [TestFixture]
    public class MapOntoTests
    {
        private class ResultRecord
        {
            public ResultRecord(int id)
            {
                Id = id;
            }

            public int Id { get; }
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class PartialRecordValue
        {
            public PartialRecordValue(int id, string value)
            {
                Id = id;
                Value = value;
            }

            public int Id { get; }
            public string Value { get; }
        }

        private class MapOntoQuery_RawRecord : ISqlQuerySimple<ResultRecord>
        {

            public string GetSql()
            {
                return @"
                    SELECT 1 AS Id, 'TEST' AS Name;
                    SELECT 1 AS Id, 'VALUE' AS Value;";
            }

            public ResultRecord Read(IDataResults reader)
            {
                var records = reader.GetNextEnumerable<ResultRecord>().ToDictionary(r => r.Id);
                reader.GetNextEnumerable<PartialRecordValue>()
                    .MapOnto(x => records[x.Id], (x, r) => r.Value = x.Value);
                return records.Values.Single();
            }
        }

        [Test]
        public void MapOnto_Test()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new MapOntoQuery_RawRecord());
            result.Id.Should().Be(1);
            result.Name.Should().Be("TEST");
            result.Value.Should().Be("VALUE");
        }

        private class MapOntoQuery_MappedRecord : ISqlQuerySimple<ResultRecord>
        {
            public string GetSql()
            {
                return @"
                    SELECT 1 AS Id, 'TEST' AS Name;
                    SELECT 1 AS Id, 'VALUE' AS Value;";
            }

            public ResultRecord Read(IDataResults reader)
            {
                var records = reader.GetNextEnumerable<ResultRecord>().ToDictionary(r => r.Id);
                reader.GetNextEnumerable<PartialRecordValue>().MapOnto(p => records[p.Id], (p, r) => r.Value = p.Value);
                return records.Values.Single();
            }
        }

        [Test]
        public void MapOnto_MappedTest()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new MapOntoQuery_MappedRecord());
            result.Id.Should().Be(1);
            result.Name.Should().Be("TEST");
            result.Value.Should().Be("VALUE");
        }
    }
}
