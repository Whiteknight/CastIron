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
        /// <summary>
        /// Add parameters to the sql command. Notice that ISqlCommand.SetupCommand or
        /// ISqlQuery.SetupCommand methods may also be used to setup parameters
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        void SetupParameters(IDbCommand command, IDataParameterCollection parameters);
    }
}