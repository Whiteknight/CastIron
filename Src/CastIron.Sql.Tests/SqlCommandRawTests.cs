using System.Data;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlCommandRawTests
    {
        public class CommandWithUntypedOutputParameter : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.AnsiString, 4);
                return true;
            }
        }

        // Sqlite doesn't support output parameters?
        [Test]
        public void SqlCommandRawCommand_UntypedOutputParameter([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandWithUntypedOutputParameter());
            result.Should().Be("TEST");
        }

        public class CommandWithTypedOutputParameter : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter<string>("@param");
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.AnsiString, 4);
                return true;
            }
        }

        // Sqlite doesn't support output parameters?
        [Test]
        public void SqlCommandRawCommand_TypedOutputParameter([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandWithTypedOutputParameter());
            result.Should().Be("TEST");
        }

        public class CommandNotExecuted : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.AnsiString, 4);
                return false;
            }
        }

        [Test]
        public void SqlCommandRaw_NotExecuted([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }
    }
}