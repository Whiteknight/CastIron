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

        [Test]
        public void Query_RowsAffected([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var rowsAffected = runner.Query(new Query());
            rowsAffected.Should().Be(4);
        }
    }
}