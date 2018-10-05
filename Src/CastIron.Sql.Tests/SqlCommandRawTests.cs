using System.Data;
using System.Data.SqlClient;
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

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = command.CreateParameter();
                p.ParameterName = "@param";
                p.DbType = DbType.AnsiString;
                p.Size = 4;
                p.Direction = ParameterDirection.Output;
                command.Parameters.Add(p);
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

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
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

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = command.CreateParameter();
                p.ParameterName = "@param";
                p.DbType = DbType.AnsiString;
                p.Size = 4;
                p.Direction = ParameterDirection.Output;
                command.Parameters.Add(p);
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