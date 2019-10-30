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

        public T Read(IDataResults result) => _materialize(result);
    }
}