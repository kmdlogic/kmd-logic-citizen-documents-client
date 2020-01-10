using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kmd.Logic.Identity.Authorization;

namespace Kmd.Logic.CitizenDocuments.Client.Sample
{
    internal class AppConfiguration
    {
        public CommandLineAction Action { get; set; }

        public LogicTokenProviderOptions TokenProvider { get; set; } = new LogicTokenProviderOptions();

        public CitizenDocumentsOptions Citizen { get; set; } = new CitizenDocumentsOptions();

        public string SubscriptionId { get; set; }

        public string ConfiguartionId { get; set; }

        public Uri Serviceuri { get; set; } = new Uri("https://gateway.kmdlogic.io/cpr/v1");

        public string Cpr { get; set; }

        public int RetentionPeriodInDays { get; set; } = 3;

        public string DocumentType { get; set; } = "CitizenDocument";

        public Stream Document { get; set; } = File.OpenRead("TestPdfInA4Format.pdf");

        public string DocumentName { get; set; } = "TestPdfInA4Format.pdf";

        public string SendingSystem { get; set; } = "test";

        public string SendDocumentType { get; set; } = "alm brev";

        public string Title { get; set; } = "test";
    }
}
