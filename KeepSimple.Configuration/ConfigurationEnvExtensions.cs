using System;
using System.Collections;

namespace KeepSimple.Configuration
{
    public static class ConfigurationEnvExtensions
    {
        public static ConfigurationBuilder AddEnvironmentVariables(this ConfigurationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddEnvironmentVariablesInternal(builder.Configuration);

            return builder;
        }

        public static void AddEnvironmentVariables(this IConfiguration configuration)
        {
            AddEnvironmentVariablesInternal(configuration);
        }

        private static void AddEnvironmentVariablesInternal(IConfiguration configuration)
        {
            foreach (DictionaryEntry envValue in Environment.GetEnvironmentVariables())
            {
                if (envValue.Key != null && envValue.Value != null)
                    configuration.AddValue(envValue.Key.ToString(), envValue.Value.ToString());
            }
        }
    }
}