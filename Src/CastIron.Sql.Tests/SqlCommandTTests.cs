using System.Data;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlCommandTTests
    {
        public class Command1 : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@param");
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText(GetSql());
                command.AddOutputParameter("@param", DbType.StringFixedLength, 4);

                return true;
            }

            public string GetSql()
            {
                return "SELECT @param = 'TEST';";
            }
        }

        [Test]
        public void Execute_T_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        public class Command2 : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@param");
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.StringFixedLength, 4);
                return false;
            }
        }

        [Test]
        public void Execute_T_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command2());
            result.Should().BeNullOrEmpty();
        }

        public class CommandWithUntypedOutputParameter : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameterValue("@param").ToString();
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
        public void Execute_UntypedOutputParameter([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandWithUntypedOutputParameter());
            result.Should().Be("TEST");
        }

        public class CommandWithTypedOutputParameter : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
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
        public void Execute_TypedOutputParameter([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandWithTypedOutputParameter());
            result.Should().Be("TEST");
        }

        public class CommandNotExecuted : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameterValue("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.AnsiString, 4);
                return false;
            }
        }

        [Test]
        public void Execute_NotExecuted([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Execute(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }

        public class CommandWithRowsAffected : ISqlCommandSimple<int>
        {
            public string GetSql()
            {
                return @"
                    DECLARE @temp TABLE (X INT NOT NULL);
                    INSERT INTO @temp(X) VALUES (1),(2),(3),(4);";
            }

            public int ReadOutputs(IDataResults result)
            {
                return result.RowsAffected;
            }
        }

        [Test]
        public void Execute_RowsAffected([Values("MSSQL")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var rowsAffected = runner.Execute(new CommandWithRowsAffected());
            rowsAffected.Should().Be(4);
        }
    }
}