using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Rest;

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
            string cpr,
            int retentionPeriodInDays,
            string companyDocumentType,
            string documentName)
        {
            var client = this.CreateClient();
            using var response = await client.UploadAttachmentForCompaniesWithHttpMessagesAsync(
                subscriptionId: new Guid(this._options.SubscriptionId),
                documentConfigurationId: documentConfigurationId.ToString(),
                cvrs: cvrs,
                document: document,
                cpr: cpr,
                retentionPeriodInDays: retentionPeriodInDays,
                companyDocumentType: companyDocumentType,
                documentName: documentName).ConfigureAwait(false);

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