using System;
using System.Collections.Generic;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping.Constructors
{
    /// <summary>
    /// IConstructorFinder implementation which forces only the use of the default parameterless constructor
    /// </summary>
    public class DefaultOnlyConstructorFinder : IConstructorFinder
    {
        public ConstructorInfo FindBestMatch(IProviderConfiguration provider, ConstructorInfo preferredConstructor, Type type, IReadOnlyDictionary<string, int> columnNames)
        {
            Argument.NotNull(type, nameof(type));

            // If the user has specified a preferred constructor, use that.
            if (preferredConstructor != null)
            {
                if (!preferredConstructor.IsPublic || preferredConstructor.IsStatic)
                    throw ConstructorFindException.NonInvokableConstructorSpecified();
                if (preferredConstructor.DeclaringType != type)
                    throw ConstructorFindException.InvalidDeclaringType(preferredConstructor.DeclaringType, type);

                return preferredConstructor;
            }

            var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null && defaultConstructor.IsPublic)
                return defaultConstructor;

            throw ConstructorFindException.NoDefaultConstructor(type);
        }
    }
}