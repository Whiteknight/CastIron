using System.Data;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryRawCommandTests
    {
        public class CommandExecuted : ISqlQueryRawCommand<string>
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT 'TEST';";
                return true;
            }

            public string Read(SqlResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_Executed()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new CommandExecuted());
            result.Should().Be("TEST");
        }

        public class CommandNotExecuted : ISqlQueryRawCommand<string>
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT 'TEST';";
                return false;
            }

            public string Read(SqlResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }

        public class ParameterResult
        {
            public string Value { get; set; }
        }

        public class CommandWithParameter : ISqlQueryRawCommand<ParameterResult>
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @value = 'TEST';";
                var param = command.CreateParameter();
                param.ParameterName = "@value";
                param.Direction = ParameterDirection.Output;
                param.DbType = DbType.AnsiString;
                param.Size = 4;
                command.Parameters.Add(param);
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