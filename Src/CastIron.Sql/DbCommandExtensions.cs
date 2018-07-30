using System.Data;

namespace CastIron.Sql
{
    public static class DbCommandExtensions
    {
        /// <summary>
        /// Implementation-independent way to add a simple input parameter with a name and value
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void AddParameterWithValue(this IDbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
