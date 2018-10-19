using System.Data;
using CastIron.Sql.Utility;

namespace CastIron.Sql.SqlServer
{
    public class SqlServerDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            Assert.ArgumentNotNull(command, nameof(command));
            return new SqlServerDataInteraction(command);
        }
    }
}