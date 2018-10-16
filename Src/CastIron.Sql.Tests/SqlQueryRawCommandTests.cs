using System.Data;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryRawCommandTests
    {
        public class CommandExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return true;
            }

            public string Read(SqlResultSet result)
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

            public string Read(SqlResultSet result)
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

            public ParameterResult Read(SqlResultSet result)
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