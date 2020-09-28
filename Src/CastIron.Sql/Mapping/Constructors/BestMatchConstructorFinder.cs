using CastIron.Sql.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Mapping.Constructors
{
    public class BestMatchConstructorFinder : IConstructorFinder
    {
        private static readonly BestMatchConstructorFinder _defaultInstance = new BestMatchConstructorFinder();

        public static BestMatchConstructorFinder GetDefaultInstance() => _defaultInstance;

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

            Argument.NotNull(columnNames, nameof(columnNames));

            // Otherwise score all constructors and find the best match
            var best = type.GetConstructors()
                .Select(c => new ScoredConstructor(provider.UnnamedColumnName, c, columnNames))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw ConstructorFindException.NoSuitableConstructorFound(type);
            return best.Constructor;
        }

        private class ScoredConstructor
        {
            private readonly string _unnamedColumnName;

            public ScoredConstructor(string unnamedColumnName, ConstructorInfo constructor, IReadOnlyDictionary<string, int> columnNames)
            {
                _unnamedColumnName = unnamedColumnName;
                Constructor = constructor;
                var parameters = constructor.GetParameters();
                Score = ScoreConstructorByParameterNames(columnNames, parameters);
            }

            public ConstructorInfo Constructor { get; }
            public int Score { get; }

            private int ScoreConstructorByParameterNames(IReadOnlyDictionary<string, int> columnNames, ParameterInfo[] parameters)
            {
                var score = 0;
                foreach (var param in parameters)
                {
                    var name = param.Name.ToLowerInvariant();
                    if (columnNames.ContainsKey(name))
                    {
                        score += 1;
                        continue;
                    }

                    if (_unnamedColumnName != null && param.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                    {
                        score += columnNames.ContainsKey(_unnamedColumnName) ? 1 : 0;
                        continue;
                    }
                    return -1;

                    // TODO: If the constructor param has the same name as a property, and that property has [Column("")] should we use that?

                    // TODO: check that the types are compatible. Add a big delta where the match is easy, smaller delta where the match requires conversion
                    // TODO: Return -1 where the types cannot be converted
                }

                return score;
            }
        }
    }
}