using System.Data;

namespace CastIron.Sql
{
    public interface ISqlQueryRawConnection<out T> : ISqlQueryBase
    {
        T Query(IDbConnection connection);
    }
}