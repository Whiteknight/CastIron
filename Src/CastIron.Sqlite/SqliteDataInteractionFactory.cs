using System.Data;
using CastIron.Sql;

namespace CastIron.Sqlite
{
    public class SqliteDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            CIAssert.ArgumentNotNull(command, nameof(command));
            return new SqliteDataInteraction(command);
        }
    }
}