using System.Data;
using System.Data.SqlClient;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    // TODO: Need to fix parameter usage throughout for sqlite
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
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
                return true;
            }
        }

#if (CASTIRON_SQLITE == false)
        [Test]
        public void SqlCommandRawCommand_UntypedOutputParameter()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new CommandWithUntypedOutputParameter());
            result.Should().Be("TEST");
        }
#endif

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

#if (CASTIRON_SQLITE == false)
        [Test]
        public void SqlCommandRawCommand_TypedOutputParameter()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new CommandWithTypedOutputParameter());
            result.Should().Be("TEST");
        }
#endif

        public class CommandNotExecuted : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
                return false;
            }
        }

#if (CASTIRON_SQLITE == false)
        [Test]
        public void SqlCommandRaw_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }
#endif
    }
}