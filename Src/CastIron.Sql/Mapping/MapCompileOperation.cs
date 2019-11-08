using CastIron.Sql.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds information about the overall map compilation progress. During compilation, MapContext objects
    /// are used at each level to compile individual expressions
    /// </summary>
    public class MapCompileOperation
    {
        private readonly VariableNumberSource _variableNumbers;

        public MapCompileOperation(IProviderConfiguration provider, Type topLevelTargetType, ObjectCreatePreferences create, string separator, ICollection<string> ignorePrefixes)
        {
            Argument.NotNull(provider, nameof(provider));
            Argument.NotNull(topLevelTargetType, nameof(topLevelTargetType));
            Argument.NotNull(create, nameof(create));

            TopLevelTargetType = topLevelTargetType;
            CreatePreferences = create;
            IgnorePrefixes = ignorePrefixes ?? new List<string>();
            Separator = separator ?? "_";

            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");

            Provider = provider;

            _variableNumbers = new VariableNumberSource();
        }

        public string Separator { get; }
        public IProviderConfiguration Provider { get; }
        public ParameterExpression RecordParam { get; }
        public Type TopLevelTargetType { get; }
        public ObjectCreatePreferences CreatePreferences { get; }
        public ICollection<string> IgnorePrefixes { get; }

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

        public MapContext CreateContextFromDataReader(IDataReader reader)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                // TODO: Specify list of prefixes to ignore. If the column starts with a prefix, remove those chars from the front
                // e.g. "Table_ID" with prefix "Table_" becomes "ID"
                var name = (reader.GetName(i) ?? "");
                var prefix = IgnorePrefixes.FirstOrDefault(p => name.StartsWith(p));
                if (prefix != null)
                    name = name.Substring(prefix.Length);
                var typeName = reader.GetDataTypeName(i);
                var columnType = reader.GetFieldType(i);
                var info = new ColumnInfo(i, name, columnType, typeName);
                if (!columnNames.ContainsKey(info.CanonicalName))
                    columnNames.Add(info.CanonicalName, new List<ColumnInfo>());
                columnNames[info.CanonicalName].Add(info);
            }

            return new MapContext(this, columnNames, TopLevelTargetType, null, null);
        }

        public class VariableNumberSource
        {
            private int _varNumber;

            public VariableNumberSource()
            {
                _varNumber = 0;
            }

            public int GetNext() => _varNumber++;
        }
    }
}