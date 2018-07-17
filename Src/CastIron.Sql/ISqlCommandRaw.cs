using System.Data;

namespace CastIron.Sql
{
    public interface ISqlCommandRaw
    {
        bool SetupCommand(IDbCommand command);
    }

    // TODO: Should this implement ISqlParameterized
    public interface ISqlCommandRaw<out T> : ISqlCommandRaw
    {
        T ReadOutputs(SqlQueryRawResultSet result);
    }
}