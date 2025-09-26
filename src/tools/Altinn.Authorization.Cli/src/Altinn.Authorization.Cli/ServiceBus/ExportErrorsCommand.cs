using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Altinn.Authorization.Cli.ErrorDb;
using Altinn.Authorization.Cli.ServiceBus.Utils;
using Altinn.Authorization.Cli.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.ServiceBus;

/// <summary>
/// Command for exporting errors from the error queues.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExportErrorsCommand(CancellationToken cancellationToken)
    : BaseCommand<ExportErrorsCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        using var mongo = await ErrorDbHelper.GetClient(cancellationToken);
        var sb = ServiceBusHandle.Create(settings.ConnectionString);

        var props = (await sb.AdministrationClient.GetNamespacePropertiesAsync(cancellationToken)).Value;
        var db = mongo.GetDatabase(props.Name);

        var queues = new Dictionary<string, QueueInfo>(StringComparer.OrdinalIgnoreCase);
        await foreach (var serverQueue in sb.AdministrationClient.GetQueuesRuntimePropertiesAsync(cancellationToken))
        {
            var name = serverQueue.Name;
            var kind = QueueKind.Normal;

            if (name.EndsWith("_error"))
            {
                name = name[..^6]; // Remove "_error" suffix
                kind = QueueKind.Error;
            }

            ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(queues, name, out var exists);
            if (!exists)
            {
                info = new QueueInfo { Name = name };
            }

            info.Add(kind, serverQueue);
        }

        var filter = Builders<BsonDocument>.Filter.Eq(doc => doc["_id"], "5");

        await AnsiConsole.Progress()
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async ctx =>
            {
                var tasks = new List<Task>();
                foreach (var queue in queues.Values)
                {
                    var session = await mongo.StartSessionAsync(new() { }, cancellationToken);

                    Task clearCollectionTask = db.DropCollectionAsync(session, queue.Name, cancellationToken);
                    tasks.Add(clearCollectionTask);

                    Task? extractNormalDql = null;
                    Task? dlqErrorActive = null;
                    Task? extractErrorDlq = null;

                    {
                        if (queue.Normal is { DeadLetterMessageCount: > 0 } normalQueue)
                        {
                            extractNormalDql = CreateExtractDlqTask(ctx, session, sb, db, dependsOn: clearCollectionTask, name: queue.Name, queue: normalQueue.Name, cancellationToken);
                            tasks.Add(extractNormalDql);
                        }
                    }

                    {
                        if (queue.Error is { ActiveMessageCount: > 0 } errorQueue)
                        {
                            dlqErrorActive = CreateMoveToDlqTask(ctx, session, sb, db, dependsOn: clearCollectionTask, name: queue.Name, queue: errorQueue.Name, cancellationToken);
                            tasks.Add(dlqErrorActive);

                            extractErrorDlq = CreateExtractDlqTask(ctx, session, sb, db, dependsOn: dlqErrorActive, name: queue.Name, queue: errorQueue.Name, cancellationToken);
                            tasks.Add(extractErrorDlq);
                        }
                    }

                    {
                        if (dlqErrorActive is null && queue.Error is { DeadLetterMessageCount: > 0 } errorQueue)
                        {
                            extractErrorDlq = CreateExtractDlqTask(ctx, session, sb, db, dependsOn: clearCollectionTask, name: queue.Name, queue: errorQueue.Name, cancellationToken);
                            tasks.Add(extractErrorDlq);
                        }
                    }

                    tasks.Add(DisposeAfter(session, [clearCollectionTask, extractNormalDql, dlqErrorActive, extractErrorDlq], cancellationToken));
                }

                await Task.WhenAll(tasks);
            });

        return 0;
    }

    private static async Task CreateExtractDlqTask(
        ProgressContext ctx,
        IClientSessionHandle session,
        ServiceBusHandle sb,
        IMongoDatabase db,
        Task? dependsOn,
        string name,
        string queue,
        CancellationToken cancellationToken)
    {
        var task = ctx.AddTask($"Extract '{name}' DLQ", autoStart: false);

        if (dependsOn is not null)
        {
            await dependsOn.WaitAsync(cancellationToken);
        }

        var stats = (await sb.AdministrationClient.GetQueueRuntimePropertiesAsync(queue, cancellationToken)).Value;
        if (stats.DeadLetterMessageCount <= 0)
        {
            task.MaxValue = 1;
            task.Value = 1;
            task.StartTask();
            task.StopTask();
            return;
        }

        task.MaxValue = stats.DeadLetterMessageCount;
        task.Value = 0;
        task.StartTask();

        await using var receiver = sb.Client.CreateReceiver(
            queue,
            new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = Math.Clamp((int)stats.DeadLetterMessageCount, 100, 10_000),
                SubQueue = SubQueue.DeadLetter,
            });

        await db.CreateCollectionAsync(session, name, cancellationToken: cancellationToken);
        var collection = db.GetCollection<BsonDocument>(name);

        var channel = Channel.CreateBounded<ServiceBusReceivedMessage>(100);
        var reader = channel.Reader;
        var writer = channel.Writer;

        var readFromQueueTask = Task.Run(
            async () =>
            {
                try
                {
                    await ProcessMessages(
                        receiver,
                        writer.WriteAsync,
                        cancellationToken);

                    writer.Complete();
                }
                catch (Exception ex)
                {
                    writer.TryComplete(ex);
                }
            },
            cancellationToken);

        var writeToDatabaseTask = Task.Run(
            async () =>
            {
                await foreach (var msg in reader.ReadAllAsync(cancellationToken))
                {
                    using var textReader = new StreamReader(msg.Body.ToStream(), Encoding.UTF8);
                    using var jsonReader = new JsonReader(textReader);
                    BsonDeserializationContext context = BsonDeserializationContext.CreateRoot(jsonReader);
                    BsonDocument body = BsonDocumentSerializer.Instance.Deserialize(context);

                    var id = msg.MessageId;
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                    var update = Builders<BsonDocument>.Update
                        .Set("sequenceNumber", msg.SequenceNumber)
                        .Set("body", body);

                    var headerDoc = new BsonDocument();
                    foreach (var kv in msg.ApplicationProperties)
                    {
                        headerDoc[kv.Key] = BsonValue.Create(kv.Value);
                    }

                    update = update.Set("headers", headerDoc);

                    await collection.UpdateOneAsync(filter, update, new() { IsUpsert = true }, cancellationToken: cancellationToken);
                }
            },
            cancellationToken);

        await Task.WhenAll(readFromQueueTask, writeToDatabaseTask);
        task.Value = task.MaxValue;
        task.StopTask();
    }

    private static async Task CreateMoveToDlqTask(
        ProgressContext ctx,
        IClientSessionHandle session,
        ServiceBusHandle sb,
        IMongoDatabase db,
        Task? dependsOn,
        string name,
        string queue,
        CancellationToken cancellationToken)
    {
        var task = ctx.AddTask($"Move '{name}' to DLQ", autoStart: false);

        if (dependsOn is not null)
        {
            await dependsOn.WaitAsync(cancellationToken);
        }

        var stats = (await sb.AdministrationClient.GetQueueRuntimePropertiesAsync(queue, cancellationToken)).Value;
        if (stats.ActiveMessageCount <= 0)
        {
            task.MaxValue = 1;
            task.Value = 1;
            task.StartTask();
            task.StopTask();
            return;
        }

        task.MaxValue = stats.ActiveMessageCount;
        task.Value = 0;
        task.StartTask();

        await using var receiver = sb.Client.CreateReceiver(
            queue, 
            new ServiceBusReceiverOptions 
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = Math.Clamp((int)stats.ActiveMessageCount, 100, 10_000),
                SubQueue = SubQueue.None,
            });

        await ProcessMessages(
            receiver,
            async (msg, cancellationToken) =>
            {
                await receiver.DeadLetterMessageAsync(msg, cancellationToken: cancellationToken);
                task.Increment(1);
            },
            cancellationToken);

        task.MaxValue = task.Value;
        task.StopTask();
    }

    private static async Task ProcessMessages(ServiceBusReceiver receiver, Func<ServiceBusReceivedMessage, CancellationToken, ValueTask> process, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cts.Token;

        var channel = Channel.CreateBounded<ServiceBusReceivedMessage>(100);

        var workersTask = Parallel.ForEachAsync(
            channel.Reader.ReadAllAsync(cancellationToken),
            cancellationToken,
            process);

        var readerTask = Task.Run(
            async () =>
            {
                try
                {
                    var unqueued = new Queue<ServiceBusReceivedMessage>(receiver.PrefetchCount);
                    while (true)
                    {
                        if (unqueued.Count == 0)
                        {
                            var newMessages = await receiver.ReceiveMessagesAsync(receiver.PrefetchCount, maxWaitTime: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
                            if (newMessages.Count == 0)
                            {
                                break;
                            }

                            foreach (var message in newMessages)
                            {
                                unqueued.Enqueue(message);
                            }
                        }

                        await WriteAll(receiver, unqueued, channel.Writer, cancellationToken);
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            },
            cancellationToken);

        try
        {
            await Task.WhenAll(workersTask, readerTask);
        }
        catch
        {
            await cts.CancelAsync();
            throw;
        }

        static async Task WriteAll(ServiceBusReceiver receiver, Queue<ServiceBusReceivedMessage> queue, ChannelWriter<ServiceBusReceivedMessage> writer, CancellationToken cancellationToken)
        {
            while (queue.TryDequeue(out var msg))
            {
                var lockExpiry = msg.LockedUntil;
                var renewAt = lockExpiry - TimeSpan.FromMinutes(1);
                var duration = renewAt - DateTimeOffset.UtcNow;

                if (duration <= TimeSpan.Zero)
                {
                    await RenewLocks(receiver, queue, cancellationToken);
                }

                await writer.WriteAsync(msg, cancellationToken);
            }
        }

        static Task RenewLocks(ServiceBusReceiver receiver, Queue<ServiceBusReceivedMessage> messages, CancellationToken cancellationToken)
        {
            return Parallel.ForEachAsync(messages, cancellationToken, (msg, ct) => new(receiver.RenewMessageLockAsync(msg, ct)));
        }
    }

    private static async Task DisposeAfter(
        IDisposable disposable,
        IEnumerable<Task?> after,
        CancellationToken cancellationToken)
    {
        var tasks = after.Where(t => t is not null).Select(t => t!.WaitAsync(cancellationToken));
        await Task.WhenAll(tasks);
        
        if (disposable is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        disposable.Dispose();
    }

    private sealed class QueueInfo
    {
        private readonly Dictionary<QueueKind, QueueRuntimeProperties> _kinds = new();

        public string Name { get; init; }

        public QueueRuntimeProperties? Normal
            => _kinds.TryGetValue(QueueKind.Normal, out var props) ? props : null;

        public QueueRuntimeProperties? Error
            => _kinds.TryGetValue(QueueKind.Error, out var props) ? props : null;

        public ulong ErrorCount
            => (ulong)(
                (Normal?.DeadLetterMessageCount ?? 0L)
                + (Error?.ActiveMessageCount ?? 0L)
                + (Error?.DeadLetterMessageCount ?? 0L)
            );

        public void Add(QueueKind kind, QueueRuntimeProperties properties)
        {
            _kinds.Add(kind, properties);
        }
    }

    private enum QueueKind
    {
        Normal,
        Error,
    }

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
    }
}
