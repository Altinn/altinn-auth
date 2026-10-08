using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Altinn.Authorization.Cli.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CommunityToolkit.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.ServiceBus;

/// <summary>
/// Command for retrying failed messages.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RetryCommand(CancellationToken cancellationToken)
    : BaseCommand<RetryCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var adminClient = new ServiceBusAdministrationClient(settings.ConnectionString);

        var queueName = settings.QueueName;
        if (string.IsNullOrEmpty(queueName))
        {
            queueName = await SelectQueue(adminClient, settings, cancellationToken);
        }

        var errorQueueName = $"{queueName}_error";
        var queue = await GetQueueInto(adminClient, queueName, cancellationToken);
        var errorQueue = await GetQueueInto(adminClient, errorQueueName, cancellationToken);

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async (ctx) =>
            {
                var client = new ServiceBusClient(settings.ConnectionString);

                // first, deadletter all messages (prevents infinite looping when messages returns to the error queue)
                await DeadLetterMessages(client, errorQueue, ctx, cancellationToken);

                // then, move all messages from the DLQ to the main queue
                await using var sender = client.CreateSender(queueName);
                await MoveMessages(client, errorQueue, sender, ctx, cancellationToken);
            });

        return 0;
    }

    private async Task DeadLetterMessages(ServiceBusClient client, QueueRuntimeProperties from, ProgressContext ctx, CancellationToken cancellationToken)
    {
        var task = ctx.AddTask($"Dead-letter {from.Name} messages", autoStart: true, maxValue: from.ActiveMessageCount);
        await using var receiver = client.CreateReceiver(from.Name, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = 100,
            SubQueue = SubQueue.None,
        });

        await foreach (var received in ReceiveMessages(receiver, cancellationToken))
        {
            await receiver.DeadLetterMessageAsync(received, cancellationToken: cancellationToken);
            task.Increment(1);
        }

        task.Value = task.MaxValue;
        task.StopTask();
        ctx.Refresh();
    }

    private async Task MoveMessages(ServiceBusClient client, QueueRuntimeProperties from, ServiceBusSender to, ProgressContext ctx, CancellationToken cancellationToken)
    {
        var task = ctx.AddTask($"Move {from.Name} DLQ messages", autoStart: true, maxValue: from.DeadLetterMessageCount + from.ActiveMessageCount);
        await using var receiver = client.CreateReceiver(from.Name, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = 100,
            SubQueue = SubQueue.DeadLetter,
        });

        await MoveMessages(receiver, to, task, cancellationToken);
        task.Value = task.MaxValue;
        task.StopTask();
        ctx.Refresh();
    }

    private async Task MoveMessages(ServiceBusReceiver receiver, ServiceBusSender sender, ProgressTask task, CancellationToken cancellationToken)
    {
        await foreach (var received in ReceiveMessages(receiver, cancellationToken))
        {
            var toSend = new ServiceBusMessage(received);
            ClearMassTransitFaultProperties(toSend);

            await sender.SendMessageAsync(toSend, cancellationToken);
            await receiver.CompleteMessageAsync(received, cancellationToken);
            task.Increment(1);
        }
    }

    private static void ClearMassTransitFaultProperties(ServiceBusMessage message)
    {
        message.ApplicationProperties.Remove("MT-Reason");

        foreach (var key in message.ApplicationProperties.Keys.ToList())
        {
            if (key.StartsWith("MT-Fault-"))
            {
                message.ApplicationProperties.Remove(key);
            }
        }
    }

    private async IAsyncEnumerable<ServiceBusReceivedMessage> ReceiveMessages(ServiceBusReceiver receiver, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            var messages = await receiver.ReceiveMessagesAsync(receiver.PrefetchCount, maxWaitTime: TimeSpan.FromSeconds(5), cancellationToken);
            if (messages.Count == 0)
            {
                break;
            }

            foreach (var message in messages)
            {
                yield return message;
            }
        }
    }

    private async Task<QueueRuntimeProperties> GetQueueInto(ServiceBusAdministrationClient client, string queueName, CancellationToken cancellationToken)
    {
        var result = await client.GetQueueRuntimePropertiesAsync(queueName, cancellationToken)
            .LogOnFailure($"[bold red]Failed to get queue info for '{queueName}'");

        return result.Value;
    }

    private async Task<string> SelectQueue(ServiceBusAdministrationClient client, Settings settings, CancellationToken cancellationToken)
    {
        var queues = new Dictionary<string, QueueInfo>();
        await foreach (var queueProperties in client.GetQueuesRuntimePropertiesAsync(cancellationToken))
        {
            var name = queueProperties.Name;
            var queueType = QueueType.Normal;

            if (IsErrorQueue(name, out var baseName))
            {
                name = baseName;
                queueType = QueueType.Error;
            }

            ref var queue = ref CollectionsMarshal.GetValueRefOrAddDefault(queues, name, out var existed);
            if (!existed)
            {
                queue = new(name);
            }

            queue![queueType] = queueProperties;
        }

        var select = new SelectionPrompt<QueueInfo>()
            .Title("Select queue to retry messages from")
            .UseConverter(static queue =>
            {
                var errorColumn = queue.ErrorProperties switch
                {
                    null => "[green]0[/]",
                    { } props => $"[red]{props.TotalMessageCount}[/]",
                };

                return $"{queue.Name} ({errorColumn})";
            })
            .AddChoices(queues.Values);

        var selected = await select.ShowAsync(AnsiConsole.Console, cancellationToken);

        return selected.Name;
    }

    private static bool IsErrorQueue(string queueName, [NotNullWhen(true)] out string? baseQueueName)
    {
        if (queueName.EndsWith("_error"))
        {
            baseQueueName = queueName[..^"_error".Length];
            return true;
        }

        baseQueueName = null;
        return false;
    }

    private enum QueueType
    {
        Normal,
        Error,
    }

    private sealed class QueueInfo(string name)
    {
        public string Name { get; } = name;

        public QueueRuntimeProperties? Properties { get; private set; }

        public QueueRuntimeProperties? ErrorProperties { get; private set; }

        public QueueRuntimeProperties this[QueueType type]
        {
            set
            {
                switch (type)
                {
                    case QueueType.Normal:
                        Properties = value;
                        break;

                    case QueueType.Error:
                        ErrorProperties = value;
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(type));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Settings for the retry command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Gets the connection string to the source database.
        /// </summary>
        [Description("The connection string to the service-bus.")]
        [CommandArgument(0, "<CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? ConnectionString { get; init; }

        /// <summary>
        /// Gets the name of the queue to retry messages from.  s
        /// </summary>
        [Description("The name of the queue to retry messages from.")]
        [CommandArgument(1, "[QUEUE_NAME]")]
        [ExpandEnvironmentVariables]
        public string? QueueName { get; init; }
    }
}
