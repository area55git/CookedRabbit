﻿using System.Threading.Tasks;
using Utf8Json;
using ZeroFormatter;
using static CookedRabbit.Library.Utilities.Enums;

namespace CookedRabbit.Library.Utilities
{
    /// <summary>
    /// CookedRabbit Utility class to help serialize objects.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Take an object of type T and serialize to a byte[].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message to be serialized.</param>
        /// <param name="serializationMethod">Specifies the serialization method.</param>
        /// <returns></returns>
        public static async Task<byte[]> SerializeAsync<T>(T message, SerializationMethod serializationMethod)
        {
            byte[] output = null;

            switch (serializationMethod)
            {
                case SerializationMethod.Utf8Json:
                    output = await SerializeAsUtf8JsonFormatAsync(message);
                    break;
                case SerializationMethod.ZeroFormat:
                    output = await SerializeAsZeroFormatAsync(message);
                    break;
                default: break;
            }

            return output;
        }

        /// <summary>
        /// Deserialize byte[] into an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="serializationMethod"></param>
        /// <returns></returns>
        public static async Task<T> DeserializeAsync<T>(byte[] input, SerializationMethod serializationMethod)
        {
            switch (serializationMethod)
            {
                case SerializationMethod.Utf8Json:
                    return await DeserializeAsUtf8JsonFormatAsync<T>(input);
                case SerializationMethod.ZeroFormat:
                    return await DeserializeAsZeroFormatAsync<T>(input);
                default:
                    return default;
            }
        }

        /// <summary>
        /// Serialize an object that is ZeroFormattable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <exception cref="System.InvalidOperationException">Thrown when object T is not ZeroFormattable.</exception>
        /// <returns>A byte[]</returns>
        public static async Task<byte[]> SerializeAsZeroFormatAsync<T>(T message)
        {
            return await Task.Run(() => ZeroFormatterSerializer.Serialize(message));
        }

        /// <summary>
        /// Deserialize an object that is ZeroFormattable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <exception cref="System.InvalidOperationException">Thrown when object T is not ZeroFormattable.</exception>
        /// <returns>An object of type T</returns>
        public static async Task<T> DeserializeAsZeroFormatAsync<T>(byte[] input)
        {
            return await Task.Run(() => ZeroFormatterSerializer.Deserialize<T>(input));
        }

        /// <summary>
        /// Serialize an object with Utf8Json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns>A byte[]</returns>
        public static async Task<byte[]> SerializeAsUtf8JsonFormatAsync<T>(T message)
        {
            return await Task.Run(() => JsonSerializer.Serialize(message));
        }

        /// <summary>
        /// Serialize an object to a string with Utf8Json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns>A json string.</returns>
        public static async Task<string> SerializeToJsonAsync<T>(T message, bool makePretty = false)
        {
            return await Task.Run(() =>
            {
                var json = JsonSerializer.ToJsonString(message);

                if (makePretty) { json = JsonSerializer.PrettyPrint(json); }

                return json;
            });
        }

        /// <summary>
        /// Deserialize an object with Utf8Json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns>An object of type T</returns>
        public static async Task<T> DeserializeAsUtf8JsonFormatAsync<T>(byte[] input)
        {
            return await Task.Run(() => JsonSerializer.Deserialize<T>(input));
        }

        /// <summary>
        /// Deserialize a json to an object with Utf8Json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns>An object of type T</returns>
        public static async Task<T> DeserializeJsonAsync<T>(string json)
        {
            return await Task.Run(() => JsonSerializer.Deserialize<T>(json));
        }
    }
}
