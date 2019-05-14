using System.Data;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class CommandsWithParametersTests
    {

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