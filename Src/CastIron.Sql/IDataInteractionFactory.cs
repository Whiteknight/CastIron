using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Factory type to create IDataInteraction instances per-provider
    /// </summary>
    public interface IDataInteractionFactory
    {
        /// <summary>
        /// Create a new IDataInteraction to wrap the given IDbCommand
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        IDataInteraction Create(IDbCommand command);
    }
}