using System;
using System.Collections.Generic;
using System.Text;

namespace CastIron.Sql
{
    public interface IProviderConfiguration
    {
        string UnnamedColumnName { get;  }
    }
}
