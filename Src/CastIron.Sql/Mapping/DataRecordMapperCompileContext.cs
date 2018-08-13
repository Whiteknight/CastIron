using System.Collections.Generic;
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
            Variables = new List<ParameterExpression>();
            Statements = new List<Expression>();
        }

        public IReadOnlyDictionary<string, int> ColumnNames { get;  }
        public ParameterExpression RecordParam { get; }

        public List<MemberAssignment> BindingExpressions { get; }

        public HashSet<string> MappedColumns { get; }

        public List<ParameterExpression> Variables { get; }
        public List<Expression> Statements { get; }
    }
}