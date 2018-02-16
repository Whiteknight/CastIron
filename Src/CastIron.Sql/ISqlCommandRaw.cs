using System.Data.SqlClient;

namespace CastIron.Sql
{
    public interface ISqlCommandRaw
    {
        bool SetupCommand(SqlCommand command);
    }

    public interface ISqlCommandRaw<out T> : ISqlCommandRaw
    {
        T ReadOutputs(SqlQueryResult result);
    }
}