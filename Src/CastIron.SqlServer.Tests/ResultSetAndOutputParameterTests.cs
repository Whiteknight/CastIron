using System;
using System.Data;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
{
    [TestFixture]
    public class ResultSetAndOutputParameterTests
    {
        public class Result
        {
            public string ResultSetValue { get; set; }
            public string OutputParameterValue { get; set; }
        }

        public class QueryWithResultSetAndParameterQuery : ISqlQuery
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText(@"
                    SELECT @value = 'param';
                    SELECT 'result';");
                command.AddOutputParameter("@value", DbType.AnsiString, 5);
                return true;
            }
        }

        [Test]
        public void GetResultEnumerable_Parameter_Map()
        {
            Result Read(IDataResults result)
            {
                var results = result.AsEnumerable<string>();
                var paramValue = result.GetOutputParameter<string>("value");
                var firstResult = results.First();
                return new Result
                {
                    OutputParameterValue = paramValue,
                    ResultSetValue = firstResult
                };
            }

            var runner = RunnerFactory.Create();
            Action getResults = () => runner.Query(new QueryWithResultSetAndParameterQuery(), Read);
            getResults.Should().Throw<SqlQueryException>();
        }

        public class EnumerateMapParameterMaterializer : IResultMaterializer<Result>
        {
            public Result Read(IDataResults result)
            {
                var results = result.AsEnumerable<string>();
                var firstResult = results.First();
                var paramValue = result.GetOutputParameter<string>("value");
                return new Result
                {
                    OutputParameterValue = paramValue,
                    ResultSetValue = firstResult
                };
            }
        }

        [Test]
        public void GetResultEnumerable_Map_Parameter()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new QueryWithResultSetAndParameterQuery(), new EnumerateMapParameterMaterializer());
            result.Should().NotBeNull();
            result.OutputParameterValue.Should().Be("param");
            result.ResultSetValue.Should().Be("result");
        }
    }
}