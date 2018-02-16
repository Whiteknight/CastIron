using System.Data.SqlClient;

namespace CastIron.Sql
{
    public interface ISqlQueryRaw<out T>
    {
        bool SetupCommand(SqlCommand command);
        T Read(SqlQueryResult result);
    }
}