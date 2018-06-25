﻿using CookedRabbit.Library.Models;
using CookedRabbit.Library.Pools;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CookedRabbit.Library.Services
{
    public class RabbitService : IRabbitService, IDisposable
    {
        private readonly RabbitChannelPool _rcp = null;
        private readonly RabbitSeasoning _originalRabbitSeasoning = null; // Used if for recovery later.

        public RabbitService(RabbitSeasoning rabbitSeasoning)
        {
            _originalRabbitSeasoning = rabbitSeasoning;
            _rcp = RabbitChannelPool.CreateRabbitChannelPoolAsync(rabbitSeasoning).GetAwaiter().GetResult();
        }

        #region BasicPublish Section

        public async Task<bool> PublishAsync(string exchangeName, string queueName, byte[] payload)
        {
            var success = false;
            var channelPair = await _rcp.GetPooledChannelPairAsync().ConfigureAwait(false);

            try
            {
                channelPair.Channel.BasicPublish(exchange: exchangeName,
                                     routingKey: queueName,
                                     false,
                                     basicProperties: null,
                                     body: payload);

                success = true;
            }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            _rcp.ReturnChannelToPool(channelPair);

            return success;
        }

        public async Task<List<int>> PublishManyAsync(string exchangeName, string queueName, List<byte[]> payloads)
        {
            var failures = new List<int>();
            var channelPair = await _rcp.GetPooledChannelPairAsync().ConfigureAwait(false);
            var rand = new Random();
            var count = 0;

            foreach (var payload in payloads)
            {
                try
                {
                    channelPair.Channel.BasicPublish(exchange: exchangeName,
                                         routingKey: queueName,
                                         false,
                                         basicProperties: null,
                                         body: payload);

                    await Task.Delay(rand.Next(0, 1));
                }
                catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
                {
                    failures.Add(count);
                    _rcp.FlagDeadChannel(channelPair.ChannelId);
                    await Console.Out.WriteLineAsync(ace.Demystify().Message);
                }
                catch (Exception e)
                {
                    failures.Add(count);
                    await Console.Out.WriteLineAsync(e.Demystify().Message);
                }

                count++;
            }

            _rcp.ReturnChannelToPool(channelPair);

            return failures;
        }

        public async Task<List<int>> PublishManyAsBatchesAsync(string exchangeName, string queueName, List<byte[]> payloads, ushort batchSize = 100)
        {
            var failures = new List<int>();
            var channelPair = await _rcp.GetPooledChannelPairAsync().ConfigureAwait(false);
            var rand = new Random();
            var count = 0;

            while (payloads.Any())
            {
                var processingPayloads = payloads.Take(batchSize);
                payloads.RemoveRange(0, payloads.Count > batchSize ? batchSize : payloads.Count);

                foreach (var payload in processingPayloads)
                {
                    try
                    {
                        channelPair.Channel.BasicPublish(exchange: exchangeName,
                                             routingKey: queueName,
                                             false,
                                             basicProperties: null,
                                             body: payload);
                    }
                    catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
                    {
                        failures.Add(count);
                        _rcp.FlagDeadChannel(channelPair.ChannelId);
                        await Console.Out.WriteLineAsync(ace.Demystify().Message);
                    }
                    catch (Exception e)
                    {
                        failures.Add(count);
                        await Console.Out.WriteLineAsync(e.Demystify().Message);
                    }

                    count++;
                }

                await Task.Delay(rand.Next(0, 2));
            }

            _rcp.ReturnChannelToPool(channelPair);

            return failures;
        }

        #endregion

        #region BasicGet Section

        public async Task<BasicGetResult> GetAsync(string queueName)
        {
            var channelPair = await _rcp.GetPooledChannelPairAsync().ConfigureAwait(false);

            BasicGetResult result = null;

            try
            { result = channelPair.Channel.BasicGet(queue: queueName, autoAck: true); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            _rcp.ReturnChannelToPool(channelPair);

            return result;
        }

        public async Task<List<BasicGetResult>> GetManyAsync(string queueName, int batchCount)
        {
            var channelPair = await _rcp.GetPooledChannelPairAsync().ConfigureAwait(false);
            var queueCount = 0U;

            try { queueCount = channelPair.Channel.MessageCount(queueName); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            int resultCount = 0;
            var results = new List<BasicGetResult>();

            if (queueCount != 0)
            {
                while (queueCount > 0 && resultCount < batchCount)
                {
                    try
                    {
                        var result = channelPair.Channel.BasicGet(queue: queueName, autoAck: true);
                        if (result == null) //Empty Queue
                        { break; }

                        results.Add(result);
                        resultCount++;
                    }
                    catch (Exception e)
                    { await Console.Out.WriteLineAsync(e.Demystify().Message); }
                }
            }

            _rcp.ReturnChannelToPool(channelPair);

            return results;
        }

        #endregion

        #region BasicGet With Manual Ack Section

        public async Task<(IModel Channel, BasicGetResult Result)> GetWithManualAckAsync(string queueName)
        {
            var channelPair = await _rcp.GetPooledChannelPairAckableAsync().ConfigureAwait(false); ;

            BasicGetResult result = null;

            try
            { result = channelPair.Channel.BasicGet(queue: queueName, autoAck: false); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            _rcp.ReturnChannelToAckPool(channelPair);

            return (channelPair.Channel, result);
        }

        public async Task<(IModel ChannelId, List<BasicGetResult> Results)> GetManyWithManualAckAsync(string queueName, int batchCount)
        {
            var channelPair = await _rcp.GetPooledChannelPairAckableAsync().ConfigureAwait(false);
            var queueCount = 0U;
            var resultCount = 0;
            var results = new List<BasicGetResult>();

            try { queueCount = channelPair.Channel.MessageCount(queueName); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            if (queueCount != 0)
            {
                while (queueCount > 0 && resultCount < batchCount)
                {
                    try
                    {
                        var result = channelPair.Channel.BasicGet(queue: queueName, autoAck: false);
                        if (result == null) //Empty Queue
                        { break; }

                        results.Add(result);
                        resultCount++;
                    }
                    catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
                    {
                        _rcp.FlagDeadChannel(channelPair.ChannelId);
                        await Console.Out.WriteLineAsync(ace.Demystify().Message);
                        break;
                    }
                    catch (Exception e)
                    { await Console.Out.WriteLineAsync(e.Demystify().Message); }
                }
            }

            _rcp.ReturnChannelToAckPool(channelPair);

            return (channelPair.Channel, results);
        }

        public async Task<AckableResult> GetAckableAsync(string queueName)
        {
            var channelPair = await _rcp.GetPooledChannelPairAckableAsync().ConfigureAwait(false); ;

            BasicGetResult result = null;

            try
            { result = channelPair.Channel.BasicGet(queue: queueName, autoAck: false); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            _rcp.ReturnChannelToAckPool(channelPair);

            return new AckableResult { Channel = channelPair.Channel, Results = new List<BasicGetResult>() { result } };
        }

        public async Task<AckableResult> GetManyAckableAsync(string queueName, int batchCount)
        {
            var channelPair = await _rcp.GetPooledChannelPairAckableAsync().ConfigureAwait(false); ;
            var queueCount = 0U;
            var resultCount = 0;
            var results = new List<BasicGetResult>();

            try { queueCount = channelPair.Channel.MessageCount(queueName); }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
            {
                _rcp.FlagDeadChannel(channelPair.ChannelId);
                await Console.Out.WriteLineAsync(ace.Demystify().Message);
            }
            catch (Exception e)
            { await Console.Out.WriteLineAsync(e.Demystify().Message); }

            if (queueCount != 0)
            {
                while (queueCount > 0 && resultCount < batchCount)
                {
                    try
                    {
                        var result = channelPair.Channel.BasicGet(queue: queueName, autoAck: false);
                        if (result == null) //Empty Queue
                        { break; }

                        results.Add(result);
                        resultCount++;
                    }
                    catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ace)
                    {
                        _rcp.FlagDeadChannel(channelPair.ChannelId);
                        await Console.Out.WriteLineAsync(ace.Demystify().Message);
                        break;
                    }
                    catch (Exception e)
                    { await Console.Out.WriteLineAsync(e.Demystify().Message); }
                }
            }

            _rcp.ReturnChannelToAckPool(channelPair);

            return new AckableResult { Channel = channelPair.Channel, Results = results };
        }

        #endregion

        #region Consumer Section

        public async Task<EventingBasicConsumer> CreateConsumerAsync(
            Action<object, BasicDeliverEventArgs> ActionWork,
            string queueName,
            ushort prefetchCount = 120,
            bool autoAck = false)
        {
            if (!_originalRabbitSeasoning.EnableDispatchConsumersAsync)
            { throw new ArgumentException("EnableDispatchConsumerAsync is set to true, set it to false to get an regular Consumer."); }

            var channel = await _rcp.GetTransientChannelAsync(enableAck: true);
            if (channel is null) throw new Exception("Channel was unable to be created for this consumer.");

            var consumer = new EventingBasicConsumer(channel);
            channel.BasicQos(_originalRabbitSeasoning.QosPrefetchSize, _originalRabbitSeasoning.QosPrefetchCount, false);
            consumer.Received += (model, ea) => ActionWork(model, ea);
            channel.BasicConsume(queue: queueName,
                                 autoAck: autoAck,
                                 consumer: consumer);

            return consumer;
        }

        public async Task<AsyncEventingBasicConsumer> CreateAsynchronousConsumerAsync(
            Func<object, BasicDeliverEventArgs, Task> AsyncWork,
            string queueName,
            ushort prefetchCount = 120,
            bool autoAck = false)
        {
            if (!_originalRabbitSeasoning.EnableDispatchConsumersAsync)
            { throw new ArgumentException("EnableDispatchConsumerAsync is set to false, set it to true to get an AsyncConsumer."); }

            var channel = await _rcp.GetTransientChannelAsync(enableAck: true);
            if (channel is null) throw new Exception("Channel was unable to be created for this consumer.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            channel.BasicQos(_originalRabbitSeasoning.QosPrefetchSize, _originalRabbitSeasoning.QosPrefetchCount, false);
            consumer.Received += (model, ea) => AsyncWork(model, ea);
            channel.BasicConsume(queue: queueName,
                                 autoAck: autoAck,
                                 consumer: consumer);

            return consumer;
        }

        #endregion

        #region Dispose

        private bool _disposedValue = false;

        public virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) { _rcp.Dispose(true); }

                _disposedValue = true;
            }
        }

        void IDisposable.Dispose() { Dispose(true); }

        #endregion
    }
}
