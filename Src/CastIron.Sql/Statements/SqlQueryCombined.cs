using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQueryCombined<T> : ISqlQuery<T>
    {
        private readonly ISqlQuery _query;
        private readonly IResultMaterializer<T> _materializer;
        private readonly bool _cacheMappings;

        public SqlQueryCombined(ISqlQuery query, IResultMaterializer<T> materializer, bool cacheMappings)
        {
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
            _cacheMappings = cacheMappings;
        }

        public bool SetupCommand(IDataInteraction interaction) => _query.SetupCommand(interaction);

        public T Read(IDataResults result)
        {
            result.CacheMappings(_cacheMappings);
            return _materializer.Read(result);
        }

        public override int GetHashCode()
        {
            if (ReferenceEquals(_query, _materializer))
                return _query.GetHashCode();
            return _query.GetHashCode() ^ _materializer.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is SqlQueryCombined<T> typed))
                return false;
            return _cacheMappings == typed._cacheMappings && _query.Equals(typed._query) && _materializer.Equals(typed._materializer);
        }
    }
}