using System.Data;

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