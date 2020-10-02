using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds information about the overall map compilation progress. During compilation, 
    /// MapTypeContext flyweight objects are used at each level to compile individual expressions.
    /// </summary>
    public class MapContext
    {
        private readonly VariableNumberSource _variableNumbers;

        public MapContext(IProviderConfiguration provider, Type topLevelTargetType, CompilationSettings typeSettings)
        {
            Argument.NotNull(provider, nameof(provider));
            Argument.NotNull(topLevelTargetType, nameof(topLevelTargetType));
            Argument.NotNull(typeSettings, nameof(typeSettings));

            TopLevelTargetType = topLevelTargetType;
            Settings = typeSettings;

            IgnorePrefixes = typeSettings.IgnorePrefixes ?? new List<string>();
            Separator = (typeSettings.Separator ?? "_").ToLowerInvariant();

            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");

            Provider = provider;

            _variableNumbers = new VariableNumberSource();
        }

        public CompilationSettings Settings { get; }
        public string Separator { get; }
        public IProviderConfiguration Provider { get; }
        public ParameterExpression RecordParam { get; }
        public Type TopLevelTargetType { get; }
        public ICollection<string> IgnorePrefixes { get; }

        public ParameterExpression CreateVariable(Type t, string name)
        {
            name = name + "_" + _variableNumbers.GetNext();
            var variable = Expression.Variable(t, name);
            return variable;
        }

        public LabelExpression CreateLabel(string name)
        {
            name = name + "_" + _variableNumbers.GetNext();
            var target = Expression.Label(name);
            var label = Expression.Label(target);
            return label;
        }

        public MapTypeContext CreateContextFromDataReader(IDataReader reader)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = (reader.GetName(i) ?? "");
                var prefix = IgnorePrefixes
                    .Where(p => name.StartsWith(p))
                    .OrderByDescending(s => s.Length)
                    .FirstOrDefault();
                if (prefix != null)
                    name = name.Substring(prefix.Length);
                var typeName = reader.GetDataTypeName(i);
                var columnType = reader.GetFieldType(i);
                var info = new ColumnInfo(i, name, columnType, typeName);
                if (!columnNames.ContainsKey(info.CanonicalName))
                    columnNames.Add(info.CanonicalName, new List<ColumnInfo>());
                columnNames[info.CanonicalName].Add(info);
            }

            return new MapTypeContext(this, columnNames, TopLevelTargetType, null, "", null);
        }

        private class VariableNumberSource
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