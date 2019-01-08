using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    /// <summary>
    /// Basic tests for SELECT queries returning values. More depth in mapping logic
    /// is included in the Mapping tests.
    /// </summary>
    [TestFixture]
    public class SqlQueryTests
    {
        public class Query1 : ISqlQuerySimple<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQuery_Tests([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_PerformanceReport([Values("MSSQL", "SQLITE")] string provider)
        {
            string report = null;
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query1(), b => b.MonitorPerformance(s => report = s));
            result.Should().Be("TEST");
            report.Should().NotBeNull();
        }

        public class Query2Values
        {
            public int Value1 { get; set; }
            public float Value2 { get; set; }
            public string Value3 { get; set; }
        }

        public class Query2 : ISqlQuery<Query2Values>
        {
            private readonly Query2Values _values;

            public Query2(Query2Values values)
            {
                _values = values;
            }

            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT @Value1 AS Value1, @Value2 AS Value2, @Value3 AS Value3;");
                interaction.AddParametersWithValues(_values);
                return interaction.IsValid;
            }

            public Query2Values Read(IDataResults result)
            {
                return result.AsEnumerable<Query2Values>().Single();
            }
        }

        [Test]
        public void ParameterObject_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var parameters = new Query2Values
            {
                Value1 = 5,
                Value2 = 3.14f,
                Value3 = "TEST"

            };
            var result = runner.Query(new Query2(parameters));
            result.Value1.Should().Be(5);
            result.Value2.Should().Be(3.14f);
            result.Value3.Should().Be("TEST");
        }


        [Test]
        public void StreamingResults_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var stream = runner.QueryStream("SELECT 1 UNION SELECT 2 UNION SELECT 3");
            var result = stream.AsEnumerable<int>().ToList();
            result.Should().ContainInOrder(1, 2, 3);
            stream.Dispose();
        }

        [Test]
        public async Task QueryAsync_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = await runner.QueryAsync<string>("SELECT 'TEST'");
            result[0].Should().Be("TEST");
        }

        public class CommandExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return true;
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_Executed([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new CommandExecuted());
            result.Should().Be("TEST");
        }

        public class CommandNotExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return false;
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_NotExecuted([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }

        public class ParameterResult
        {
            public string Value { get; set; }
        }

        public class CommandWithParameter : ISqlQuery<ParameterResult>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @value = 'TEST';");
                command.AddOutputParameter("@value", DbType.AnsiString, 4);
                return true;
            }

            public ParameterResult Read(IDataResults result)
            {
                return result.GetOutputParameters<ParameterResult>();
            }
        }

        [Test]
        public void SqlQueryRawCommand_GetParameters()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new CommandWithParameter());
            result.Should().NotBeNull();
            result.Value.Should().Be("TEST");
        }
    }
}
