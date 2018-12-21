using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public interface IMapCompilerBuilderBase<T>
    {
        IMapCompilerBuilderBase<T> UseMap(Func<IDataRecord, T> map);
        IMapCompilerBuilderBase<T> UseCompiler(IMapCompiler compiler);
        IMapCompilerBuilderBase<T> UseConstructor(ConstructorInfo constructor);
        IMapCompilerBuilderBase<T> UseConstructor(params Type[] argumentTypes);
        IMapCompilerBuilderBase<T> UseConstructorFinder(IConstructorFinder finder);
        IMapCompilerBuilderBase<T> UseFactoryMethod(Func<T> factory);
    }

    public interface IMapCompilerBuilder<T> : IMapCompilerBuilderBase<T>
    {
        IMapCompilerBuilder<T> UseClass<TSpecific>()
            where TSpecific : T;
        IMapCompilerBuilder<T> UseSubclass<TSubclass>(Func<IDataRecord, bool> predicate, Action<IMapCompilerBuilderBase<T>> setup = null)
            where TSubclass : T;
    }
}