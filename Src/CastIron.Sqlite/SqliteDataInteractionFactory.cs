using System.Data;
using CastIron.Sql;
using CastIron.Sql.Utility;

namespace CastIron.Sqlite
{
    public class SqliteDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            Assert.ArgumentNotNull(command, nameof(command));
            return new SqliteDataInteraction(command);
        }
    }
}