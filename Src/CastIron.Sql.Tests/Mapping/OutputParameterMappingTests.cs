using System.Data;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class OutputParameterMappingTests
    {
        public class TestCommand_OutputParameter : ISqlCommand<string>
        {
            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT @value = 'TEST';");
                interaction.AddOutputParameter("@value", DbType.AnsiString, 8000);
                return true;
            }

            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@value");
            }
        }

        [Test]
        public void GetOutputParameter_Test([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Execute(new TestCommand_OutputParameter());
            result.Should().Be("TEST");
        }

        public class TestCommand_OutputParameterConverted : ISqlCommand<string>
        {
            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT @value = 5;");
                interaction.AddOutputParameter("@value", DbType.Int32, 0);
                return true;
            }

            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@value");
            }
        }

        [Test]
        public void GetOutputParameter_ConvertType([Values("MSSQL")] string provider)
        {
            var target = RunnerFactory.Create(provider);
            var result = target.Execute(new TestCommand_OutputParameterConverted());
            result.Should().Be("5");
        }
    }
}