using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kmd.Logic.CitizenDocuments.Client.Models;
using Kmd.Logic.CitizenDocuments.Client.sample;
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
                    AuthorizationScope = configuration.TokenProvider.AuthorizationScope, // "https://logicidentityprod.onmicrosoft.com/ac1d197a-0e7c-4add-83c5-e1b30a08efd6/.default",
                    AuthorizationTokenIssuer = new Uri(configuration.TokenProvider.AuthorizationTokenIssuer.ToString()), //new Uri("https://login.microsoftonline.com/logicidentityprod.onmicrosoft.com/oauth2/v2.0/token"),
                    ClientId = configuration.TokenProvider.ClientId, //"085d3847-1b71-4203-aa52-bbb98d5ce57c",
                    ClientSecret = configuration.TokenProvider.ClientSecret // "Ox30ERiezE+gbat7k9jtCmnfKGISFoA8AVjnJo8IgH8="
                };
                var tokenProviderFactory = new LogicTokenProviderFactory(tokenProviderOptions);
                var citizenDocumentClient = new CitizenDocumentsClient(httpClient, tokenProviderFactory, configuration.Citizen);
                var uploadDocument = await citizenDocumentClient.UploadAttachmentWithHttpMessagesAsync(new Guid(configuration.SubscriptionId), configuration.ConfiguartionId, configuration.RetentionPeriodInDays, configuration.Cpr, configuration.DocumentType, configuration.Document, configuration.DocumentName).ConfigureAwait(false);

                if (uploadDocument == null)
                {
                    Log.Error("There is error occured in upload");
                    return;
                }
                Log.Information($"Document uploaded successfully and details are :-  DocumentId : {uploadDocument.DocumentId} ; DocumentType : {uploadDocument.DocumentType} ; FileAccessPageUrl : {uploadDocument.FileAccessPageUrl} ");

                var sendDocument = await citizenDocumentClient.SendDocumentWithHttpMessagesAsync(new Guid(configuration.SubscriptionId), new SendCitizenDocumentRequest
                {
                    ConfigurationId = new Guid(configuration.ConfiguartionId),
                    SendingSystem = configuration.SendingSystem,
                    Cpr = configuration.Cpr,
                    DocumentType = configuration.SendDocumentType,
                    CitizenDocumentId = uploadDocument.DocumentId,
                    Title = configuration.title,
                    DigitalPostCoverLetterId = uploadDocument.DocumentId,
                    SnailMailCoverLetterId = uploadDocument.DocumentId
                });
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