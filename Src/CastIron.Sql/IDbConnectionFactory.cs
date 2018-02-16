using System.Data;

namespace CastIron.Sql
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}