using System;
using System.Runtime.Serialization;

namespace Kmd.Logic.CitizenDocuments.Client
{
    [System.Serializable]
    public class DocumentsException : Exception
    {
        public string InnerMessage { get; }

        public DocumentsException()
        {
        }

        public DocumentsException(string message, string innerMessage)
           : base(message)
        {
            this.InnerMessage = innerMessage;
        }

        public DocumentsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DocumentsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public DocumentsException(string message)
            : base(message)
        {
        }
    }
}