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
            {
                First = first;
                Second = second;
            }

            public int First { get; set; }
            public int Second { get; set; }
        }

        public class TabledValuesQuery : ISqlQuery<int[]>
        {
            private readonly TableValues[] _values;

            public TabledValuesQuery(TableValues[] values)
            {
                _values = values;
            }

            public int[] Read(IDataResults result)
                => result.AsEnumerable<int>().ToArray();

            public bool SetupCommand(IDataInteraction interaction)
            {
                //var table = new DataTable();
                //table.Columns.Add("First", typeof(int));
                //table.Columns.Add("Second", typeof(int));
                //var row = table.Row
                interaction
                    .AddTableValuedParameter("tableValuedParameter", "MyTestType", _values)
                    .ExecuteText("SELECT First + Second FROM @tableValuedParameter AS tvp");
                return true;
            }
        }

        [Test]
        public void Test()
        {
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            batch.Add("CREATE TYPE MyTestType AS TABLE ( First INT, Second INT );");
            var result = batch.Add(new TabledValuesQuery(new[] {
                new TableValues(10, 5),
                new TableValues(20, 7),
                new TableValues(30, 9)
            }));
            batch.Add("DROP TYPE MyTestType;");
            runner.Execute(batch);
            result.GetValue().Should().ContainInOrder(15, 27, 39);
        }
    }
}
