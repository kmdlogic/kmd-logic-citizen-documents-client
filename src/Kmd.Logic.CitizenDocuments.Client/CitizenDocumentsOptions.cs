using System;
using System.Collections.Generic;
using System.Text;

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
        public Uri Serviceuri { get; set; } = new Uri(string.Empty);

        public string SubscriptionId { get; set; } = string.Empty;
    }
}
