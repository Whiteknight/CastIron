using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQueryCombined<T> : ISqlQuery<T>
    {
        private readonly ISqlQuery _query;
        private readonly IResultMaterializer<T> _materializer;

        public SqlQueryCombined(ISqlQuery query, IResultMaterializer<T> materializer)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
        }

        public bool SetupCommand(IDataInteraction interaction)
        {
            return _query.SetupCommand(interaction);
        }

        public T Read(IDataResults result)
        {
            return _materializer.Read(result);
        }
    }
}