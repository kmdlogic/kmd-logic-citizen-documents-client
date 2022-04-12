// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Kmd.Logic.CitizenDocuments.Client.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class CitizenDocumentUpdateRequest
    {
        /// <summary>
        /// Initializes a new instance of the CitizenDocumentUpdateRequest
        /// class.
        /// </summary>
        public CitizenDocumentUpdateRequest()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CitizenDocumentUpdateRequest
        /// class.
        /// </summary>
        /// <param name="documentType">Possible values include:
        /// 'CitizenDocument', 'DigitalPostCoverLetter',
        /// 'SnailMailCoverLetter'</param>
        /// <param name="status">Possible values include: 'InProgress',
        /// 'Completed', 'Failed'</param>
        public CitizenDocumentUpdateRequest(System.Guid? id = default(System.Guid?), System.Guid? subscriptionId = default(System.Guid?), System.Guid? citizenDocumentConfigId = default(System.Guid?), string cpr = default(string), string documentType = default(string), System.DateTime? uploadedAt = default(System.DateTime?), string uploadedBy = default(string), string documentUrl = default(string), int? retentionPeriodInDays = default(int?), string status = default(string), string fileName = default(string), string documentName = default(string))
        {
            Id = id;
            SubscriptionId = subscriptionId;
            CitizenDocumentConfigId = citizenDocumentConfigId;
            Cpr = cpr;
            DocumentType = documentType;
            UploadedAt = uploadedAt;
            UploadedBy = uploadedBy;
            DocumentUrl = documentUrl;
            RetentionPeriodInDays = retentionPeriodInDays;
            Status = status;
            FileName = fileName;
            DocumentName = documentName;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public System.Guid? Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionId")]
        public System.Guid? SubscriptionId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "citizenDocumentConfigId")]
        public System.Guid? CitizenDocumentConfigId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "cpr")]
        public string Cpr { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'CitizenDocument',
        /// 'DigitalPostCoverLetter', 'SnailMailCoverLetter'
        /// </summary>
        [JsonProperty(PropertyName = "documentType")]
        public string DocumentType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "uploadedAt")]
        public System.DateTime? UploadedAt { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "uploadedBy")]
        public string UploadedBy { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "documentUrl")]
        public string DocumentUrl { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "retentionPeriodInDays")]
        public int? RetentionPeriodInDays { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'InProgress', 'Completed',
        /// 'Failed'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "documentName")]
        public string DocumentName { get; set; }

    }
}
