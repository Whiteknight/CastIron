using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQuerySetupCommandTests
    {

        public class CommandExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return true;
            }

            public string Read(IDataResults result)
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

            public string Read(IDataResults result)
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
    }
}