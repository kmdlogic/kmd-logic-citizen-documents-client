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
        public async Task<CitizenDocumentUploadResponse> UploadAttachment1WithHttpMessagesAsync(string configurationId, int retentionPeriodInDays, string cpr, string documentType, IFormFile document, string documentName, CitizenDocumentUploadRequestModel citizenDocumentUploadRequestModel)
        {
            var client = this.CreateClient();


            var response1 = await client.StorageAccessWithHttpMessagesAsync(
                                   subscriptionId: new Guid(this.options.SubscriptionId),
                                   documentName: documentName).ConfigureAwait(false);
            var responseUri = new Uri(response1.ToString());

            CloudBlobContainer container = new CloudBlobContainer(new Uri(""),
                new StorageCredentials(""));
            var uploadresponse = await UploadDocumentAzureStorage(document, documentName, container, 100000).ConfigureAwait(false);
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

        public async Task<string> UploadDocumentAzureStorage(IFormFile document, string documentName, CloudBlobContainer container, int size = 100000)
        {
            var documentId = Guid.NewGuid();
            var _documentName = string.Empty;
            if (!string.IsNullOrWhiteSpace(documentName) && string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(documentName)))
            {
                _documentName = Path.GetFileNameWithoutExtension(document.FileName).Trim() + "_" + documentId + Path.GetExtension(document.FileName);
            }
            else
            {
                _documentName = documentName ?? Path.GetFileNameWithoutExtension(document.FileName);

                if (!string.IsNullOrEmpty(Path.GetExtension(documentName)) && !string.IsNullOrEmpty(Path.GetExtension(document.FileName)))
                {
                    _documentName = Path.GetFileNameWithoutExtension(documentName).Trim() + "_" + documentId + Path.GetExtension(document.FileName);

                }
                else
                {
                    _documentName= documentName.Trim() + "_" + documentId + ".pdf";
                }
            }
            
            
            CloudBlockBlob blob = container.GetBlockBlobReference(_documentName);

            // local variable to track the current number of bytes read into buffer
            int bytesRead;

            // track the current block number as the code iterates through the file
            int blockNumber = 0;

            Stream stream = document.OpenReadStream();
            // Create list to track blockIds, it will be needed after the loop
            List<string> blockList = new List<string>();

            do
            {
                // increment block number by 1 each iteration
                blockNumber++;

                // set block ID as a string and convert it to Base64 which is the required format
                string blockId = $"{blockNumber:0000000}";
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                // create buffer and retrieve chunk
                byte[] buffer = new byte[size];
                bytesRead = await stream.ReadAsync(buffer, 0, size).ConfigureAwait(false);

                // Upload buffer chunk to Azure
                await blob.PutBlockAsync(base64BlockId, new MemoryStream(buffer, 0, bytesRead), null).ConfigureAwait(false);

                // add the current blockId into our list
                blockList.Add(base64BlockId);

                // While bytesRead == size it means there is more data left to read and process
            } while (bytesRead == size);

            // add the blockList to the Azure which allows the resource to stick together the chunks
            await blob.PutBlockListAsync(blockList).ConfigureAwait(false);

            // make sure to dispose the stream once your are done
            stream.Dispose();
            return "ok";
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
