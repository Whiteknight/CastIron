using System;
using System.Collections.Generic;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Search algorithm to find the "best" constructor to use for creating an object instance from a record set
    /// </summary>
    public interface IConstructorFinder
    {
        /// <summary>
        /// Find the best constructor, if any, which can be used for a result set with the given column names
        /// and counts. If a suitable constructor cannot be found an exception will be thrown (return value
        /// is expected to be non-null)
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="preferredConstructor">A preferred constructor to use. The implementation may throw
        /// an exception or return a better constructor if the supplied one does not meet requirements,
        /// depending on the implementation. preferredConstructor.DeclaringType must be equal to the provided
        /// type</param>
        /// <param name="type">The type of object to create.</param>
        /// <param name="columnNames">A dictionary of column names->count. Unnamed columns should be
        /// represented by the empty string</param>
        /// <returns></returns>
        ConstructorInfo FindBestMatch(IProviderConfiguration provider, ConstructorInfo preferredConstructor, Type type, IReadOnlyDictionary<string, int> columnNames);
    }
}