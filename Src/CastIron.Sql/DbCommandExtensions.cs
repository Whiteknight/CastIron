using System.Data;

namespace CastIron.Sql
{
    public static class DbCommandExtensions
    {
        public static void AddParameterWithValue(this IDbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
