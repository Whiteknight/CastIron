using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class ObjectCreatePreferences
    {
        public Func<object> Factory { get; }
        public ConstructorInfo PreferredConstructor { get; }
        public IConstructorFinder ConstructorFinder { get; }

        public ObjectCreatePreferences(Func<object> factory, ConstructorInfo preferredConstructor, IConstructorFinder constructorFinder)
        {
            Factory = factory;
            PreferredConstructor = preferredConstructor;
            ConstructorFinder = constructorFinder;
        }
    }

    public class MapCompileContext
    {
        public IProviderConfiguration Provider { get; }

        private readonly VariableNumberSource _variableNumbers;
        private readonly string _separator;

        // TODO: Reduce the size of this parameter list
        public MapCompileContext(IProviderConfiguration provider, Type specific, ObjectCreatePreferences create, ColumnInfoCollection columns, string separator = "_")
        {
            Argument.NotNull(provider, nameof(provider));
            Argument.NotNull(specific, nameof(specific));
            Argument.NotNull(columns, nameof(columns));
            Argument.NotNull(create, nameof(create));

            _separator = (separator ?? "_").ToLowerInvariant();
            Specific = specific;
            CreatePreferences = create;

            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");
            
            Provider = provider;
            Columns = columns;

            _variableNumbers = new VariableNumberSource();
        }

        private MapCompileContext(MapCompileContext parent, Type type, ColumnInfoCollection columns, string separator)
        {
            Argument.NotNull(parent, nameof(parent));
            Argument.NotNullOrEmpty(separator, nameof(_separator));
            Argument.NotNull(columns, nameof(columns));

            _separator = separator;
            Specific = type;
            Provider = parent.Provider;

            RecordParam = parent.RecordParam;

            CreatePreferences = parent.CreatePreferences;

            _variableNumbers = parent._variableNumbers;
            Columns = columns;
        }

        public ColumnInfoCollection Columns { get; }
        public ParameterExpression RecordParam { get; }
        public Type Specific { get; }
        public ObjectCreatePreferences CreatePreferences { get; }

        public ConstructorInfo GetConstructor(Type type)
        {
            var finder = CreatePreferences.ConstructorFinder ?? ConstructorFinder.GetDefaultInstance();
            return finder.FindBestMatch(Provider, CreatePreferences.PreferredConstructor, type, GetColumnNameCounts());
        }

        public ParameterExpression CreateVariable<T>(string name) => CreateVariable(typeof(T), name);

        public ParameterExpression CreateVariable(Type t, string name)
        {
            name = name + "_" + _variableNumbers.GetNext();
            var variable = Expression.Variable(t, name);
            return variable;
        }

        public IReadOnlyDictionary<string, int> GetColumnNameCounts() => Columns.GetColumnNameCounts();

        


        public MapState GetState(string name, Type targetType)
        {
            return new MapState(this, Columns.ForName(name), targetType, name, null);
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