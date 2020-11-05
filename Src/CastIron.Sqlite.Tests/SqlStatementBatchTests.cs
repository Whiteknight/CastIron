using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    [TestFixture]
    public class SqlStatementBatchTests
    {
        public class CreateTempTableCommand : ISqlCommandSimple
        {
            public string GetSql()
            {
                return @"
                    CREATE TEMPORARY TABLE castiron_test (
                        [Value] INT NOT NULL
                    );
                    INSERT INTO castiron_test ([Value]) VALUES (1),(3),(5),(7);
";
            }
        }

        public class QueryTempTableQuery : ISqlQuerySimple<List<int>>
        {
            public string GetSql()
            {
                return "SELECT * FROM castiron_test;";
            }

            public List<int> Read(IDataResults result)
            {
                return result.AsEnumerable<int>().ToList();
            }
        }

        [Test]
        public void CommandAndQueryBatch_Test()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            batch.Add(new CreateTempTableCommand());
            var result = batch.Add(new QueryTempTableQuery());
            runner.Execute(batch);

            result.IsComplete.Should().Be(true);
            var list = result.GetValue();
            list.Should().NotBeNull();
            list.Should().BeEquivalentTo(1, 3, 5, 7);
        }

        [Test]
        public void CommandAndQueryBatch_Transaction()
        {
            var runner = RunnerFactory.Create(b => b.UseTransaction(IsolationLevel.Serializable));
            var batch = runner.CreateBatch();
            batch.Add(new CreateTempTableCommand());
            var result = batch.Add(new QueryTempTableQuery());
            runner.Execute(batch);

            result.IsComplete.Should().Be(true);
            var list = result.GetValue();
            list.Should().NotBeNull();
            list.Should().BeEquivalentTo(1, 3, 5, 7);
        }

        [Test]
        public void CommandAndQueryBatch_SimpleSql()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            batch.Add(@"
                CREATE TEMPORARY TABLE castiron_test (
                    [Value] INT NOT NULL
                );
                INSERT INTO castiron_test ([Value]) VALUES (1),(3),(5),(7);");
            var result = batch.Add<int>("SELECT * FROM castiron_test;");
            runner.Execute(batch);

            result.IsComplete.Should().Be(true);
            var list = result.GetValue();
            list.Should().NotBeNull();
            list.Should().BeEquivalentTo(1, 3, 5, 7);
        }

        public class QuerySimpleQuery : ISqlQuerySimple<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().Single();
            }
        }

        [Test]
        public async Task CommandAndQueryBatch_AsTask()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            var promise = batch.Add(new QuerySimpleQuery());
            var task = promise.AsTask(TimeSpan.FromSeconds(10));

            runner.Execute(batch);

            var result = await task;
            result.Should().Be("TEST");
        }

        [Test]
        public void Batch_SimpleSql()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            var result = batch.Add<string>("SELECT 'TEST';");
            runner.Execute(batch);

            var list = result.GetValue();
            list.Count.Should().Be(1);
            list[0].Should().Be("TEST");
        }
    }
}