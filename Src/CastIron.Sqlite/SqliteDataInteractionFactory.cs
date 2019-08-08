using System.Data;
using CastIron.Sql;
using CastIron.Sql.Utility;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Sqlite IDataInteraction factory type.
    /// </summary>
    public class SqliteDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            Argument.NotNull(command, nameof(command));
            return new SqliteDataInteraction(command);
        }
    }
}