using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Provides a mechanism to specify parameters in the command
    /// This is used with ISqlQuery and ISqlCommand, but will not be used with
    /// the "Raw" variants
    /// </summary>
    public interface ISqlParameterized
    {
        void SetupParameters(IDbCommand command, IDataParameterCollection parameters);
    }
}