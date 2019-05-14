using System.Data;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class CommandOutputParametersTests
    {
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
    }
}