using System;
using System.Runtime.Serialization;

namespace CastIron.Sql.Execution
{
    [Serializable]
    public class ExecutionContextException : Exception
    {
        public ExecutionContextException()
        {
        }

        public ExecutionContextException(string message) : base(message)
        {
        }

        public ExecutionContextException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ExecutionContextException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static ExecutionContextException ConnectionAlreadyOpen()
        {
            return new ExecutionContextException(
                "The connection is already open. " + 
                "In this state, the connection cannot be opened again and certain details such as " +
                "the transaction or isolation level cannot be modified");
        }

        public static ExecutionContextException ConnectionNotOpen()
        {
            return new ExecutionContextException(
                "The connection is not open. " +
                "Certain actions such as attempting to create or execute a command may only be performed " +
                "when the connection is open.");

        }
    }
}