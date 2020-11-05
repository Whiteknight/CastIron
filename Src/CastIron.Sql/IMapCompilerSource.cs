using CastIron.Sql.Mapping;

namespace CastIron.Sql
{
    public interface IMapCompilerSource
    {
        void Add(IScalarMapCompiler compiler);
        void Clear();
        IMapCompiler GetCompiler();
    }
}