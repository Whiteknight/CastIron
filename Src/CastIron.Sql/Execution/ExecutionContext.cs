using System.Data;

namespace CastIron.Sql.Execution
{
    public class ExecutionContext : IExecutionContext
    {
        public ExecutionContext(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; }

        public IDbCommand CreateCommand()
        {
            var command = Connection.CreateCommand();
            if (Transaction != null)
                command.Transaction = Transaction;
            return command;
        }

        public void Dispose()
        {
            Connection?.Dispose();
            Transaction?.Commit();
            Transaction?.Dispose();
        }
    }
}