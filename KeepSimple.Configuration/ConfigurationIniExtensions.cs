using System;
using System.IO;

namespace KeepSimple.Configuration
{
    public static class ConfigurationIniExtensions
    {
        public static ConfigurationBuilder AddIniFile(this ConfigurationBuilder builder, string filePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddIniFileInternal(builder.Configuration, filePath);

            return builder;
        }

        public static void AddIniFile(this IConfiguration configuration, string filePath)
        {
            AddIniFileInternal(configuration, filePath);
        }

        private static void AddIniFileInternal(IConfiguration configuration, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                var sectionPrefix = string.Empty;

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine.Trim();

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        continue;
                    }

                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // remove the brackets
                        sectionPrefix = line.Substring(1, line.Length - 2) + ConfigurationPath.PathSeparator;
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator == -1)
                    {
                        throw new FormatException($"Unrecognized line format: \"{rawLine}\".");
                    }

                    string key = sectionPrefix + line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    configuration.AddValue(key, value);
                }
            }
        }
    }
}