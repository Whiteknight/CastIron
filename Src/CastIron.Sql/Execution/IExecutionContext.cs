using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public interface IExecutionContext : IDisposable
    {
        IDbConnection Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommand CreateCommand();
    }
}