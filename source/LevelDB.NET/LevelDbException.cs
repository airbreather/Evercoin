using System;
using System.Runtime.Serialization;

namespace LevelDb
{
    [Serializable]
    public sealed class LevelDbException : Exception
    {
        public LevelDbException()
        {
        }

        public LevelDbException(string message)
            : base(message)
        {
        }

        public LevelDbException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private LevelDbException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
