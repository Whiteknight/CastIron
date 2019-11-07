using CastIron.Sql.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds information about the overall map compilation progress. During compilation, MapState objects
    /// are used at each level to compile individual expressions
    /// </summary>
    public class MapCompileContext
    {
        private readonly VariableNumberSource _variableNumbers;

        public MapCompileContext(IProviderConfiguration provider, Type specific, ObjectCreatePreferences create, string separator)
        {
            Argument.NotNull(provider, nameof(provider));
            Argument.NotNull(specific, nameof(specific));
            Argument.NotNull(create, nameof(create));

            Specific = specific;
            CreatePreferences = create;
            Separator = separator ?? "_";

            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");

            Provider = provider;

            _variableNumbers = new VariableNumberSource();
        }

        public string Separator { get; }
        public IProviderConfiguration Provider { get; }
        public ParameterExpression RecordParam { get; }
        public Type Specific { get; }
        public ObjectCreatePreferences CreatePreferences { get; }

        public ConstructorInfo GetConstructor(IReadOnlyDictionary<string, int> columnNameCounts, Type type)
            => CreatePreferences.GetConstructor(Provider, columnNameCounts, type);

        public ConstructorInfo GetPreferredConstructor(Type type)
            => CreatePreferences.GetPreferredConstructor(type);

        public Func<object> GetFactoryMethod(Type type) => CreatePreferences.GetFactory(type);

        public ParameterExpression CreateVariable<T>(string name) => CreateVariable(typeof(T), name);

        public ParameterExpression CreateVariable(Type t, string name)
        {
            name = name + "_" + _variableNumbers.GetNext();
            var variable = Expression.Variable(t, name);
            return variable;
        }

        public class VariableNumberSource
        {
            private int _varNumber;

            public VariableNumberSource()
            {
                _varNumber = 0;
            }

            public int GetNext()
            {
                return _varNumber++;
            }
        }
    }
}