using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    [TestFixture]
    public class AffectedRowsTests
    {

        public class Query : ISqlQuerySimple<int>
        {
            public string GetSql()
            {
                return @"
                    CREATE TEMPORARY TABLE temp (X INT NOT NULL);
                    INSERT INTO temp(X) VALUES (1),(2),(3),(4);";
            }

            public int Read(IDataResults result)
            {
                return result.RowsAffected;
            }
        }

        [Test]
        public void Query_RowsAffected()
        {
            var runner = RunnerFactory.Create();
            var rowsAffected = runner.Query(new Query());
            rowsAffected.Should().Be(4);
        }

        public class Command : ISqlCommandSimple<int>
        {
            public string GetSql()
            {
                return @"
                    CREATE TEMPORARY TABLE temp (X INT NOT NULL);
                    INSERT INTO temp(X) VALUES (1),(2),(3),(4);";
            }

            public int ReadOutputs(IDataResults result)
            {
                return result.RowsAffected;
            }
        }

        [Test]
        public void Execute_RowsAffected()
        {
            var runner = RunnerFactory.Create();
            var rowsAffected = runner.Execute(new Command());
            rowsAffected.Should().Be(4);
        }
    }
}