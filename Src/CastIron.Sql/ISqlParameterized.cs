using System.Collections.Generic;

namespace CastIron.Sql
{
    public interface ISqlParameterized
    {
        IEnumerable<Parameter> GetParameters();
    }
}