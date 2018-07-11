﻿using CookedRabbit.Tests.Integration.Fixtures;
using System.Threading.Tasks;
using Xunit;
using static CookedRabbit.Library.Utilities.RandomData;

namespace CookedRabbit.Tests.Integration
{
    [Collection("IntegrationTests")]
    public class Delivery_01_PublishTests
    {
        private readonly IntegrationFixture _fixture;

        public Delivery_01_PublishTests(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("Rabbit Delivery", "Publish")]
        public async Task PublishAsync()
        {
            // Arrange
            var queueName = $"{_fixture.TestQueueName1}.1111";
            var exchangeName = string.Empty;
            var payload = await GetRandomByteArray(1000);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var publishSuccess = await _fixture.RabbitDeliveryService.PublishAsync(exchangeName, queueName, payload, false, null);
            var messageCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.True(publishSuccess, "Message failed to publish.");
            Assert.True(messageCount > 0, "Message was lost in routing.");

            // Re-Act
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(deleteSuccess);
        }

        [Fact]
        [Trait("Rabbit Delivery", "Publish")]
        public async Task PublishManyAsync()
        {
            // Arrange
            var messagesToSend = 17;
            var queueName = $"{_fixture.TestQueueName2}.1112";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messagesToSend);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var failures = await _fixture.RabbitDeliveryService.PublishManyAsync(exchangeName, queueName, payloads, false, null);
            var messageCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.Empty(failures);
            Assert.True(messageCount == messagesToSend, "Messages were lost in routing.");

            // Re-Act
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(deleteSuccess);
        }

        [Fact]
        [Trait("Rabbit Delivery", "Publish")]
        public async Task PublishManyAsBatchesAsync()
        {
            // Arrange
            var messagesToSend = 99;
            var queueName = $"{_fixture.TestQueueName3}.1113";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messagesToSend);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            var failures = await _fixture.RabbitDeliveryService.PublishManyAsBatchesAsync(exchangeName, queueName, payloads, 15, false, null);
            var messageCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.Empty(failures);
            Assert.True(messageCount == messagesToSend, "Messages were lost in routing.");

            // Re-Act
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(deleteSuccess, "Queue was not deleted.");
        }

        [Fact]
        [Trait("Rabbit Delivery", "Publish")]
        public async Task PublishManyAsBatchesInParallelAsync()
        {
            // Arrange
            var messagesToSend = 99;
            var queueName = $"{_fixture.TestQueueName4}.1114";
            var exchangeName = string.Empty;
            var payloads = await CreatePayloadsAsync(messagesToSend);

            // Act
            var createSuccess = await _fixture.RabbitTopologyService.QueueDeclareAsync(queueName);
            await _fixture.RabbitDeliveryService.PublishManyAsBatchesInParallelAsync(exchangeName, queueName, payloads, 10, false, null);
            var messageCount = await _fixture.RabbitDeliveryService.GetMessageCountAsync(queueName);

            // Assert
            Assert.True(createSuccess, "Queue was not created.");
            Assert.True(messageCount == messagesToSend, "Messages were lost in routing.");

            // Re-Act
            var deleteSuccess = await _fixture.RabbitTopologyService.QueueDeleteAsync(queueName);

            // Re-Assert
            Assert.True(deleteSuccess);
        }
    }
}
