using System;
using System.Data;
using System.Runtime.Serialization;

namespace CastIron.Sql.Mapping
{
    [Serializable]
    public class DataReaderException : Exception
    {
        public DataReaderException()
        {
        }

        public DataReaderException(string message) : base(message)
        {
        }

        public DataReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static DataReaderException ThrowNullReaderException()
        {
            return new DataReaderException(
                $"This result does not contain a data reader. Readers are not produced when executing an {nameof(ISqlCommand)} variant. " +
                $"Depending on your command text, it may still be possible to read output parameters or connection metrics such as {nameof(IDataReader)}.{nameof(IDataReader.RecordsAffected)}");
        }

        public static DataReaderException ThrowRawReaderConsumedException()
        {
            return new DataReaderException(
                $"The raw {nameof(IDataReader)} has already been consumed and cannot be consumed again. " +
                $"The method {nameof(IDataResults.AsRawReader)} relinquishes ownership of the {nameof(IDataReader)}. " +
                $"The code component which holds a reference to the {nameof(IDataReader)} has full control over that object, including reading values and calling {nameof(IDisposable.Dispose)} when necessary.");
        }

        public static DataReaderException ThrowConsumeAfterStreamingStartedException()
        {
            return new DataReaderException(
                $"{nameof(IDataReader)} has already started streaming results. The raw reader cannot be consumed. " +
                $"{nameof(IDataReader)} can either be consumed raw (using .{nameof(IDataResults.AsRawReader)}()) " +
                $"or it can be consumed through this {nameof(IDataResults)} object (using .{nameof(IDataResults.AsEnumerable)}()). " +
                "Once the reader has been consumed, it cannot be consumed again.");
        }

        public static DataReaderException ThrowReaderClosedException()
        {
            return new DataReaderException(
                $"The underlying {nameof(IDataReader)} has already been closed and cannot be consumed again. " +
                $"The {nameof(IDataReader)} object monopolizes the database connection for streaming results while it is open. " +
                $"The {nameof(IDataReader)} is closed to use the connection for other purposes, such as reading output parameters. " +
                $"In your application, make sure to read all result set data from the {nameof(IDataReader)} before attempting to read output parameters to avoid this issue.");
        }
    }
}