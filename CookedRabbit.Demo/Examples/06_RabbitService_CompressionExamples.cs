﻿using CookedRabbit.Demo.Helpers;
using CookedRabbit.Library.Models;
using CookedRabbit.Library.Services;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using static CookedRabbit.Library.Utilities.Enums;
using static CookedRabbit.Library.Utilities.RandomData;
using static CookedRabbit.Demo.DemoHelper;

namespace CookedRabbit.Demo
{
    public class RabbitServiceCompressionExamples
    {
        #region RabbitService Setup

        private static readonly RabbitSeasoning _rabbitSeasoning = new RabbitSeasoning();
        private static RabbitSerializeService _rabbitSerializeService;
        private static TestObject testObject = new TestObject();
        private static string _equalityCheck = string.Empty;

        #endregion

        #region RabbitService Compress and Decompress Test

        public static async Task RunRabbitServiceCompressAndDecompressTestAsync()
        {
            _rabbitSeasoning.FactorySettings.EnableDispatchConsumersAsync = false;
            _rabbitSeasoning.SerializeSettings.CompressionMethod = CompressionMethod.Gzip;
            _rabbitSerializeService = new RabbitSerializeService(_rabbitSeasoning);

            await RabbitService_SendMessageAsync();
            await RabbitService_ReceiveMessageAsync();
            await Console.Out.WriteLineAsync("Finished sending messages.");
        }

        public static async Task RabbitService_SendMessageAsync()
        {
            var payload = await GetAnObjectAsBytes();
            var envelope = new Envelope
            {
                ExchangeName = exchangeName,
                RoutingKey = queueName,
                MessageBody = payload,
                MessageType = $"{ContentType.Json.Description()} {Charset.Utf8.Description()}"
            };

            await _rabbitSerializeService.CompressAndPublishAsync(envelope);
        }

        public static async Task RabbitService_ReceiveMessageAsync()
        {
            var result = await _rabbitSerializeService.GetAndDecompressAsync(queueName);

            var jsonResult = Encoding.UTF8.GetString(result);
            var receivedObject = JsonConvert.DeserializeObject<TestObject>(jsonResult);
            var equal = _equalityCheck == jsonResult;
            await Console.Out.WriteLineAsync($"Compressed message received. IsEqualToOriginal? {equal}");
        }

        private static async Task<byte[]> GetAnObjectAsBytes()
        {
            testObject.FirstName = "House";
            testObject.LastName = "Cat";
            testObject.InnerObject = new InnerObject { Test1 = "Test1", Test2 = "Test2" };
            testObject.RandoString = Encoding.UTF8.GetString(await GetRandomByteArray(1000));

            _equalityCheck = JsonConvert.SerializeObject(testObject);
            return Encoding.UTF8.GetBytes(_equalityCheck);
        }

        #endregion
    }
}
