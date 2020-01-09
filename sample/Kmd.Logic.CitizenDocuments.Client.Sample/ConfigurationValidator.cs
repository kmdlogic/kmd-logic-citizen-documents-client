using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kmd.Logic.CitizenDocuments.Client.sample
{
    internal class ConfigurationValidator
    {
        private readonly AppConfiguration configuration;



        public ConfigurationValidator(AppConfiguration configuration)
        {
            this.configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(this.configuration.TokenProvider?.ClientId)
                || string.IsNullOrWhiteSpace(this.configuration.TokenProvider?.ClientSecret)
                || this.configuration.SubscriptionId == null)
            {
                Log.Error(
                    "Invalid configuration. Please provide proper information to `appsettings.json`. Current data is: {@Settings}",
                    this.configuration);



                return false;
            }
            return true;
        }
    }
}
