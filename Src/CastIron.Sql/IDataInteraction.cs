using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    public interface IDataInteraction
    {
        IDbCommand Command { get; }
        IDataInteraction AddParameterWithValue(string name, object value);
        IDataInteraction AddParametersWithValues(IEnumerable<KeyValuePair<string, object>> parameters);
        // TODO: Method to pass an object, where each property is converted to a parameter
        IDataInteraction AddOutputParameter(string name, DbType dbType, int size);
        IDataInteraction AddInputOutputParameter(string name, object value, DbType dbType, int size);
        IDataInteraction ExecuteText(string sqlText);
        IDataInteraction CallStoredProc(string procedureName);
        bool IsValid { get; }
    }
}
