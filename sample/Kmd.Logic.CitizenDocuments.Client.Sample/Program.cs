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

        private static async Task Run(AppConfiguration configuration)
        {
            var validator = new ConfigurationValidator(configuration);
            if (!validator.Validate())
            {
                return;
            }

            using (var httpClient = new HttpClient())
            {
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
                var uploadDocument = await citizenDocumentClient.UploadAttachmentWithHttpMessagesAsync(configuration.ConfiguartionId, configuration.RetentionPeriodInDays, configuration.Cpr, configuration.DocumentType, configuration.Document, configuration.DocumentName).ConfigureAwait(false);

                if (uploadDocument == null)
                {
                    Log.Error("There is error occured in upload");
                    return;
                }

                Log.Information($"Document uploaded successfully and details are :-  DocumentId : {uploadDocument.DocumentId} ; DocumentType : {uploadDocument.DocumentType} ; FileAccessPageUrl : {uploadDocument.FileAccessPageUrl} ");

                var sendDocument = await citizenDocumentClient.SendDocumentWithHttpMessagesAsync(new SendCitizenDocumentRequest
                {
                    ConfigurationId = new Guid(configuration.ConfiguartionId),
                    SendingSystem = configuration.SendingSystem,
                    Cpr = configuration.Cpr,
                    DocumentType = configuration.SendDocumentType,
                    CitizenDocumentId = uploadDocument.DocumentId,
                    Title = configuration.Title,
                    DigitalPostCoverLetterId = uploadDocument.DocumentId,
                    SnailMailCoverLetterId = uploadDocument.DocumentId,
                }).ConfigureAwait(false);

                if (sendDocument == null)
                {
                    Log.Error("There is error occured in send document");
                    return;
                }

                Log.Information("Document was sent and got doc2mail messageId {MessageId}", sendDocument.MessageId);
            }
        }
    }
}