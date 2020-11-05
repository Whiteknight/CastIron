namespace CastIron.Sql
{
    public interface IMapCache
    {
        bool Cache(object key, int set, object map);
        void Clear();
        void Remove(object key);
        void Remove(object key, int set);

        object Get(object key, int set);
    }
}