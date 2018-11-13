using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class ConstructorFinder
    {
        public ConstructorInfo FindBestMatch(ConstructorInfo preferredConstructor, Type type, IReadOnlyDictionary<string, int> columnNames)
        {
            // If the user has specified a preferred constructor, use that.
            if (preferredConstructor != null)
            {
                if (!preferredConstructor.IsPublic || preferredConstructor.IsStatic)
                    throw new Exception("Specified constructor is static or non-public and cannot be used");
                if (preferredConstructor.DeclaringType != type)
                    throw new Exception($"Specified constructor is for type {preferredConstructor.DeclaringType?.FullName} instead of the expected {type.FullName}");

                return preferredConstructor;
            }

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
                int score = 0;
                foreach (var param in parameters)
                {
                    var name = param.Name.ToLowerInvariant();
                    if (!columnNames.ContainsKey(name))
                        return -1;
                    if (!CompilerTypes.Primitive.Contains(param.ParameterType))
                        return -1;

                    // TODO: check that the types are compatible. Add a big delta where the match is easy, smaller delta where the match requires conversion
                    // TODO: Return -1 where the types cannot be converted
                    score++;
                }

                return score;
            }
        }
    }
}