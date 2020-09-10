using CastIron.Sql;
using Newtonsoft.Json;
using System.Linq;

namespace CastIron.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            var runner = RunnerFactory.Create("server=localhost;Integrated Security=SSPI;");
            var result = runner.Query(new TestQuery());
            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            System.Console.WriteLine(json);
        }
    }

    public class TestObject
    {
        public int TestInt { get; set; }
        public string TestString { get; set; }
        public bool TestBool { get; set; }
    }

    public class TestQuery : ISqlQuerySimple<TestObject>
    {
        public string GetSql()
        {
            return "SELECT 1 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;";
        }

        public TestObject Read(IDataResults result)
        {
            return result.AsEnumerable<TestObject>().FirstOrDefault();
        }
    }
}
