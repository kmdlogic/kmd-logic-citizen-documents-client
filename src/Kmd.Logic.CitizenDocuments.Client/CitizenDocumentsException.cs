using System;
using System.Runtime.Serialization;

namespace Kmd.Logic.CitizenDocuments.Client
{
    [System.Serializable]
    public class CitizenDocumentsException : Exception
    {
        public string InnerMessage { get; }

        public CitizenDocumentsException()
        {
        }

        public CitizenDocumentsException(string message, Microsoft.Rest.HttpOperationResponse<object> response)
            : base(message)
        {
        }

        public CitizenDocumentsException(string message, string innerMessage)
           : base(message)
        {
            this.InnerMessage = innerMessage;
        }

        public CitizenDocumentsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CitizenDocumentsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CitizenDocumentsException(string message)
            : base(message)
        {
        }
    }
}
