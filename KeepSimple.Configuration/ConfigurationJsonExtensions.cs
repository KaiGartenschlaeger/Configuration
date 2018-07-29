using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace KeepSimple.Configuration
{
    public static class ConfigurationJsonExtensions
    {
        public static ConfigurationBuilder AddJsonFile(this ConfigurationBuilder builder, string filePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddJsonFileInternal(builder.Configuration, filePath);

            return builder;
        }

        public static void AddJsonFile(this IConfiguration configuration, string filePath)
        {
            AddJsonFileInternal(configuration, filePath);
        }

        private static void AddJsonFileInternal(IConfiguration configuration, string filePath)
        {
            var jsonPath = string.Empty;
            var context = new Stack<string>();

            using (var reader = new JsonTextReader(new StreamReader(filePath)))
            {
                reader.DateParseHandling = DateParseHandling.None;
                var jsonConfig = JObject.Load(reader);

                VisitJObject(configuration, context, jsonConfig, ref jsonPath);
            }
        }

        private static void VisitJObject(IConfiguration configuration, Stack<string> context, JObject jObject, ref string jsonPath)
        {
            foreach (var property in jObject.Properties())
            {
                EnterContext(configuration, context, property.Name, ref jsonPath);
                VisitProperty(configuration, context, property, ref jsonPath);
                ExitContext(configuration, context, ref jsonPath);
            }
        }

        private static void EnterContext(IConfiguration configuration, Stack<string> context, string propertyName, ref string jsonPath)
        {
            context.Push(propertyName);
            jsonPath = ConfigurationPath.Combine(context.Reverse());
        }

        private static void VisitProperty(IConfiguration configuration, Stack<string> context, JProperty property, ref string jsonPath)
        {
            VisitToken(configuration, context, property.Value, ref jsonPath);
        }

        private static void VisitToken(IConfiguration configuration, Stack<string> context, JToken token, ref string jsonPath)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    VisitJObject(configuration, context, token.Value<JObject>(), ref jsonPath);
                    break;

                case JTokenType.Array:
                    VisitArray(configuration, context, token.Value<JArray>(), ref jsonPath);
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                case JTokenType.Null:
                    VisitPrimitive(configuration, token.Value<JValue>(), ref jsonPath);
                    break;

                default:
                    throw new FormatException($"The json token of type {token.Type} is not supported yet.");
            }
        }

        private static void VisitArray(IConfiguration configuration, Stack<string> context, JArray array, ref string jsonPath)
        {
            for (int index = 0; index < array.Count; index++)
            {
                EnterContext(configuration, context, index.ToString(), ref jsonPath);
                VisitToken(configuration, context, array[index], ref jsonPath);
                ExitContext(configuration, context, ref jsonPath);
            }
        }

        private static void VisitPrimitive(IConfiguration configuration, JValue data, ref string jsonPath)
        {
            var key = jsonPath;
            var value = data.ToString(CultureInfo.InvariantCulture);

            configuration.AddValue(key, value);
        }

        private static void ExitContext(IConfiguration configuration, Stack<string> context, ref string jsonPath)
        {
            context.Pop();
            jsonPath = ConfigurationPath.Combine(context.Reverse());
        }
    }
}