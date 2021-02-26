using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Kmd.Logic.CitizenDocuments.Client
{
    /// <summary>
    /// upload and send documents.
    /// </summary>
    /// <remarks>
    /// To access the Citizen documents you:
    /// - Create a Logic subscription
    /// - Have a client credential issued for the Logic platform
    /// - Create a Citizen document configuration for the distribution service being used.
    /// </remarks>
    public sealed class CitizenDocumentsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CitizenDocumentsOptions _options;
        private readonly LogicTokenProviderFactory _tokenProviderFactory;

        private InternalClient _internalClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CitizenDocumentsClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use. The caller is expected to manage this resource and it will not be disposed.</param>
        /// <param name="tokenProviderFactory">The Logic access token provider factory.</param>
        /// <param name="options">The required configuration options.</param>
        public CitizenDocumentsClient(
            HttpClient httpClient,
            LogicTokenProviderFactory tokenProviderFactory,
            CitizenDocumentsOptions options)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._tokenProviderFactory =
                tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));

#pragma warning disable CS0618 // Type or member is obsolete
            if (string.IsNullOrEmpty(this._tokenProviderFactory.DefaultAuthorizationScope))
            {
                this._tokenProviderFactory.DefaultAuthorizationScope =
                    "https://logicidentityprod.onmicrosoft.com/bb159109-0ccd-4b08-8d0d-80370cedda84/.default";
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Uploads the single citizen document.
        /// </summary>
        /// <param name="configurationId">Citizen document provider config id.</param>
        /// <param name="retentionPeriodInDays">Retention period of the uploaded document.</param>
        /// <param name="cpr">Citizen CPR no.</param>
        /// <param name="documentType">Type of the citizen document.</param>
        /// <param name="document">Original citizen document.</param>
        /// <param name="documentName">Preferred name of citizen document.</param>
        /// <returns>The file access page details or error if isn't valid.</returns>
        /// <exception cref="ValidationException">Missing cpr number.</exception>
        /// <exception cref="SerializationException">Unable to process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen document configuration details.</exception>
        public async Task<CitizenDocumentUploadResponse> UploadAttachmentWithHttpMessagesAsync(
            string configurationId,
            int retentionPeriodInDays,
            string cpr,
            string documentType,
            Stream document,
            string documentName)
        {
            var client = this.CreateClient();

            using var response = await client.UploadAttachmentWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                configurationId: configurationId,
                retentionPeriodInDays: retentionPeriodInDays,
                cpr: cpr,
                documentType: documentType,
                document: document,
                documentName: documentName).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CitizenDocumentUploadResponse)response.Body;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized", response.Body as string);

                default:
                    throw new CitizenDocumentsException(
                        "An unexpected error occurred while processing the request",
                        response.Body as string);
            }
        }

        /// <summary>
        /// Uploads the single citizen document.
        /// </summary>
        /// <param name="document">Original citizen document.</param>
        /// <param name="parameters">citizenDocumentUploadRequestModel to update to db.</param>
        /// <returns>The file access page details or error if isn't valid.</returns>
        /// <exception cref="ValidationException">Missing cpr number.</exception>
        /// <exception cref="SerializationException">Unable to process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen document configuration details.</exception>
        public async Task<CitizenDocumentUploadResponse> UploadFileAsync(
            Stream document,
            UploadFileParameters parameters)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var client = this.CreateClient();
            var documentId = Guid.NewGuid();
            var storageDocName = parameters.DocumentName.Trim().Replace(".", string.Empty) +
                                 "_" + documentId + ".pdf";
            using var responseSasUri = await client.StorageAccessWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                documentName: storageDocName).ConfigureAwait(false);

            if (responseSasUri.Response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new UnauthorizedAccessException("You don't have permission to upload");
            }

            var sasTokenUri = new Uri(responseSasUri.Body);

            var containerAddress =
                new Uri($"{sasTokenUri.Scheme}://{sasTokenUri.Host}/{sasTokenUri.AbsolutePath.Split('/')[1]}");

            CloudBlobContainer container = new CloudBlobContainer(
                containerAddress,
                new StorageCredentials(sasTokenUri.Query));

            var uploadResponse = await UploadDocumentToAzureStorage(document, storageDocName, container, parameters.BufferSize)
                .ConfigureAwait(false);

            if (uploadResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new ApplicationException("Upload Failed");
            }

            var updateDataRequest = new CitizenDocumentUploadRequestModel
            {
                Id = documentId,
                SubscriptionId = parameters.SubscriptionId,
                DocumentUrl = $"{containerAddress}/{storageDocName}",
                Status = "Completed",
                Cpr = parameters.Cpr,
                DocumentName = parameters.DocumentName,
                DocumentType = parameters.DocumentType,
                RetentionPeriodInDays = parameters.RetentionPeriodInDays,
                CitizenDocumentConfigId = parameters.CitizenDocumentConfigId,
            };

            using var updateResponse = await client.UpdateDataToDbWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                updateDataRequest).ConfigureAwait(false);
            switch (updateResponse.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CitizenDocumentUploadResponse)updateResponse.Body;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized", updateResponse.Body as string);

                default:
                    throw new CitizenDocumentsException(
                        "Invalid configuration provided to access Citizen Document service",
                        updateResponse.Body as string);
            }
        }

        private static async Task<UploadResponseModel> UploadDocumentToAzureStorage(
            Stream document,
            string documentName,
            CloudBlobContainer container,
            int size)
        {
            if (documentName == null)
            {
                throw new ArgumentNullException(nameof(documentName));
            }

            CloudBlockBlob blob = container.GetBlockBlobReference(documentName);

            try
            {
                int bytesRead;
                int blockNumber = 0;
                List<string> blockList = new List<string>();
                do
                {
                    blockNumber++;
                    string blockId = $"{blockNumber:0000000}";
                    string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));
                    byte[] buffer = new byte[size];
                    bytesRead = await document.ReadAsync(buffer, 0, size).ConfigureAwait(false);
                    using var bufferStream = new MemoryStream(buffer, 0, bytesRead);
                    await blob.PutBlockAsync(base64BlockId, bufferStream, null)
                        .ConfigureAwait(false);
                    blockList.Add(base64BlockId);
                }
                while (bytesRead == size);

                await blob.PutBlockListAsync(blockList).ConfigureAwait(false);
                return new UploadResponseModel
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Message = "Upload successful",
                };
            }
            catch (ApplicationException ex)
            {
                return new UploadResponseModel
                {
                    StatusCode = System.Net.HttpStatusCode.Conflict,
                    Message = ex.Message,
                };
            }
        }

        /// <summary>
        ///  Sends the documents to citizens.
        /// </summary>
        /// <param name="sendCitizenDocumentRequest">The send request class.</param>
        /// <returns>The messageId or error if the identifier isn't valid.</returns>
        /// <exception cref="SerializationException">Unable to process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen configuration details.</exception>
        public async Task<SendCitizenDocumentResponse> SendDocumentWithHttpMessagesAsync(
            SendCitizenDocumentRequest sendCitizenDocumentRequest)
        {
            var client = this.CreateClient();

            using var response = await client.SendDocumentWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                request: sendCitizenDocumentRequest).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return response.Body;

                case System.Net.HttpStatusCode.NotFound:
                    throw new CitizenDocumentsException(
                        "Provided citizen document id is invalid",
                        response.Response.Content.ReadAsStringAsync().Result);

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException(
                        "Unauthorized",
                        response.Response.Content.ReadAsStringAsync().Result);

                default:
                    throw new CitizenDocumentsException(
                        "An unexpected error occurred while processing the request",
                        response.Response.Content.ReadAsStringAsync().Result);
            }
        }

        private InternalClient CreateClient()
        {
            if (this._internalClient != null)
            {
                return this._internalClient;
            }

            var tokenProvider = this._tokenProviderFactory.GetProvider(this._httpClient);

            this._internalClient = new InternalClient(new TokenCredentials(tokenProvider))
            {
                BaseUri = this._options.ServiceUri,
            };

            return this._internalClient;
        }

        public void Dispose()
        {
            this._httpClient?.Dispose();
            this._tokenProviderFactory?.Dispose();
            this._internalClient?.Dispose();
        }
    }
}