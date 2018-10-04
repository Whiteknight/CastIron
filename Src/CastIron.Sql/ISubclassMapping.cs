using System;
using System.Data;

namespace CastIron.Sql
{
    public interface ISubclassMapping<in TParent>
    {
        ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine)
            where T : TParent;
        ISubclassMapping<TParent> Otherwise<T>()
            where T : TParent;
    }
}