using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql
{
    public interface ISubclassMapping<in TParent>
    {
        ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine, Func<IDataRecord, TParent> map = null, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null)
            where T : TParent;
        ISubclassMapping<TParent> Otherwise<T>(Func<IDataRecord, TParent> map = null, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null)
            where T : TParent;
    }
}