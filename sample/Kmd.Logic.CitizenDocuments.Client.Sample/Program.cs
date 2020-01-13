using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.Identity.Authorization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Kmd.Logic.CitizenDocuments.Client.Sample
{
    [SuppressMessage("Design", "CA2000:Types that own disposable fields should be disposable", Justification = "HttpClient is not owned by this class.")]
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

                switch (config.Action)
                {
                    case CommandLineAction.None:

                        Log.Information("You must provide arguments");
                        Log.Verbose("You must get a bearer token from the https://console.kmdlogic.io/ or using Client Credentials for your subscription.");
                        Log.Verbose("Examples:");
                        Log.Verbose("--Action=UploadDocument --SubscriptionId={SubscriptionId} --ConfigurationId={ConfigurationId} ... --BearerToken={BearerToken}", "INSERT", "INSERT", "INSERT");
                        Log.Verbose("--Action=SendDocument --SubscriptionId={SubscriptionId} --ConfigurationId={ConfigurationId} ... --BearerToken={BearerToken}", "INSERT", "INSERT", "INSERT");
                        break;

                    case CommandLineAction.UploadDocument:
                        await UploadAttachmentAsync(config).ConfigureAwait(false);
                        break;

                    case CommandLineAction.SendDocument:
                        await SendDocumentAsync(config).ConfigureAwait(false);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"Unknown action {config.Action}");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log.Fatal(ex, "Caught a fatal unhandled exception");
            }
            finally
            {
                Log.Information("Shutting down");
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

        private static CitizenDocumentsClient GetClient(AppConfiguration configuration)
        {
            var validator = new ConfigurationValidator(configuration);
            if (!validator.Validate())
            {
                return null;
            }

            var httpClient = new HttpClient();
            LogicTokenProviderOptions tokenProviderOptions = new LogicTokenProviderOptions()
            {
                AuthorizationScope = configuration.TokenProvider.AuthorizationScope,
                AuthorizationTokenIssuer = new Uri(configuration.TokenProvider.AuthorizationTokenIssuer.ToString()),
                ClientId = configuration.TokenProvider.ClientId,
                ClientSecret = configuration.TokenProvider.ClientSecret,
            };
            configuration.Citizen.SubscriptionId = configuration.SubscriptionId;
            configuration.Citizen.Serviceuri = configuration.Serviceuri;
            var tokenProviderFactory = new LogicTokenProviderFactory(tokenProviderOptions);
            var citizenDocumentClient = new CitizenDocumentsClient(httpClient, tokenProviderFactory, configuration.Citizen);

            return citizenDocumentClient;
        }

        private static async Task<Guid> UploadAttachmentAsync(AppConfiguration config)
        {
            var citizenDocumentClient = GetClient(config);
            var uploadDocument = await citizenDocumentClient.UploadAttachmentWithHttpMessagesAsync(config.ConfigurationId, config.RetentionPeriodInDays, config.Cpr, config.DocumentType, config.Document, config.DocumentName).ConfigureAwait(false);

            if (uploadDocument == null)
            {
                Log.Error("There is error occured in upload");
                return Guid.Empty;
            }

            Log.Information($"Document uploaded successfully and details are :-  DocumentId : {uploadDocument.DocumentId} ; DocumentType : {uploadDocument.DocumentType} ; FileAccessPageUrl : {uploadDocument.FileAccessPageUrl} ");
            return uploadDocument.DocumentId.Value;
        }

        private static async Task SendDocumentAsync(AppConfiguration config)
        {
            var documentId = await UploadAttachmentAsync(config).ConfigureAwait(false);
            var citizenDocumentClient = GetClient(config);
            var sendDocument = await citizenDocumentClient.SendDocumentWithHttpMessagesAsync(new SendCitizenDocumentRequest()
            {
                ConfigurationId = new Guid(config.ConfigurationId),
                SendingSystem = config.SendingSystem,
                Cpr = config.Cpr,
                DocumentType = config.SendDocumentType,
                CitizenDocumentId = documentId,
                Title = config.Title,
                DigitalPostCoverLetterId = documentId,
                SnailMailCoverLetterId = documentId,
            }).ConfigureAwait(false);

            if (sendDocument == null)
            {
                Log.Error("An error occurred while send document operation");
                return;
            }

            Log.Information("Document was sent and got doc2mail messageId {MessageId}", sendDocument.MessageId);
        }
    }
}