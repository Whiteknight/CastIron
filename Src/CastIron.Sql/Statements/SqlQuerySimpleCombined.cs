using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQuerySimpleCombined<T> : ISqlQuerySimple<T>
    {
        private readonly ISqlQuerySimple _query;
        private readonly IResultMaterializer<T> _materializer;
        private readonly bool _cacheMappings;

        public SqlQuerySimpleCombined(ISqlQuerySimple query, IResultMaterializer<T> materializer, bool cacheMappings)
        {
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
            _cacheMappings = cacheMappings;
        }

        public T Read(IDataResults result)
        {
            result.CacheMappings(_cacheMappings);
            return _materializer.Read(result);
        }

        public string GetSql() => _query.GetSql();

        public override int GetHashCode()
        {
            if (ReferenceEquals(_query, _materializer))
                return _query.GetHashCode();
            return _query.GetHashCode() ^ _materializer.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SqlQuerySimpleCombined<T> typed))
                return false;
            return _cacheMappings == typed._cacheMappings && _query.Equals(typed._query) && _materializer.Equals(typed._materializer);
        }
    }
}