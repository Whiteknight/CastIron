using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlStatementBatchTests
    {
        public class CreateTempTableCommand : ISqlCommand
        {
            public string GetSql()
            {
                return @"
CREATE TABLE #castiron_test (
    [Value] INT NOT NULL
);
INSERT INTO #castiron_test ([Value]) VALUES (1),(3),(5),(7);
";
            }
        }

        public class QueryTempTableQuery : ISqlQuery<List<int>>
        {
            public string GetSql()
            {
                return "SELECT * FROM #castiron_test;";
            }

            public List<int> Read(SqlResultSet result)
            {
                return result.AsEnumerable<int>().ToList();
            }
        }

        [Test]
        public void CommandAndQueryBatch_Test()
        {
            var runner = RunnerFactory.Create();
            var batch = new SqlStatementBatch();
            batch.Add(new CreateTempTableCommand());
            var result = batch.Add(new QueryTempTableQuery());
            runner.Execute(batch);

            result.IsComplete.Should().Be(true);
            var list = result.GetValue();
            list.Should().NotBeNull();
            list.Should().BeEquivalentTo(1, 3, 5, 7);
        }

        public class CreateTempStoredProcCommand : ISqlCommand
        {
            public string GetSql()
            {
                return @"
-- Create local temp procedure
CREATE PROCEDURE #TestProcedure
AS
    SELECT 'TEST';
";
            }
        }

        public class ExecuteStoredProcQuery : ISqlQuery<string>, ISqlStoredProc
        {
            public string GetSql()
            {
                return "#TestProcedure";
            }

            public string Read(SqlResultSet result)
            {
                return result.AsEnumerable<string>().Single();
            }
        }

        [Test]
        public void CreateAndQueryStoredProc_Test()
        {
            var runner = RunnerFactory.Create();
            var batch = new SqlStatementBatch();
            batch.Add(new CreateTempStoredProcCommand());
            var result = batch.Add(new ExecuteStoredProcQuery());
            runner.Execute(batch);

            result.IsComplete.Should().BeTrue();
            result.GetValue().Should().Be("TEST");
        }
    }
}