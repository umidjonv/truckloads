﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Shared.Common.Dtos.Telegram;
using TL.Shared.Core.MessageBroker;

namespace TL.Module.Telegram.Worker.Jobs;

public class ParseMessageJob(IServiceScopeFactory serviceScopeFactory) : IParseMessageJob
{
    private static readonly ConcurrentQueue<TdApi.Message> NotSentMessages = [];

    public Task Invoke(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(DoWork(cancellationToken), Retry(cancellationToken));
    }

    private async Task Retry(CancellationToken cancellationToken)
    {
        if (!NotSentMessages.Any())
            return;

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ParseMessageJob>>();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration["ConvertMessageToJsonConsumer:ExchangeKey"];
        var routingKey = configuration["ConvertMessageToJsonConsumer:RoutingKey"];
        var queueKey = configuration["ConvertMessageToJsonConsumer:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] QueueKey is empty!");
            return;
        }
        
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        await Parallel.ForEachAsync(NotSentMessages, parallelOptions, async (_, token) =>
        {
            if (NotSentMessages.TryDequeue(out var message))
                try
                {
                    if (!(message.Content is TdApi.MessageContent.MessageText messageText))
                        return;
                    
                    var text = messageText.Text.Text;

                    if (string.IsNullOrWhiteSpace(text) || text.Length < 10)
                        return;

                    await rabbit.PublishAsync(exchangeKey, routingKey, queueKey,
                        new InsertMessageParams(message.ChatId, text), token);
                }
                catch (Exception e)
                {
                    logger.LogError("[{0}] Retry publish not sent massage is failed. Details: {1}",
                        nameof(ParseMessageJob),
                        e.Message);
                    NotSentMessages.Enqueue(message);
                }
        });
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ParseMessageJob>>();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration["ConvertMessageToJsonConsumer:ExchangeKey"];
        var routingKey = configuration["ConvertMessageToJsonConsumer:RoutingKey"];
        var queueKey = configuration["ConvertMessageToJsonConsumer:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError($"[{nameof(ParseMessageJob)}] QueueKey is empty!");
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var state = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);
        if (state.State is not TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters.AuthorizationStateReady)
        {
            logger.LogError("[{0}] Telegram is not in authorization state", nameof(ParseMessageJob));
            return;
        }

        var chats = await mediator.Send(new GetAllowedChatIdsParams(), cancellationToken);
        if (chats.ChatIds.Any())
        {
            logger.LogWarning("[{0}] No Telegram chats found", nameof(ParseMessageJob));
            return;
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        await Parallel.ForEachAsync(chats.ChatIds, parallelOptions, async (id, token) =>
        {
            var messages = await mediator.Send(new GetChatNewMessageParams<TdApi.Message>(id), cancellationToken);
            foreach (var message in messages.Messages)
                try
                {
                    if (!(message.Content is TdApi.MessageContent.MessageText messageText))
                        return;
                    var text = messageText.Text.Text;

                    if (string.IsNullOrWhiteSpace(text) || text.Length < 10)
                        return;

                    await rabbit.PublishAsync(exchangeKey, routingKey, queueKey,
                        new InsertMessageParams(message.ChatId, text), token);
                }
                catch (Exception e)
                {
                    NotSentMessages.Enqueue(message);
                    logger.LogError("[{0}] Publish massage is failed. Details: {1}", nameof(ParseMessageJob),
                        e.Message);
                }
        });
    }
}