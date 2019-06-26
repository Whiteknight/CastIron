using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class AffectedRowsTests
    { 

        public class Query : ISqlQuerySimple<int>
        {
            public string GetSql()
            {
                return @"
                    DECLARE @temp TABLE (X INT NOT NULL);
                    INSERT INTO @temp(X) VALUES (1),(2),(3),(4);";
            }

            public int Read(IDataResults result)
            {
                return result.RowsAffected;
            }
        }

        // TODO: Postgres uses TEMPORARY TABLE instead of table variables like this

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
                    DECLARE @temp TABLE (X INT NOT NULL);
                    INSERT INTO @temp(X) VALUES (1),(2),(3),(4);";
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