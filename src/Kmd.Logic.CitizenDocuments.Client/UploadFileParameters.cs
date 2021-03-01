using System;

namespace Kmd.Logic.CitizenDocuments.Client
{
    public class UploadFileParameters
    {
        public Guid CitizenDocumentConfigId { get; }

        public Guid SubscriptionId { get; }

        public string Cpr { get; }

        public string DocumentName { get; }

        public string DocumentType { get; }

        public int RetentionPeriodInDays { get; }

        public int BufferSize { get; }

        public UploadFileParameters(
            Guid citizenDocumentConfigId,
            Guid subscriptionId,
            string cpr,
            string documentName,
            string documentType,
            int retentionPeriodInDays = 5,
            int bufferSize = 5 * 1024 * 1024)
        {
            this.CitizenDocumentConfigId = citizenDocumentConfigId;
            this.SubscriptionId = subscriptionId;
            this.Cpr = cpr ?? throw new ArgumentNullException(nameof(cpr));
            this.DocumentName = documentName ?? throw new ArgumentNullException(nameof(documentName));
            this.DocumentType = documentType ?? throw new ArgumentNullException(nameof(documentType));
            this.RetentionPeriodInDays = retentionPeriodInDays > 0 ?
                retentionPeriodInDays :
                throw new ArgumentException("RetentionPeriodInDays must be greater than 0", nameof(retentionPeriodInDays));
            this.BufferSize = bufferSize > 0 ?
                bufferSize :
                throw new ArgumentException("BufferSize must be greater than 0", nameof(bufferSize));
        }
    }
}