using System;
using System.Collections.Generic;

namespace KeepSimple.Configuration
{
    public static class ConfigurationCommandLineExtensions
    {
        public static ConfigurationBuilder AddCommandLine(this ConfigurationBuilder builder, string[] args, IDictionary<string, string> switchMappings = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddCommandLineInternal(builder.Configuration, args, switchMappings);

            return builder;
        }

        public static void AddCommandLine(this IConfiguration configuration, string[] args, IDictionary<string, string> switchMappings = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            AddCommandLineInternal(configuration, args, switchMappings);
        }

        private static void AddCommandLineInternal(IConfiguration configuration, IEnumerable<string> arguments, IDictionary<string, string> switchMappings)
        {
            if (arguments == null)
                return;

            Dictionary<string, string> activeSwitchMappings = null;
            if (switchMappings != null)
            {
                activeSwitchMappings = GetValidatedSwitchMappingsCopy(switchMappings);
            }

            string key, value;
            using (var enumerator = arguments.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentArg = enumerator.Current;
                    var keyStartIndex = 0;

                    if (currentArg.StartsWith("--"))
                    {
                        keyStartIndex = 2;
                    }
                    else if (currentArg.StartsWith("-"))
                    {
                        keyStartIndex = 1;
                    }
                    else if (currentArg.StartsWith("/"))
                    {
                        // "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
                        // So we do a conversion to simplify later processing
                        currentArg = string.Format("--{0}", currentArg.Substring(1));
                        keyStartIndex = 2;
                    }

                    var separator = currentArg.IndexOf('=');
                    if (separator == -1)
                    {
                        // If there is neither equal sign nor prefix in current arugment, it is an invalid format
                        if (keyStartIndex == 0)
                        {
                            // Ignore invalid formats
                            continue;
                        }

                        // If the switch is a key in given switch mappings, interpret it
                        if (activeSwitchMappings != null && activeSwitchMappings.ContainsKey(currentArg))
                        {
                            key = activeSwitchMappings[currentArg];
                        }
                        // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage so ignore it
                        else if (keyStartIndex == 1)
                        {
                            continue;
                        }
                        // Otherwise, use the switch name directly as a key
                        else
                        {
                            key = currentArg.Substring(keyStartIndex);
                        }

                        var previousKey = enumerator.Current;
                        if (!enumerator.MoveNext())
                        {
                            // ignore missing values
                            continue;
                        }

                        value = enumerator.Current;
                    }
                    else
                    {
                        var keySegment = currentArg.Substring(0, separator);

                        // If the switch is a key in given switch mappings, interpret it
                        if (activeSwitchMappings != null && activeSwitchMappings.ContainsKey(keySegment))
                        {
                            key = activeSwitchMappings[keySegment];
                        }
                        // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
                        else if (keyStartIndex == 1)
                        {
                            throw new FormatException($"Short switch is not defined: {currentArg}");
                        }
                        // Otherwise, use the switch name directly as a key
                        else
                        {
                            key = currentArg.Substring(keyStartIndex, separator - keyStartIndex);
                        }

                        value = currentArg
                            .Substring(separator + 1)
                            .Trim('"');
                    }

                    // Override value when key is duplicated. So we always have the last argument win.
                    configuration.AddValue(key, value);
                }
            }
        }

        private static Dictionary<string, string> GetValidatedSwitchMappingsCopy(IDictionary<string, string> switchMappings)
        {
            // The dictionary passed in might be constructed with a case-sensitive comparer
            // However, the keys in configuration providers are all case-insensitive
            // So we check whether the given switch mappings contain duplicated keys with case-insensitive comparer
            var switchMappingsCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in switchMappings)
            {
                // Only keys start with "--" or "-" are acceptable
                if (!mapping.Key.StartsWith("-") && !mapping.Key.StartsWith("--"))
                {
                    throw new ArgumentException($"Invalid switch mapping: {mapping.Key}");
                }

                if (switchMappingsCopy.ContainsKey(mapping.Key))
                {
                    throw new ArgumentException($"Duplicated key in switch mappings: {mapping.Key}");
                }

                switchMappingsCopy.Add(mapping.Key, mapping.Value);
            }

            return switchMappingsCopy;
        }
    }
}