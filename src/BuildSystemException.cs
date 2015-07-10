using System;
using System.Runtime.Serialization;

namespace BuildSystem
{
    class BuildSystemException : Exception
    {
        public BuildSystemException()
        {
        }

        public BuildSystemException(String message)
            : base(message)
        {
        }

        public BuildSystemException(String message, Exception innerException)
            : base(message, innerException) 
        {
        }

        protected BuildSystemException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
