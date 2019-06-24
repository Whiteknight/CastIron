using System.Data;
using CastIron.Sql;

namespace CastIron.Postgres
{
    public class PostgresDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            return new PostgresDataInteraction(command);
        }
    }
}