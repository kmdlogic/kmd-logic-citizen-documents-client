using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
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
    /// To access the citizen/company documents you:
    /// - Create a Logic subscription
    /// - Have a client credential issued for the Logic platform
    /// - Create a Conpany document configuration for the distribution service being used.
    /// </remarks>
    public sealed class CompanyDocumentsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly DocumentsOptions _options;
        private readonly ITokenProviderFactory _tokenProviderFactory;

        private InternalClient _internalClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyDocumentsClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use. The caller is expected to manage this resource and it will not be disposed.</param>
        /// <param name="tokenProviderFactory">The Logic access token provider factory.</param>
        /// <param name="options">The required configuration options.</param>
        public CompanyDocumentsClient(
            HttpClient httpClient,
            ITokenProviderFactory tokenProviderFactory,
            DocumentsOptions options)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._tokenProviderFactory =
                tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));
        }

        public async Task<CompanyDocumentUploadResponse> UploadAttachmentWithHttpMessagesAsync(
            Guid documentConfigurationId,
            List<string> cvrs,
            Stream document,
            int retentionPeriodInDays,
            string companyDocumentType,
            string documentName,
            string sender,
            string documentComment)
        {
            var client = this.CreateClient();
            using var response = await client.UploadAttachmentForCompaniesWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                documentConfigurationId: documentConfigurationId.ToString(),
                cvrs: cvrs,
                document: document,
                retentionPeriodInDays: retentionPeriodInDays,
                companyDocumentType: companyDocumentType,
                documentName: documentName,
                sender: sender,
                documentComment: documentComment).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CompanyDocumentUploadResponse)response.Body;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new DocumentsException("Unauthorized", response.Body as string);

                default:
                    throw new DocumentsException(
                        "An unexpected error occurred while processing the request",
                        response.Body as string);
            }
        }

        public async Task<CompanyDocumentResponse> UpdateCompanyDataToDbWithHttpMessagesAsync(CompanyDocumentRequest request)
        {
            var client = this.CreateClient();
            using var response = await client.UpdateCompanyDataToDbWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                request).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CompanyDocumentResponse)response.Body;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new DocumentsException("Unauthorized", response.Body as string);

                default:
                    throw new DocumentsException(
                        "An unexpected error occurred while processing the request",
                        response.Body as string);
            }
        }

        public async Task<CompanyDocumentResponse> UploadCompanyFileAsync(
           Stream document,
           CompanyDocumentRequest parameters)
        {
            var bufferSize = 5 * 1024 * 1024;
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

            var uploadResponse = await UploadDocumentToAzureStorage(document, storageDocName, container, bufferSize)
                .ConfigureAwait(false);

            if (uploadResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new ApplicationException("Upload Failed");
            }

            parameters.DocumentUrl = $"{containerAddress}/{storageDocName}";
            parameters.Id = documentId;

            return await this.UpdateCompanyDataToDbWithHttpMessagesAsync(
                parameters).ConfigureAwait(false);
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