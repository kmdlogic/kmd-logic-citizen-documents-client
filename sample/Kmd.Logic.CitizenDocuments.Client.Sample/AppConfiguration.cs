using Kmd.Logic.Identity.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kmd.Logic.CitizenDocuments.Client.sample
{
    internal class AppConfiguration
    {

        public LogicTokenProviderOptions TokenProvider { get; set; } = new LogicTokenProviderOptions();

        public CitizenDocumentsOptions Citizen { get; set; } = new CitizenDocumentsOptions();

        public string SubscriptionId { get; set; } = "";

        public string ConfiguartionId { get; set; } = "";

        public Uri Serviceuri { get; set; } = new Uri("");

        public string Cpr { get; set; } = "";

        public int RetentionPeriodInDays { get; set; } = 3;

        public string DocumentType { get; set; } = "CitizenDocument";

        public Stream Document { get; set; } = File.OpenRead("TestPdfInA4Format.pdf");

        public string DocumentName { get; set; } = "TestPdfInA4Format.pdf";

        public string SendingSystem { get; set; } = "test";

        public string SendDocumentType { get; set; } = "alm brev";

        public string title { get; set; } = "test";


    }
}
