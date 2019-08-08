using System.Data;
using CastIron.Sql;

namespace CastIron.Postgres
{
    /// <summary>
    /// Factory to create an IDataInteraction for Postgres
    /// </summary>
    public class PostgresDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            return new PostgresDataInteraction(command);
        }
    }
}