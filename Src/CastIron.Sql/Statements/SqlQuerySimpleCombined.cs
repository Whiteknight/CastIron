using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQuerySimpleCombined<T> : ISqlQuerySimple<T>
    {
        private readonly ISqlQuerySimple _query;
        private readonly IResultMaterializer<T> _materializer;

        public SqlQuerySimpleCombined(ISqlQuerySimple query, IResultMaterializer<T> materializer)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
        }

        public T Read(IDataResults result)
        {
            return _materializer.Read(result);
        }

        public string GetSql()
        {
            return _query.GetSql();
        }
    }
}