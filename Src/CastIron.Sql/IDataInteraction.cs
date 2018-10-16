using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    public interface IDataInteraction
    {
        IDbCommand Command { get; }
        IDataInteraction AddParameterWithValue(string name, object value);
        IDataInteraction AddParametersWithValues(IEnumerable<KeyValuePair<string, object>> parameters);
        IDataInteraction ExecuteText(string sqlText);
        IDataInteraction CallStoredProc(string procedureName);
        bool IsValid { get; }
    }
}
