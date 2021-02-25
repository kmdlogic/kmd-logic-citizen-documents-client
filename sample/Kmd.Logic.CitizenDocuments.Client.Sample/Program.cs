using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Kmd.Logic.CitizenDocuments.Client.Sample
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            InitLogger();
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddUserSecrets(typeof(Program).Assembly)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build()
                    .Get<AppConfiguration>();

                await Run(config).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Log.Fatal(ex, "Caught a fatal unhandled exception");
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void InitLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static async Task<string> Run(AppConfiguration configuration)
        {
            var validator = new ConfigurationValidator(configuration);
            if (!validator.Validate())
            {
                return "The validation of provider configuration details failed";
            }

            var tokenProviderOptions = new LogicTokenProviderOptions
            {
                AuthorizationScope = configuration.TokenProvider.AuthorizationScope,
                ClientId = configuration.TokenProvider.ClientId,
                ClientSecret = configuration.TokenProvider.ClientSecret,
            };

            if (configuration.TokenProvider.AuthorizationTokenIssuer != null)
            {
                tokenProviderOptions.AuthorizationTokenIssuer = configuration.TokenProvider.AuthorizationTokenIssuer;
            }

            using (var httpClient = new HttpClient())
            using (var tokenProviderFactory = new LogicTokenProviderFactory(tokenProviderOptions))
            {
                configuration.Citizen.SubscriptionId = configuration.SubscriptionId;
                configuration.Citizen.Serviceuri = configuration.Serviceuri;
                var citizenDocumentClient = new CitizenDocumentsClient(httpClient, tokenProviderFactory, configuration.Citizen);
                var uploadDocument = await citizenDocumentClient.UploadAttachmentWithHttpMessagesAsync(configuration.ConfigurationId, configuration.RetentionPeriodInDays, configuration.Cpr, configuration.DocumentType, configuration.Document, configuration.DocumentName).ConfigureAwait(false);
                configuration.Document.Close();
                var uploadWithLargeSizeDocument = await citizenDocumentClient.UploadLargeFileAttachmentWithHttpMessagesAsync(configuration.Document, new CitizenDocumentUploadRequestModel
                {
                    SubscriptionId = new Guid(configuration.SubscriptionId),
                    CitizenDocumentConfigId = new Guid(configuration.ConfigurationId),
                    Cpr = configuration.Cpr,
                    DocumentType = configuration.DocumentType,
                    RetentionPeriodInDays = configuration.RetentionPeriodInDays,
                    DocumentName = configuration.DocumentName,
                }).ConfigureAwait(false);

                Log.Information("The {DocumentType} document with id {DocumentId} and file access page url {FileAccessPageUrl} is uploaded successfully", uploadDocument.DocumentType, uploadDocument.DocumentId, uploadDocument.FileAccessPageUrl);

                var sendDocument = await citizenDocumentClient.SendDocumentWithHttpMessagesAsync(new SendCitizenDocumentRequest
                {
                    ConfigurationId = new Guid(configuration.ConfigurationId),
                    SendingSystem = configuration.SendingSystem,
                    Cpr = configuration.Cpr,
                    DocumentType = configuration.SendDocumentType,
                    CitizenDocumentId = uploadDocument.DocumentId,
                    Title = configuration.Title,
                    DigitalPostCoverLetterId = uploadDocument.DocumentId,
                    SnailMailCoverLetterId = uploadDocument.DocumentId,
                }).ConfigureAwait(false);

                Log.Information("The document is sent successfully and doc2mail provider response message id is {MessageId}", sendDocument.MessageId);

                return "The citizen document was uploaded and sent successfully";
            }
        }
    }
}