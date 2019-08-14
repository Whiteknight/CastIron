using System.Data;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public static partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Wraps an existing IDataReader in an IDataResultsStream for object mapping and other capabilities.
        /// The IDataReader (and IDbCommand and IDbConnection, if any) will need to be managed and disposed
        /// manually.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="reader"></param>
        /// <param name="command">If provided, gives access to mapped output parameters</param>
        /// <returns></returns>
        public static IDataResultsStream WrapAsResultStream(this ISqlRunner runner, IDataReader reader, IDbCommand command = null)
        {
            return new DataReaderResultsStream(runner.Provider, command, null, reader);
        }

        /// <summary>
        /// Wraps an existing DataTable in an IDataResultsStream for object mapping and other capabilities.
        /// Any objects used to produce the data table, such as IDbCommand and IDbConnection must be 
        /// managed and disposed manually.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IDataResultsStream WrapAsResultStream(this ISqlRunner runner, DataTable table)
        {
            Argument.NotNull(table, nameof(table));
            return new DataReaderResultsStream(runner.Provider, null, null, table.CreateDataReader());
        }
    }
}
