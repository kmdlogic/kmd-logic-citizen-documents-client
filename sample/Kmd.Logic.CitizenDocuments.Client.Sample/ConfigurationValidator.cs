using System;
using Serilog;

namespace Kmd.Logic.CitizenDocuments.Client.Sample
{
    internal class ConfigurationValidator
    {
        private readonly AppConfiguration _configuration;

        public ConfigurationValidator(AppConfiguration configuration)
        {
            this._configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(this._configuration.TokenProvider?.ClientId)
                || string.IsNullOrWhiteSpace(this._configuration.TokenProvider?.ClientSecret)
                || string.IsNullOrWhiteSpace(this._configuration.TokenProvider?.AuthorizationScope)
                || !Guid.TryParse(this._configuration.SubscriptionId, out _)
                || !Guid.TryParse(this._configuration.ConfigurationId, out _))
            {
                Log.Error(
                    "Invalid configuration. Please provide proper information to `appsettings.json`. Current data is: {@Settings}",
                    this._configuration);
                return false;
            }

            return true;
        }
    }
}
