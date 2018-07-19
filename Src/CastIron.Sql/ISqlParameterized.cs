using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Provides a mechanism to specify parameters in the command
    /// </summary>
    public interface ISqlParameterized
    {
        void SetupParameters(IDataParameterCollection parameters);
    }
}