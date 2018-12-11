using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class ConstructorFinder : IConstructorFinder
    {
        public ConstructorInfo FindBestMatch(ConstructorInfo preferredConstructor, Type type, IReadOnlyDictionary<string, int> columnNames)
        {
            Assert.ArgumentNotNull(type, nameof(type));

            // If the user has specified a preferred constructor, use that.
            if (preferredConstructor != null)
            {
                if (!preferredConstructor.IsPublic || preferredConstructor.IsStatic)
                    throw new Exception("Specified constructor is static or non-public and cannot be used");
                if (preferredConstructor.DeclaringType != type)
                    throw new Exception($"Specified constructor is for type {preferredConstructor.DeclaringType?.FullName} instead of the expected {type.FullName}");

                return preferredConstructor;
            }

            Assert.ArgumentNotNull(columnNames, nameof(columnNames));

            // Otherwise score all constructors and find the best match
            var best = type.GetConstructors()
                .Select(c => new ScoredConstructor(c, columnNames))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw new Exception($"Cannot find a suitable constructor for type {type.FullName}");
            return best.Constructor;
        }

        private class ScoredConstructor
        {
            public ScoredConstructor(ConstructorInfo constructor, IReadOnlyDictionary<string, int> columnNames)
            {
                Constructor = constructor;
                var parameters = constructor.GetParameters();
                Score = ScoreConstructorByParameterNames(columnNames, parameters);
            }

            public ConstructorInfo Constructor { get; }
            public int Score { get; }

            private static int ScoreConstructorByParameterNames(IReadOnlyDictionary<string, int> columnNames, ParameterInfo[] parameters)
            {
                var score = 0;
                foreach (var param in parameters)
                {
                    if (DataRecordExpressions.IsSupportedPrimitiveType(param.ParameterType))
                    {
                        var name = param.Name.ToLowerInvariant();
                        if (columnNames.ContainsKey(name))
                        {
                            score += 1;
                            continue;
                        }

                        if (param.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                        {
                            score += columnNames.ContainsKey("") ? 1 : 0;
                            continue;
                        }
                        return -1;
                    }

                    if (DataRecordExpressions.IsSupportedCollectionType(param.ParameterType))
                    {
                        var name = param.Name.ToLowerInvariant();
                        if (columnNames.ContainsKey(name))
                        {
                            score += columnNames[name];
                            continue;
                        }

                        if (param.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                        {
                            score += columnNames.ContainsKey("") ? columnNames[""] : 0;
                            continue;
                        }
                        return -1;
                    }
                    

                    if (param.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                    {
                        score++;
                        continue;
                    }

                    // TODO: If the constructor param has the same name as a property, and that property has [Column("")] should we use that?

                    // TODO: check that the types are compatible. Add a big delta where the match is easy, smaller delta where the match requires conversion
                    // TODO: Return -1 where the types cannot be converted
                    return -1;
                }

                return score;
            }
        }
    }
}