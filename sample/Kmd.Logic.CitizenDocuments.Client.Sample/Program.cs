using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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

            using var httpClient = new HttpClient();
            using var tokenProviderFactory = new LogicTokenProviderFactory(tokenProviderOptions);
            var options = new CitizenDocumentsOptions(
                configuration.Serviceuri,
                configuration.SubscriptionId);

            using var citizenDocumentClient = new CitizenDocumentsClient(httpClient, tokenProviderFactory, options);
            using Stream stream = File.OpenRead(configuration.DocumentName);

            var uploadWithLargeSizeDocument = await citizenDocumentClient.UploadFileAsync(stream, new UploadFileParameters(
                    new Guid(configuration.ConfigurationId),
                    new Guid(configuration.SubscriptionId),
                    cpr: configuration.Cpr,
                    documentName: configuration.DocumentName,
                    documentType: configuration.DocumentType,
                    retentionPeriodInDays: configuration.RetentionPeriodInDays))
                .ConfigureAwait(false);

            Log.Information("The {DocumentType} document with id {DocumentId} and file access page url {FileAccessPageUrl} is uploaded successfully", uploadWithLargeSizeDocument.DocumentType, uploadWithLargeSizeDocument.DocumentId, uploadWithLargeSizeDocument.FileAccessPageUrl);

            return "The citizen document was uploaded successfully";
        }
    }
}