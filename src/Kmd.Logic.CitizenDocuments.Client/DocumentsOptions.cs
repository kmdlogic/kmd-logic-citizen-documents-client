using System;

namespace Kmd.Logic.CitizenDocuments.Client
{
    /// <summary>
    /// Provide the configuration options for using the Document service.
    /// </summary>
    public sealed class DocumentsOptions
    {
        /// <summary>
        /// Gets the Logic Document service.
        /// </summary>
        /// <remarks>
        /// This option should not be overridden except for testing purposes.
        /// </remarks>
        public Uri ServiceUri { get; }

        /// <summary>
        /// Gets the Logic subscription Id.
        /// </summary>
        public string SubscriptionId { get; }

        public DocumentsOptions(string subscriptionId, Uri serviceUri = null)
        {
            this.SubscriptionId = subscriptionId;
            this.ServiceUri = serviceUri ?? new Uri("https://gateway.kmdlogic.io/citizen-documents/v1");
        }
    }
}


