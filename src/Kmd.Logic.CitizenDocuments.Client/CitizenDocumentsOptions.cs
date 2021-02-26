using System;

namespace Kmd.Logic.CitizenDocuments.Client
{
    /// <summary>
    /// Provide the configuration options for using the Citizen Document service.
    /// </summary>
    public sealed class CitizenDocumentsOptions
    {
        /// <summary>
        /// Gets the Logic Citizen Document service.
        /// </summary>
        /// <remarks>
        /// This option should not be overridden except for testing purposes.
        /// </remarks>
        public Uri ServiceUri { get; }

        /// <summary>
        /// Gets the Logic subscription Id.
        /// </summary>
        public string SubscriptionId { get; }

        public CitizenDocumentsOptions(Uri serviceUri, string subscriptionId)
        {
            this.ServiceUri = serviceUri;
            this.SubscriptionId = subscriptionId;
        }
    }
}
