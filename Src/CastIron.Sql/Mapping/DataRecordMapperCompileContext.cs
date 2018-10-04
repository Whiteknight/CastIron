using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMapperCompileContext
    {
        public DataRecordMapperCompileContext(ParameterExpression recordParam, ParameterExpression instance)
        {
            Assert.ArgumentNotNull(recordParam, nameof(recordParam));

            RecordParam = recordParam;
            Instance = instance;

            MappedColumns = new HashSet<string>();
            Variables = new List<ParameterExpression>();
            if (instance != null)
                Variables.Add(instance);
            Statements = new List<Expression>();
        }

        public IReadOnlyDictionary<string, int> ColumnNames { get; private set; }
        public IReadOnlyDictionary<string, Type> ColumnTypes { get; private set; }
        public ParameterExpression RecordParam { get; }
        public ParameterExpression Instance { get; }

        public HashSet<string> MappedColumns { get; }

        public List<ParameterExpression> Variables { get; }
        public List<Expression> Statements { get; }

        public void PopulateColumnLookups(IDataReader reader)
        {
            var columnNames = new Dictionary<string, int>();
            var columnTypes = new Dictionary<string, Type>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i).ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!columnNames.ContainsKey(name))
                    columnNames.Add(name, i);
                if (!columnTypes.ContainsKey(name))
                    columnTypes.Add(name, reader.GetFieldType(i));
            }

            ColumnNames = columnNames;
            ColumnTypes = columnTypes;
        }
    }
}