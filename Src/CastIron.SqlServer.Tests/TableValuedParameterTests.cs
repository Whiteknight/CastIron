using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
{
    public class TableValuedParameterTests
    {
        public class TableValues
        {
            public TableValues(int first, int second)
                => (First, Second) = (first, second);

            public int First { get; set; }
            public int Second { get; set; }
        }

        [Test]
        public void TableValuedParameterFromObjectArray()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            batch.Add("CREATE TYPE MyTestType AS TABLE ( First INT, Second INT );");
            var result = batch.Add(interaction => interaction
                .AddTableValuedParameter("tableValuedParameter", "MyTestType", new[] {
                    new TableValues(10, 5),
                    new TableValues(20, 7),
                    new TableValues(30, 9)
                })
                .ExecuteText("SELECT First + Second FROM @tableValuedParameter AS tvp")
                .IsValid,
                result => result.AsEnumerable<int>().ToArray()
            );
            batch.Add("DROP TYPE MyTestType;");
            runner.Execute(batch);
            result.GetValue().Should().ContainInOrder(15, 27, 39);
        }
    }
}
