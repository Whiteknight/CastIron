using System.Data;

namespace CastIron.Sql
{
    public interface ISqlQueryRaw<out T>
    {
        bool SetupCommand(IDbCommand command);
        T Read(SqlQueryResult result);
    }
}