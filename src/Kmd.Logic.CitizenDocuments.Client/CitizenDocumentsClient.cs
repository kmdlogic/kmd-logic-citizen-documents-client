using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.AspNetCore.Http;
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
    [SuppressMessage("Design", "CA2000:Types that own disposable fields should be disposable", Justification = "HttpClient is not owned by this class.")]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public sealed class CitizenDocumentsClient
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly HttpClient httpClient;
        private readonly CitizenDocumentsOptions options;
        private readonly LogicTokenProviderFactory tokenProviderFactory;

        private InternalClient internalClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CitizenDocumentsClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use. The caller is expected to manage this resource and it will not be disposed.</param>
        /// <param name="tokenProviderFactory">The Logic access token provider factory.</param>
        /// <param name="options">The required configuration options.</param>
        public CitizenDocumentsClient(HttpClient httpClient, LogicTokenProviderFactory tokenProviderFactory, CitizenDocumentsOptions options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.tokenProviderFactory = tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));

#pragma warning disable CS0618 // Type or member is obsolete
            if (string.IsNullOrEmpty(this.tokenProviderFactory.DefaultAuthorizationScope))
            {
                this.tokenProviderFactory.DefaultAuthorizationScope = "https://logicidentityprod.onmicrosoft.com/bb159109-0ccd-4b08-8d0d-80370cedda84/.default";
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
        /// <returns>The fileaccess page details or error if isn't valid.</returns>
        /// <exception cref="ValidationException">Missing cpr number.</exception>
        /// <exception cref="SerializationException">Unable to process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen document configuration details.</exception>
        public async Task<CitizenDocumentUploadResponse> UploadAttachmentWithHttpMessagesAsync(string configurationId, int retentionPeriodInDays, string cpr, string documentType, Stream document, string documentName)
        {
            var client = this.CreateClient();

            var response = await client.UploadAttachmentWithHttpMessagesAsync(
                                subscriptionId: new Guid(this.options.SubscriptionId),
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
                    throw new CitizenDocumentsException("An unexpected error occurred while processing the request", response.Body as string);
            }
        }

        /// <summary>
        /// Uploads the single citizen document.
        /// </summary>
        /// <param name="document">Original citizen document.</param>
        /// <param name="citizenDocumentUploadRequestModel">citizenDocumentUploadRequestModel to update to db.</param>
        /// <returns>The fileaccess page details or error if isn't valid.</returns>
        /// <exception cref="ValidationException">Missing cpr number.</exception>
        /// <exception cref="SerializationException">Unable to process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen document configuration details.</exception>
        public async Task<CitizenDocumentUploadResponse> UploadLargeFileAttachmentWithHttpMessagesAsync(Stream document, CitizenDocumentUploadRequestModel citizenDocumentUploadRequestModel)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (citizenDocumentUploadRequestModel == null)
            {
                throw new ArgumentNullException(nameof(citizenDocumentUploadRequestModel));
            }

            var client = this.CreateClient();
            var documentId = Guid.NewGuid();
            var storageDocName = citizenDocumentUploadRequestModel.DocumentName.Trim().Replace(".", string.Empty) + "_" + documentId + ".pdf";
            var responseSasUri = await client.StorageAccessWithHttpMessagesAsync(
                                   subscriptionId: new Guid(this.options.SubscriptionId),
                                   documentName: storageDocName).ConfigureAwait(false);

            var sasTokenUri = new Uri(responseSasUri.Body);

            var containerAddress = new Uri($"{sasTokenUri.Scheme}://{sasTokenUri.Host}/{sasTokenUri.AbsolutePath.Split('/')[1]}");

            CloudBlobContainer container = new CloudBlobContainer(
                containerAddress,
                new StorageCredentials(sasTokenUri.Query));

            await UploadDocumentAzureStorage(document, storageDocName, container, 100000).ConfigureAwait(false);

            var updateResponse = await client.UpdateDataToDbWithHttpMessagesAsync(
                                 subscriptionId: new Guid(this.options.SubscriptionId),
                                 request: citizenDocumentUploadRequestModel).ConfigureAwait(false);
            switch (updateResponse.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CitizenDocumentUploadResponse)updateResponse.Body;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized", updateResponse.Body as string);

                default:
                    throw new CitizenDocumentsException("Invalid configuration provided to access Citizen Document service", updateResponse.Body as string);
            }
        }

        private static async Task<string> UploadDocumentAzureStorage(Stream document, string documentName, CloudBlobContainer container, int size = 100000)
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
                    await blob.PutBlockAsync(base64BlockId, new MemoryStream(buffer, 0, bytesRead), null).ConfigureAwait(false);
                    blockList.Add(base64BlockId);
                }
                while (bytesRead == size);
                await blob.PutBlockListAsync(blockList).ConfigureAwait(false);
                document.Dispose();
                return "ok";
            }
            catch (ArgumentNullException ex)
            {
                return ex.ToString();
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
        public async Task<SendCitizenDocumentResponse> SendDocumentWithHttpMessagesAsync(SendCitizenDocumentRequest sendCitizenDocumentRequest)
        {
            var client = this.CreateClient();

            var response = await client.SendDocumentWithHttpMessagesAsync(
                                subscriptionId: new Guid(this.options.SubscriptionId),
                                request: sendCitizenDocumentRequest).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (SendCitizenDocumentResponse)response.Body;

                case System.Net.HttpStatusCode.NotFound:
                    throw new CitizenDocumentsException("Provided citizen document id is invalid", response.Response.Content.ReadAsStringAsync().Result as string);

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized", response.Response.Content.ReadAsStringAsync().Result as string);

                default:
                    throw new CitizenDocumentsException("An unexpected error occurred while processing the request", response.Response.Content.ReadAsStringAsync().Result as string);
            }
        }

        private InternalClient CreateClient()
        {
            if (this.internalClient != null)
            {
                return this.internalClient;
            }

            var tokenProvider = this.tokenProviderFactory.GetProvider(this.httpClient);

            this.internalClient = new InternalClient(new TokenCredentials(tokenProvider))
            {
                BaseUri = this.options.Serviceuri,
            };

            return this.internalClient;
        }
    }
}
