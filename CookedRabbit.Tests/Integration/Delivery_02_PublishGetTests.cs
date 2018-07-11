﻿using CookedRabbit.Library.Models;
using CookedRabbit.Library.Services;
using CookedRabbit.Tests.Integration.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;
using static CookedRabbit.Library.Utilities.RandomData;

namespace CookedRabbit.Tests.Integration
{
    [Collection("IntegrationTests")]
    public class Delivery_02_PublishGetTests
    {
        private readonly IntegrationFixture _fixture;

        public Delivery_02_PublishGetTests(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("Rabbit Delivery", "PublishGet")]
        public async Task PublishAndGetAsync()
        {
            // Arrange
            var queueName = $"{_fixture.TestQueueName1}.2111";
            var exchangeName = string.Empty;
            var payload = await GetRandomByteArray(1000);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var publishSuccess = await _fixture.RabbitDeliveryService.PublishAsync(exchangeName, queueName, payload, false, null);
            var result = await _fixture.RabbitDeliveryService.GetAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.True(publishSuccess, "Message failed to publish.");
            Assert.True(result != null, "Result was null.");

            // Re-Act
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(deleteSuccess, "Queue was not deleted.");
        }

        [Fact]
        [Trait("Rabbit Delivery", "PublishGet")]
        public async Task PublishManyAndGetManyAsync()
        {
            // Arrange
            var messageCount = 17;
            var queueName = $"{_fixture.TestQueueName2}.2222";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messageCount);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var failures = await _fixture.RabbitDeliveryService.PublishManyAsync(exchangeName, queueName, payloads, false, null);
            var queueCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.Empty(failures);
            Assert.True(queueCount == messageCount, "Messages were lost in routing.");

            // Re-Act
            var results = await _fixture.RabbitDeliveryService.GetManyAsync(queueName, messageCount);
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(results.Count == messageCount);
            Assert.True(deleteSuccess, "Queue was not deleted.");
        }

        [Fact]
        [Trait("Rabbit Delivery", "PublishGet")]
        public async Task PublishManyAsBatchesAsync()
        {
            // Arrange
            var messageCount = 111;
            var queueName = $"{_fixture.TestQueueName3}.2333";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messageCount);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var failures = await _fixture.RabbitDeliveryService.PublishManyAsBatchesAsync(exchangeName, queueName, payloads, 7, false, null);
            var queueCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.Empty(failures);
            Assert.True(queueCount == messageCount, "Messages were lost in routing.");

            // Re-Act
            var results = await _fixture.RabbitDeliveryService.GetAllAsync(queueName);
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(results.Count == messageCount);
            Assert.True(deleteSuccess, "Queue was not deleted.");
        }

        [Fact]
        [Trait("Rabbit Delivery", "PublishGet")]
        public async Task PublishManyAsBatchesInParallelAsync()
        {
            // Arrange
            var messageCount = 100;
            var queueName = $"{_fixture.TestQueueName4}.2444";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messageCount);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            await _fixture.RabbitDeliveryService.PublishManyAsBatchesInParallelAsync(exchangeName, queueName, payloads, 10, false, null);
            var queueCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.True(queueCount == messageCount, "Message were lost in routing.");

            // Re-Act
            var results = await _fixture.RabbitDeliveryService.GetAllAsync(queueName);
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(results.Count == messageCount);
            Assert.True(deleteSuccess, "Queue was not deleted.");
        }
    }
}
