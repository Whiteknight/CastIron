using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// A factory for IDbConnection objects. Implementations of this class allow CastIron to target different
    /// SQL providers such as MS SQL Server, MySQL, MariaDB or Oracle SQL if they implement the necessary
    /// abstractions.
    /// </summary>
    public interface IDbConnectionFactory
    {
        IDbConnection Create(string connectionString);
    }
}