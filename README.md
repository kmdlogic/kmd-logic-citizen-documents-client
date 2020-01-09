# KMD Logic CitizenDocuments Client

A dotnet client library for uploading and sending documents for citizens via the Logic platform.

## How to use this client library

In projects or components where you need to upload or send documents, add a NuGet package reference to [Kmd.Logic.CitizenDocuments.Client](https://www.nuget.org/packages/Kmd.Logic.CitizenDocuments.Client).

The simplest example to get a citizens details is:

```csharp
using (var httpClient = new HttpClient())
{
     var tokenProviderFactory = new LogicTokenProviderFactory(tokenProviderOptions);
     var citizenDocumentClient = new CitizenDocumentsClient(httpClient, tokenProviderFactory, configuration.Citizen);
     var uploadDocument = await citizenDocumentClient.UploadAttachmentWithHttpMessagesAsync(new Guid(configuration.SubscriptionId), configuration.ConfiguartionId, configuration.RetentionPeriodInDays, configuration.Cpr, configuration.DocumentType, configuration.Document, configuration.DocumentName).ConfigureAwait(false);
     var sendDocument = await citizenDocumentClient.SendDocumentWithHttpMessagesAsync(new Guid(configuration.SubscriptionId), new SendCitizenDocumentRequest
}
```

The `LogicTokenProviderFactory` authorizes access to the Logic platform through the use of a Logic Identity issued client credential. The authorization token is reused until it  expires. You would generally create a single instance of `LogicTokenProviderFactory`.

The `CitizenDocumentsClient` accesses the Logic CitizenDocuments service which in turn interacts with one of the data providers.

## How to configure the CitizenDocuments client

Perhaps the easiest way to configure the CitizenDocuments client is from Application Settings.

```json
{
  "TokenProvider": {
    "ClientId": "",
    "ClientSecret": "",
    "AuthorizationScope": "https://logicidentityprod.onmicrosoft.com/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/.default",
    "AuthorizationTokenIssuer": "https://login.microsoftonline.com/logicidentityprod.onmicrosoft.com/oauth2/v2.0/token"
  }
}
```

To get started:

1. Create a subscription in [Logic Console](https://console.kmdlogic.io). This will provide you the `SubscriptionId`.
2. Request a client credential. Once issued you can view the `ClientId` and `ClientSecret` in [Logic Console](https://console.kmdlogic.io).
3. Create a CitizenDocuments configuration. Select the Digital Post configuration and upload the details. This will give you the `CitizenDocumentsConfigurationId`.

## Sample application

A simple console application is included to demonstrate how to call Logic CitizenDocuments API. You will need to provide the settings described above in `appsettings.json`.

When run you should see the details of the _FileAccessPage_ and _messageId_ is printed to the console.


