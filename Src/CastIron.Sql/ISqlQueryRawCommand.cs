using System.Data;

namespace CastIron.Sql
{
    public interface ISqlQueryRawCommand<out T> : ISqlQueryBase
    {
        bool SetupCommand(IDbCommand command);
        T Read(SqlResultSet result);
    }
}