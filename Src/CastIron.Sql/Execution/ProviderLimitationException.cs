using System;
using System.Runtime.Serialization;

namespace CastIron.Sql.Execution
{
    [Serializable]
    public class ProviderLimitationException : Exception
    {
        public ProviderLimitationException()
        {
        }

        public ProviderLimitationException(string message)
            : base(message)
        {
        }

        public ProviderLimitationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ProviderLimitationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
