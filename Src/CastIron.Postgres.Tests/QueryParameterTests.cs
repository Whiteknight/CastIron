using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests
{
    [TestFixture]
    public class QueryParameterTests
    {
        public class QueryValues
        {
            public int Value1 { get; set; }
            public float Value2 { get; set; }
            public string Value3 { get; set; }
        }

        public class Query : ISqlQuery<QueryValues>
        {
            private readonly QueryValues _values;

            public Query(QueryValues values)
            {
                _values = values;
            }

            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText("SELECT @Value1 AS Value1, @Value2 AS Value2, @Value3 AS Value3;");
                interaction.AddParametersWithValues(_values);
                return interaction.IsValid;
            }

            public QueryValues Read(IDataResults result)
            {
                return result.AsEnumerable<QueryValues>().Single();
            }
        }

        [Test]
        public void ParameterObject_Test()
        {
            var runner = RunnerFactory.Create();
            var parameters = new QueryValues
            {
                Value1 = 5,
                Value2 = 3.14f,
                Value3 = "TEST"

            };
            var result = runner.Query(new Query(parameters));
            result.Value1.Should().Be(5);
            result.Value2.Should().Be(3.14f);
            result.Value3.Should().Be("TEST");
        }

        [Test]
        public void Query2_Stringify()
        {
            var runner = RunnerFactory.Create();
            var parameters = new QueryValues
            {
                Value1 = 5,
                Value2 = 3.14f,
                Value3 = "TEST"

            };
            var query = new Query(parameters);
            var result = runner.Stringifier.Stringify(query);
            result.Should().NotBeNullOrEmpty();
        }
    }
}