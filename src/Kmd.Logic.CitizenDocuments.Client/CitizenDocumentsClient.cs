using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Rest;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "HttpClient is not owned by this class.")]
    public sealed class CitizenDocumentsClient
    {
        private readonly HttpClient httpClient;
        private readonly CitizenDocumentsOptions options;
        private readonly LogicTokenProviderFactory tokenProviderFactory;

        private InternalClient internalClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CitizenDocumentClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use. The caller is expected to manage this resource and it will not be disposed.</param>
        /// <param name="tokenProviderFactory">The Logic access token provider factory.</param>
        /// <param name="options">The required configuration options.</param>
        public CitizenDocumentsClient(HttpClient httpClient, LogicTokenProviderFactory tokenProviderFactory, CitizenDocumentsOptions options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.tokenProviderFactory = tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));
        }

        /// <summary>
        /// Uploads the single citizen document.
        /// </summary>
        /// <returns>The fileaccess page details or error if isn't valid.</returns>
        /// <exception cref="ValidationException">Missing cpr number.</exception>
        /// <exception cref="SerializationException">Unable process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen documents configuration details.</exception>
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

        /// <summary>
        ///  Sends the documents to citizens.
        /// </summary>
        /// <param name="sendCitizenDocumentRequest">The send request class.</param>
        /// <returns>The messageId or error if the identifier isn't valid.</returns>
        /// <exception cref="SerializationException">Unable process the service response.</exception>
        /// <exception cref="LogicTokenProviderException">Unable to issue an authorization token.</exception>
        /// <exception cref="CitizenDocumentsException">Invalid Citizen configuration details.</exception>
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
