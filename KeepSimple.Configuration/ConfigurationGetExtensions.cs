using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace KeepSimple.Configuration
{
    public static class ConfigurationGetExtensions
    {
        #region Helper

        private static object CreateInstance(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsInterface)
                throw new InvalidOperationException($"Cannot create an instance of interface type \"{type}\".");
            if (typeInfo.IsAbstract)
                throw new InvalidOperationException($"Cannot create an instance of abstract type \"{type}\".");

            if (type.IsArray)
            {
                if (typeInfo.GetArrayRank() > 1)
                    throw new NotSupportedException($"Multidimensional array type \"{type}\" is not supported.");

                return Array.CreateInstance(typeInfo.GetElementType(), 0);
            }

            var hasDefaultConstructor = typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0);
            if (!hasDefaultConstructor)
                throw new InvalidOperationException($"There is no default constructor for type \"{type}\".");

            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create an instance of type \"{type}\".", ex);
            }
        }
        private static bool TryConvertValue(Type type, string value, out object result)
        {
            result = null;

            if (type == typeof(object))
            {
                result = value;
                return true;
            }

            if (type.GetTypeInfo().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                return TryConvertValue(Nullable.GetUnderlyingType(type), value, out result);
            }

            var converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
        private static Type FindOpenGenericInterface(Type expected, Type actual)
        {
            var actualTypeInfo = actual.GetTypeInfo();
            if (actualTypeInfo.IsGenericType &&
                actual.GetGenericTypeDefinition() == expected)
            {
                return actual;
            }

            var interfaces = actualTypeInfo.ImplementedInterfaces;
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.GetTypeInfo().IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == expected)
                {
                    return interfaceType;
                }
            }

            return null;
        }

        private static T BindValueType<T>(IConfiguration configuration, string path, T defaultValue)
        {
            var value = configuration.GetValue(path);
            if (TryConvertValue(typeof(T), value, out object result))
                return (T)result;

            return defaultValue;
        }

        private static T BindObjectType<T>(IConfiguration configuration, string path, T defaultValue = default(T))
        {
            var targetType = typeof(T);

            if (targetType.IsAbstract || targetType.IsInterface)
                return defaultValue;

            if (targetType.IsArray)
                return (T)BindArray(targetType, configuration.GetChildren(path));

            var dictionaryType = FindOpenGenericInterface(typeof(IDictionary<,>), targetType);
            if (dictionaryType != null)
                return (T)BindDictionary(targetType, configuration.GetChildren(path));

            var collectionType = FindOpenGenericInterface(typeof(ICollection<>), targetType);
            if (collectionType != null)
                return (T)BindCollection(targetType, configuration.GetChildren(path));

            var instance = CreateInstance(targetType);
            BindInstance(instance, targetType, configuration.GetChildren(path));

            return (T)instance;
        }

        private static void BindInstance<T>(T instance, Type targetType, IConfiguration configuration)
        {
            foreach (var property in targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.SetMethod == null || property.SetMethod.IsPrivate)
                    continue;

                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                {
                    var value = configuration.GetValue(property.Name);
                    if (TryConvertValue(property.PropertyType, value, out object convertedPropertyValue))
                        property.SetValue(instance, convertedPropertyValue);
                }
                else if (property.PropertyType.IsArray)
                {
                    var child = configuration.GetChildren(property.Name);
                    if (child != null)
                    {
                        var array = BindArray(property.PropertyType, child);
                        property.SetValue(instance, array);
                    }
                }
                else
                {
                    var propertyValueInstance = CreateInstance(property.PropertyType);
                    var child = configuration.GetChildren(property.Name);
                    if (child != null)
                    {
                        BindInstance(propertyValueInstance, property.PropertyType, child);
                        property.SetValue(instance, propertyValueInstance);
                    }
                }
            }
        }

        private static object BindArray(Type arrayType, IConfiguration configuration)
        {
            var elementType = arrayType.GetElementType();
            var isValueType = elementType.IsValueType || elementType == typeof(string);

            var arrayLength = isValueType ? configuration.Values.Count : configuration.Children.Count;

            var arrayInstance = Array.CreateInstance(elementType, arrayLength);
            if (arrayInstance.Length == 0)
                return arrayInstance;

            if (isValueType)
            {
                for (int arrayIndex = 0; arrayIndex < arrayInstance.Length; arrayIndex++)
                {
                    var value = configuration.GetValue(arrayIndex.ToString());
                    if (TryConvertValue(elementType, value, out object convertedValue))
                        arrayInstance.SetValue(convertedValue, arrayIndex);
                }
            }
            else
            {
                int arrayIndex = 0;
                foreach (var element in configuration.Children.Values)
                {
                    var elementInstance = CreateInstance(elementType);
                    BindInstance(elementInstance, elementType, element);

                    arrayInstance.SetValue(elementInstance, arrayIndex);

                    arrayIndex++;
                }
            }

            return arrayInstance;
        }

        private static object BindDictionary(Type dictionaryType, IConfiguration configuration)
        {
            var dictionaryTypeInfo = dictionaryType.GetTypeInfo();

            var keyType = dictionaryType.GenericTypeArguments[0];

            // support only for key type enum, value type or string
            if (!keyType.IsEnum && !keyType.IsValueType && keyType != typeof(string))
                return null;

            var valueType = dictionaryType.GenericTypeArguments[1];
            var isValueType = valueType.IsValueType || valueType == typeof(string);

            var dictionaryInstance = CreateInstance(dictionaryType);
            var setter = dictionaryTypeInfo.GetDeclaredProperty("Item");

            if (isValueType)
            {
                foreach (var v in configuration.Values)
                {
                    if (TryConvertValue(keyType, v.Key, out object convertedKey) &&
                        TryConvertValue(valueType, v.Value, out object convertedValue))
                    {
                        setter.SetValue(dictionaryInstance, convertedValue, new object[] { convertedKey });
                    }
                }
            }
            else
            {
                foreach (var c in configuration.Children)
                {
                    if (TryConvertValue(keyType, c.Key, out object convertedKey))
                    {
                        var elementInstance = CreateInstance(valueType);
                        BindInstance(elementInstance, valueType, c.Value);

                        setter.SetValue(dictionaryInstance, elementInstance, new object[] { convertedKey });
                    }
                }
            }

            return dictionaryInstance;
        }

        private static object BindCollection(Type collectionType, IConfiguration configuration)
        {
            var typeInfo = collectionType.GetTypeInfo();
            var itemType = typeInfo.GenericTypeArguments[0];
            var isValueType = itemType.IsValueType || itemType == typeof(string);

            var collectionInstance = CreateInstance(collectionType);
            var addMethod = typeInfo.GetDeclaredMethod("Add");

            if (isValueType)
            {
                foreach (var v in configuration.Values)
                {
                    if (TryConvertValue(itemType, v.Value, out object convertedValue))
                        addMethod.Invoke(collectionInstance, new[] { convertedValue });
                }
            }
            else
            {
                foreach (var element in configuration.Children.Values)
                {
                    var elementInstance = CreateInstance(itemType);
                    BindInstance(elementInstance, itemType, element);

                    addMethod.Invoke(collectionInstance, new[] { elementInstance });
                }
            }

            return collectionInstance;
        }

        #endregion

        #region Methods

        public static T Get<T>(this IConfiguration configuration, string path)
        {
            return Get(configuration, path, default(T));
        }

        public static T Get<T>(this IConfiguration configuration, string path, T defaultValue = default(T))
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var targetType = typeof(T);
            if (targetType.IsValueType || targetType == typeof(string))
                return BindValueType(configuration, path, defaultValue);
            else
                return BindObjectType(configuration, path, defaultValue);
        }

        #endregion
    }
}