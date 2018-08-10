using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class MapOntoTests
    {
        class ResultRecord
        {
            public ResultRecord(int id)
            {
                Id = id;
            }

            public int Id { get;  }
            public string Name { get; set; }
            public string Value { get; set; }
        }

        class PartialRecordValue
        {
            public PartialRecordValue(int id, string value)
            {
                Id = id;
                Value = value;
            }

            public int Id { get; }
            public string Value { get; }
        }

        class MapOntoQuery_RawRecord : ISqlQuerySimple<ResultRecord>
        {
            public string GetSql()
            {
                return @"
                    SELECT 1 AS Id, 'TEST' AS Name;
                    SELECT 1 AS Id, 'VALUE' AS Value;";
            }

            public ResultRecord Read(SqlResultSet result)
            {
                var reader = result.AsResultMapper();
                var records = reader.GetNextEnumerable<ResultRecord>().ToDictionary(r => r.Id);
                reader
                    .GetNextEnumerable(r => new
                    {
                        Id = r.GetInt32(0),
                        Value = r.GetString(1)
                    })
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

        class MapOntoQuery_MappedRecord : ISqlQuerySimple<ResultRecord>
        {
            public string GetSql()
            {
                return @"
                    SELECT 1 AS Id, 'TEST' AS Name;
                    SELECT 1 AS Id, 'VALUE' AS Value;";
            }

            public ResultRecord Read(SqlResultSet result)
            {
                var reader = result.AsResultMapper();
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
