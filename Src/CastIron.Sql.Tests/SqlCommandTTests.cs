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
                return result.GetOutputParameter("@param").ToString();
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
        public void SqlCommandT_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        public class Command2 : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.StringFixedLength, 4);
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