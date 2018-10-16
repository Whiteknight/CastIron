using System.Data;
using System.Data.SqlClient;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlCommandTTests
    {
        public class Command1 : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText(GetSql());

                var p1 = command.Command.CreateParameter();
                p1.ParameterName = "@param";
                p1.DbType = DbType.StringFixedLength;
                p1.Size = 4;
                p1.Direction = ParameterDirection.Output;
                command.Command.Parameters.Add(p1);

                return true;
            }

            public string GetSql()
            {
                return "SELECT @param = 'TEST';";
            }
        }

        [Test]
        public void SqlCommandT_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        public class Command2 : ISqlCommand<string>
        {
            public string ReadOutputs(SqlResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Command.Parameters.Add(p);
                return false;
            }
        }

        [Test]
        public void SqlCommandT_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command2());
            result.Should().BeNullOrEmpty();
        }
    }
}