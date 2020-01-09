using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Rest;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kmd.Logic.CitizenDocuments.Client
{
    public sealed class CitizenDocumentsClient
    {
        private readonly HttpClient httpClient;
        private readonly CitizenDocumentsOptions options;
        private readonly LogicTokenProviderFactory tokenProviderFactory;

        private InternalClient internalClient;

        public CitizenDocumentsClient(HttpClient httpClient, LogicTokenProviderFactory tokenProviderFactory, CitizenDocumentsOptions options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.tokenProviderFactory = tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));
        }

        public async Task<CitizenDocumentUploadResponse> UploadAttachmentWithHttpMessagesAsync(Guid subscriptionId, string configurationId, int retentionPeriodInDays, string cpr, string documentType, Stream document, String documentName)
        {
            var client = this.CreateClient();

            var response = await client.UploadAttachmentWithHttpMessagesAsync(
                                subscriptionId: subscriptionId,
                                configurationId: configurationId,
                                retentionPeriodInDays: retentionPeriodInDays,
                                cpr: cpr,
                                documentType: documentType,
                                document: document, documentName: documentName).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (CitizenDocumentUploadResponse)response.Body;

                case System.Net.HttpStatusCode.NotFound:
                    return null;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized ", response.Body as string);

                default:
                    throw new CitizenDocumentsException("Invalid configuration provided to access Citizen Document service", response.Body as string);
            }
        }

        public async Task<SendCitizenDocumentResponse> SendDocumentWithHttpMessagesAsync(Guid subscriptionId, SendCitizenDocumentRequest sendCitizenDocumentRequest)
        {
            var client = this.CreateClient();

            var response = await client.SendDocumentWithHttpMessagesAsync(
                                subscriptionId: subscriptionId,
                                sendCitizenDocumentRequest: sendCitizenDocumentRequest).ConfigureAwait(false);

            switch (response.Response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return (SendCitizenDocumentResponse)response.Body;

                case System.Net.HttpStatusCode.NotFound:
                    return null;

                case System.Net.HttpStatusCode.Unauthorized:
                    throw new CitizenDocumentsException("Unauthorized ", (response.Response.Content.ReadAsStringAsync()).Result as string);

                default:
                    throw new CitizenDocumentsException("Invalid configuration provided to access CPR service", (response.Response.Content.ReadAsStringAsync()).Result as string);
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
