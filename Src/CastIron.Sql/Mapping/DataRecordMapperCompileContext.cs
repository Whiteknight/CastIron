﻿using System.Collections.Generic;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMapperCompileContext
    {
        public DataRecordMapperCompileContext(IReadOnlyDictionary<string, int> columnNames, ParameterExpression recordParam)
        {
            ColumnNames = columnNames;
            RecordParam = recordParam;
            BindingExpressions = new List<MemberAssignment>();
            MappedColumns = new HashSet<string>();
        }

        public IReadOnlyDictionary<string, int> ColumnNames { get;  }
        public ParameterExpression RecordParam { get; }

        public List<MemberAssignment> BindingExpressions { get; }

        public HashSet<string> MappedColumns { get; }
    }
}