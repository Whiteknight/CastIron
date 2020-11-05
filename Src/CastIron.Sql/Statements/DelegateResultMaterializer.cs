using System;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public sealed class DelegateResultMaterializer<T> : IResultMaterializer<T>
    {
        private readonly Func<IDataResults, T> _materialize;

        public DelegateResultMaterializer(Func<IDataResults, T> materialize)
        {
            Argument.NotNull(materialize, nameof(materialize));
            _materialize = materialize;
        }

        // We can't instruct the compiler to cache mappings here, because we don't know what the 
        // query is. Different queries might have columns in different orders but use the same
        // materialization routine, which would be a different map.
        public T Read(IDataResults result) => _materialize(result);

        public override int GetHashCode() => _materialize.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is Func<IDataResults, T> asFunc)
                return _materialize.Equals(asFunc);
            if (obj is DelegateResultMaterializer<T> asMaterializer)
                return _materialize.Equals(asMaterializer._materialize);
            return false;
        }
    }
}