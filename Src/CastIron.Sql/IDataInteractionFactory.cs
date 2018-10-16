using System.Data;

namespace CastIron.Sql
{
    public interface IDataInteractionFactory
    {
        IDataInteraction Create(IDbCommand command);
    }
}