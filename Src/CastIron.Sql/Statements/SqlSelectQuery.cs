using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace CastIron.Sql.Statements
{
    public class SqlSelectQuery<T> : ISqlSelectQuery<T>
    {
        private readonly ISelectListBuilder _selectListBuilder;
        private string _criteria;

        public SqlSelectQuery(ISelectListBuilder selectListBuilder = null)
        {
            _selectListBuilder = selectListBuilder ?? SelectListBuilder.GetCached();
        }

        public bool SetupCommand(IDbCommand command)
        {
            command.CommandType = CommandType.Text;
            command.CommandText = GetSql();
            // TODO: Add parameters, if the criteria include any
            return true;
        }

        private string GetSql()
        {
            return $@"
SELECT
    {_selectListBuilder.BuildSelectList(typeof(T), "X")}
    FROM
        {SqlUtilities.GetTableName(typeof(T))} AS X
    WHERE
        {_criteria}
;";
        }

        public IEnumerable<T> Read(SqlResultSet result)
        {
            return result.AsEnumerable<T>().ToList();
        }

        private class WhereClauseBuilder : ISelectWhereClauseBuilder<T>
        {
            private string _criteria;

            public string Build()
            {
                return _criteria ?? "1 = 1";
            }

            public void And(params Action<ISelectWhereClauseBuilder<T>>[] conditions)
            {
                var strings = conditions
                    .Select(c =>
                    {
                        var builder = new WhereClauseBuilder();
                        c?.Invoke(builder);
                        return builder.Build();
                    })
                    .Select(sc => "(" + sc.ToString() + ")");

                _criteria = string.Join(" AND ", strings);
            }

            public void Or(params Action<ISelectWhereClauseBuilder<T>>[] conditions)
            {
                var strings = conditions
                    .Select(c =>
                    {
                        var builder = new WhereClauseBuilder();
                        c?.Invoke(builder);
                        return builder.Build();
                    })
                    .Select(sc => "(" + sc.ToString() + ")");

                _criteria = string.Join(" OR ", strings);
            }

            public void Equal<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                Equal(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void Equal(string property, object value)
            {
                _criteria = $"{property} = {SqlUtilities.ToSqlValue(value)}";
            }

            public void NotEqual<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                NotEqual(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void NotEqual(string property, object value)
            {
                _criteria = $"{property} <> {SqlUtilities.ToSqlValue(value)}";
            }

            public void GreaterThan<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                GreaterThan(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void GreaterThan(string property, object value)
            {
                _criteria = $"{property} > {SqlUtilities.ToSqlValue(value)}";
            }

            public void GreaterThanOrEqual<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                GreaterThanOrEqual(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void GreaterThanOrEqual(string property, object value)
            {
                _criteria = $"{property} >= {SqlUtilities.ToSqlValue(value)}";
            }

            public void LessThan<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                LessThan(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void LessThan(string property, object value)
            {
                _criteria = $"{property} < {SqlUtilities.ToSqlValue(value)}";
            }

            public void LessThanOrEqual<TProperty>(Expression<Func<T, TProperty>> property, object value)
            {
                LessThanOrEqual(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void LessThanOrEqual(string property, object value)
            {
                _criteria = $"{property} <= {SqlUtilities.ToSqlValue(value)}";
            }

            public void Like<TProperty>(Expression<Func<T, TProperty>> property, string value)
            {
                Like(SqlUtilities.GetPropertyColumnName(property), value);
            }

            public void Like(string property, string value)
            {
                _criteria = $"{property} LIKE '{value.Replace("'", "''")}'";
            }

            public void Between<TProperty>(Expression<Func<T, TProperty>> property, object value1, object value2)
            {
                Between(SqlUtilities.GetPropertyColumnName(property), value1, value2);
            }

            public void Between(string property, object value1, object value2)
            {
                _criteria = $"{property} BETWEEN {SqlUtilities.ToSqlValue(value1)} AND {SqlUtilities.ToSqlValue(value2)}";
            }
        }

        public ISqlSelectQuery<T> Where(Action<ISelectWhereClauseBuilder<T>> build)
        {
            Assert.ArgumentNotNull(build, nameof(build));
            var builder = new WhereClauseBuilder();
            build?.Invoke(builder);
            _criteria = builder.Build();
            return this;
        }
    }
}