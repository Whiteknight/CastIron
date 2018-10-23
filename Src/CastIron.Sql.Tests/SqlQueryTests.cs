using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryTests
    {
        public class Query1 : ISqlQuerySimple<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQuery_Tests([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }

        [Test]
        public void SqlQuery_PerformanceReport([Values("MSSQL", "SQLITE")] string provider)
        {
            string report = null;
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query1(), b => b.MonitorPerformance(s => report = s));
            result.Should().Be("TEST");
            report.Should().NotBeNull();
        }

        public class Query2Values
        {
            public int Value1 { get; set; }
            public float Value2 { get; set; }
            public string Value3 { get; set; }
        }

        public class Query2 : ISqlQuery<Query2Values>
        {
            private readonly Query2Values _values;

            public Query2(Query2Values values)
            {
                _values = values;
            }

            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT @Value1 AS Value1, @Value2 AS Value2, @Value3 AS Value3;");
                interaction.AddParametersWithValues(_values);
                return interaction.IsValid;
            }

            public Query2Values Read(IDataResults result)
            {
                return result.AsEnumerable<Query2Values>().Single();
            }
        }

        [Test]
        public void ParameterObject_Test([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var parameters = new Query2Values
            {
                Value1 = 5,
                Value2 = 3.14f,
                Value3 = "TEST"

            };
            var result = runner.Query(new Query2(parameters));
            result.Value1.Should().Be(5);
            result.Value2.Should().Be(3.14f);
            result.Value3.Should().Be("TEST");
        }
    }
}
