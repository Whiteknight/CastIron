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
        private readonly IProviderConfiguration _provider;
        
        private readonly List<ParameterExpression> _variables;
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
            
            _provider = provider;
            Columns = columns;

            _variableNumbers = new VariableNumberSource();
            _variables = new List<ParameterExpression>();
        }

        private MapCompileContext(MapCompileContext parent, Type type, ColumnInfoCollection columns, string separator)
        {
            Argument.NotNull(parent, nameof(parent));
            Argument.NotNullOrEmpty(separator, nameof(_separator));
            Argument.NotNull(columns, nameof(columns));

            _separator = separator;
            Specific = type;
            _provider = parent._provider;

            RecordParam = parent.RecordParam;

            CreatePreferences = parent.CreatePreferences;

            _variableNumbers = parent._variableNumbers;
            _variables = parent._variables;
            Columns = columns;
        }

        public ColumnInfoCollection Columns { get; }
        public ParameterExpression RecordParam { get; }
        public Type Specific { get; }
        public ObjectCreatePreferences CreatePreferences { get; }
        public IReadOnlyList<ParameterExpression> Variables => _variables;

        public MapCompileContext CreateSubcontext(Type t, string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                prefix += _separator;
            var columns = Columns.ForPrefix(prefix);
            return new MapCompileContext(this, t, columns, _separator);
        }

        public ConstructorInfo GetConstructor()
        {
            var finder = CreatePreferences.ConstructorFinder ?? ConstructorFinder.GetDefaultInstance();
            return finder.FindBestMatch(_provider, CreatePreferences.PreferredConstructor, Specific, GetColumnNameCounts());
        }

        public ParameterExpression AddVariable<T>(string name) => AddVariable(typeof(T), name);

        public ParameterExpression AddVariable(Type t, string name)
        {
            name = name + "_" + _variableNumbers.GetNext();
            var variable = Expression.Variable(t, name);
            _variables.Add(variable);
            return variable;
        }

        public IReadOnlyDictionary<string, int> GetColumnNameCounts() => Columns.GetColumnNameCounts();

        public IEnumerable<ColumnInfo> GetColumnsForProperty(string name, ICustomAttributeProvider attrs)
        {
            if (name == null)
                return Columns.GetColumns(null);

            var propertyName = name.ToLowerInvariant();
            if (Columns.HasColumn(propertyName))
                return Columns.GetColumns(propertyName);

            if (attrs == null)
                return Enumerable.Empty<ColumnInfo>();

            var columnName = attrs.GetTypedAttributes<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (Columns.HasColumn(columnName))
                return Columns.GetColumns(columnName);

            var acceptsUnnamed = attrs.GetTypedAttributes<UnnamedColumnsAttribute>().Any();
            if (acceptsUnnamed && _provider.UnnamedColumnName != null)
                return Columns.GetColumns(_provider.UnnamedColumnName);
            return Enumerable.Empty<ColumnInfo>();
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