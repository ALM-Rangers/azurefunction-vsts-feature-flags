using System;
using System.Runtime.Serialization;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    [Serializable]
    internal class SecurityTokenException : Exception
    {
        public SecurityTokenException()
        {
        }

        public SecurityTokenException(string message) : base(message)
        {
        }

        public SecurityTokenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SecurityTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}