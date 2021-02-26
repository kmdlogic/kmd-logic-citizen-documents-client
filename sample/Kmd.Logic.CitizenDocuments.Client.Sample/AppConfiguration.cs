using System;
using Kmd.Logic.Identity.Authorization;

namespace Kmd.Logic.CitizenDocuments.Client.Sample
{
    internal class AppConfiguration
    {
        public LogicTokenProviderOptions TokenProvider { get; set; } = new LogicTokenProviderOptions();

        public string SubscriptionId { get; set; }

        public string ConfigurationId { get; set; }

        public Uri ServiceUri { get; set; } = new Uri("https://gateway.kmdlogic.io/citizen-documents/v1");

        public string Cpr { get; set; }

        public int RetentionPeriodInDays { get; set; } = 3;

        public string DocumentType { get; set; } = "CitizenDocument";

        public string DocumentName { get; set; } = "TestPdfInA4Format.pdf";

        public string SendingSystem { get; set; } = "test";

        public string SendDocumentType { get; set; } = "alm brev";

        public string Title { get; set; } = "test";
    }
}
