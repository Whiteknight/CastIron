using System;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    /// <summary>
    /// An interaction with the database such as a command or a query. This object acts as a wrapper around
    /// IDbCommand to simplify some operations and abstract some differences between providers.
    /// </summary>
    public interface IDataInteraction
    {
        /// <summary>
        /// The raw underlying IDbCommand object, which can be modified directly
        /// </summary>
        IDbCommand Command { get; }

        /// <summary>
        /// Add a new input parameter with a value. The provider may mangle the name to add a '@' prefix or
        /// other decoration if required.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IDataInteraction AddParameterWithValue(string name, object value);

        /// <summary>
        /// Add several new input parameters with values from a dictionary or other key/value set. The 
        /// provider may mangle the name to add a '@' prefix or other decoration if required.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        IDataInteraction AddParametersWithValues(IEnumerable<KeyValuePair<string, object>> parameters);

        /// <summary>
        /// Add several new input parameters with values from the public properties of an object. The 
        /// provider may mangle the name to add a '@' prefix or other decoration if required.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        IDataInteraction AddParametersWithValues(object parameters);

        /// <summary>
        /// Add a named output parameter. Not supported by all providers.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dbType"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IDataInteraction AddOutputParameter(string name, DbType dbType, int size);

        /// <summary>
        /// Add a named input/output parameter with initial value. Not supported by all providers.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IDataInteraction AddInputOutputParameter(string name, object value, DbType dbType, int size);

        /// <summary>
        /// Execute a string of raw SQL text such as a query or command.
        /// </summary>
        /// <param name="sqlText"></param>
        /// <returns></returns>
        IDataInteraction ExecuteText(string sqlText);

        /// <summary>
        /// Call a stored procedure with the given name
        /// </summary>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        IDataInteraction CallStoredProc(string procedureName);

        /// <summary>
        /// Returns true if the interaction has been successfully setup, false otherwise
        /// </summary>
        bool IsValid { get; }
    }

    public static class DataInteractionExtensions
    {
        public static void SetTimeoutSeconds(this IDataInteraction interaction, int seconds)
        {
            Assert.ArgumentNotNull(interaction, nameof(interaction));
            interaction.Command.CommandTimeout = seconds;
        }

        public static void SetTimeout(this IDataInteraction interaction, TimeSpan timeSpan)
        {
            Assert.ArgumentNotNull(interaction, nameof(interaction));
            interaction.Command.CommandTimeout = (int)timeSpan.TotalSeconds;
        }
    }
}
