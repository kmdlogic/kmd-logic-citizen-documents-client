using System;

namespace Kmd.Logic.CitizenDocuments.Client
{
    /// <summary>
    /// Provide the configuration options for using the Citizen Document service.
    /// </summary>
    public sealed class CitizenDocumentsOptions
    {
        /// <summary>
        /// Gets or sets the Logic Citizen Document service.
        /// </summary>
        /// <remarks>
        /// This option should not be overridden except for testing purposes.
        /// </remarks>
        public Uri Serviceuri { get; set; }

        public string SubscriptionId { get; set; }
    }
}
