using System.Data;
using System.Text;

namespace CastIron.Sql.Execution
{
    public interface IDbCommandStringifier
    {
        string Stringify(IDbCommand command);
        string Stringify(IDbCommandAsync command);
        void Stringify(IDbCommand command, StringBuilder sb);
    }
}