using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQuerySimpleCombined<T> : ISqlQuerySimple<T>
    {
        private readonly ISqlQuerySimple _query;
        private readonly IResultMaterializer<T> _materializer;

        public SqlQuerySimpleCombined(ISqlQuerySimple query, IResultMaterializer<T> materializer)
        {
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
        }

        public T Read(IDataResults result) => _materializer.Read(result);

        public string GetSql() => _query.GetSql();
    }
}