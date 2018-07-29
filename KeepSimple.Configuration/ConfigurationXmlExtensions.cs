using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace KeepSimple.Configuration
{
    public static class ConfigurationXmlExtensions
    {
        #region Consts

        private const string NameAttributeKey = "Name";

        #endregion

        #region Extension methods

        public static ConfigurationBuilder AddXmlFile(this ConfigurationBuilder builder, string filePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddXmlFileInternal(builder.Configuration, filePath);

            return builder;
        }

        public static void AddXmlFile(this IConfiguration configuration, string filePath)
        {
            AddXmlFileInternal(configuration, filePath);
        }

        #endregion

        private static void AddXmlFileInternal(IConfiguration configuration, string filePath)
        {
            var readerSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = XmlReader.Create(stream, readerSettings))
                {
                    var prefixStack = new Stack<string>();

                    SkipUntilRootElement(reader);

                    // We process the root element individually since it doesn't contribute to prefix 
                    ProcessAttributes(configuration, reader, prefixStack, AddNamePrefix);
                    ProcessAttributes(configuration, reader, prefixStack, AddAttributePair);

                    var preNodeType = reader.NodeType;
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                prefixStack.Push(reader.LocalName);

                                ProcessAttributes(configuration, reader, prefixStack, AddNamePrefix);
                                ProcessAttributes(configuration, reader, prefixStack, AddAttributePair);

                                // If current element is self-closing
                                if (reader.IsEmptyElement)
                                {
                                    prefixStack.Pop();
                                }
                                break;

                            case XmlNodeType.EndElement:
                                if (prefixStack.Any())
                                {
                                    // If this EndElement node comes right after an Element node,
                                    // it means there is no text/CDATA node in current element
                                    if (preNodeType == XmlNodeType.Element)
                                    {
                                        var key = ConfigurationPath.Combine(prefixStack.Reverse());

                                        configuration.AddValue(key, string.Empty);
                                    }

                                    prefixStack.Pop();
                                }
                                break;

                            case XmlNodeType.CDATA:
                            case XmlNodeType.Text:
                                {
                                    var key = ConfigurationPath.Combine(prefixStack.Reverse());
                                    var value = reader.Value;

                                    configuration.AddValue(key, value);
                                    break;
                                }
                            case XmlNodeType.XmlDeclaration:
                            case XmlNodeType.ProcessingInstruction:
                            case XmlNodeType.Comment:
                            case XmlNodeType.Whitespace:
                                // Ignore certain types of nodes
                                break;

                            default:
                                throw new FormatException($"Unsupported node of type {reader.NodeType} at {GetLineInfo(reader)}");
                        }

                        preNodeType = reader.NodeType;

                        // If this element is a self-closing element,
                        // we pretend that we just processed an EndElement node
                        // because a self-closing element contains an end within itself
                        if (preNodeType == XmlNodeType.Element &&
                            reader.IsEmptyElement)
                        {
                            preNodeType = XmlNodeType.EndElement;
                        }
                    }
                }
            }
        }

        private static void SkipUntilRootElement(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.XmlDeclaration &&
                    reader.NodeType != XmlNodeType.ProcessingInstruction)
                {
                    break;
                }
            }
        }

        private static string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo == null ? string.Empty :
                $"{lineInfo.LineNumber}, {lineInfo.LinePosition}";
        }

        private static void ProcessAttributes(
            IConfiguration configuration,
            XmlReader reader,
            Stack<string> prefixStack,
            Action<IConfiguration, XmlReader, Stack<string>, XmlWriter> act,
            XmlWriter writer = null)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException($"Namespace at {GetLineInfo(reader)} is not supported");
                }

                act(configuration, reader, prefixStack, writer);
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        // The special attribute "Name" only contributes to prefix
        // This method adds a prefix if current node in reader represents a "Name" attribute
        private static void AddNamePrefix(
            IConfiguration configuration,
            XmlReader reader,
            Stack<string> prefixStack,
            XmlWriter writer)
        {
            if (!string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If current element is not root element
            if (prefixStack.Any())
            {
                var lastPrefix = prefixStack.Pop();
                prefixStack.Push(ConfigurationPath.Combine(new[] { lastPrefix, reader.Value }));
            }
            else
            {
                prefixStack.Push(reader.Value);
            }
        }

        // Common attributes contribute to key-value pairs
        // This method adds a key-value pair if current node in reader represents a common attribute
        private static void AddAttributePair(
            IConfiguration configuration,
            XmlReader reader,
            Stack<string> prefixStack,
            XmlWriter writer)
        {
            prefixStack.Push(reader.LocalName);

            var key = ConfigurationPath.Combine(prefixStack.Reverse());
            var value = reader.Value;

            configuration.AddValue(key, value);

            prefixStack.Pop();
        }
    }
}