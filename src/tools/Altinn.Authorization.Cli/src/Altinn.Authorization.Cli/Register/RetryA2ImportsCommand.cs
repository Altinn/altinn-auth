using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Altinn.Authorization.Cli.Database;
using Altinn.Authorization.Cli.ServiceBus.MassTransit;
using Altinn.Authorization.Cli.ServiceBus.Utils;
using Altinn.Authorization.Cli.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CommunityToolkit.Diagnostics;
using Npgsql;
using NpgsqlTypes;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Altinn.Authorization.Cli.Register;

/// <summary>
/// Command for retrying failed messages.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RetryA2ImportsCommand(CancellationToken ct)
    : BaseCommand<RetryA2ImportsCommand.Settings>(ct)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // Note: order of queues is important
    private static readonly ImmutableArray<ErrorQueueHandler> ErrorQueues = [
        new A2ImportErrorQueueHandler(),
        new A2ProfileImportErrorQueueHandler(),
        new ImportValidationQueueHandler(),
        new ImportBatchErrorQueueHandler(),
        ////new A2ExternalRoleResolverErrorQueueHandler(),
        ////new UpsertUserRecordQueueHandler(),
    ];

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var db = await DbHelper.Create(settings.Database, cancellationToken);
        var sb = ServiceBusHandle.Create(settings.ServiceBus);

        var skipped = new Dictionary<string, long>();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SkippedColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async (ctx) =>
            {
                Dictionary<string, QueueRuntimeProperties> sbQueues;
                {
                    using var task = ctx.AddTask("Getting queues", autoStart: false).Run(setValueMax: true);
                    sbQueues = await sb.AdministrationClient.GetQueuesRuntimePropertiesAsync(cancellationToken)
                        .ToDictionaryAsync(static queue => queue.Name)
                        .LogOnFailure("Failed to get queues");
                }

                var queueNames = sbQueues.Keys.ToHashSet(StringComparer.Ordinal);
                await using var sender = new MessageSender(sb.Client);
                var context = new Context(
                    db: db,
                    sender: sender);

                var handled = new HashSet<string>(StringComparer.Ordinal);
                foreach (var queue in ErrorQueues)
                {
                    if (queueNames.Contains(queue.Name) || queueNames.Contains(queue.ErrorQueueName) || queueNames.Contains(queue.SkippedQueueName))
                    {
                        handled.Add(queue.Name);
                        handled.Add(queue.ErrorQueueName);
                        handled.Add(queue.SkippedQueueName);
                        await Process(sb, context, queue, queueNames, ctx, cancellationToken);
                    }
                }

                foreach (var queue in sbQueues)
                {
                    if (!handled.Contains(queue.Key) && ErrorMessageCount(queue.Value) is var errorMessageCount and > 0)
                    {
                        skipped.Add(queue.Key, errorMessageCount);
                    }
                }
            });

        foreach (var s in skipped)
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]Warning:[/] Queue [blue]{s.Key}[/] with [cyan]{s.Value}[/] errors not handled, skipping.");
        }

        return 0;
    }

    private static long ErrorMessageCount(
        QueueRuntimeProperties props)
    {
        var errorCount = props.DeadLetterMessageCount;
        
        if (props.Name.EndsWith("_error", StringComparison.Ordinal))
        {
            errorCount += props.ActiveMessageCount;
        }

        return errorCount;
    }

    private async Task Process(
        ServiceBusHandle sb,
        Context context,
        ErrorQueueHandler queue,
        HashSet<string> queueNames,
        ProgressContext ctx,
        CancellationToken cancellationToken)
    {
        if (queueNames.Contains(queue.Name))
        {
            await RetryDeadLetters(sb, context, queue, queue.Name, ctx, cancellationToken);
        }

        if (queueNames.Contains(queue.SkippedQueueName))
        {
            await DeadLetterMessages(sb, queue, queue.SkippedQueueName, ctx, cancellationToken);
            await RetryDeadLetters(sb, context, queue, queue.SkippedQueueName, ctx, cancellationToken);
        }

        if (queueNames.Contains(queue.ErrorQueueName))
        {
            await DeadLetterMessages(sb, queue, queue.ErrorQueueName, ctx, cancellationToken);
            await RetryDeadLetters(sb, context, queue, queue.ErrorQueueName, ctx, cancellationToken);
        }
    }

    private async Task DeadLetterMessages(
        ServiceBusHandle sb,
        ErrorQueueHandler queue,
        string queueName,
        ProgressContext ctx,
        CancellationToken cancellationToken)
    {
        var queueStats = await sb.AdministrationClient.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);

        var expected = (int)queueStats.Value.ActiveMessageCount;
        var task = ctx.AddTask($"Dead-letter {queueName} messages", autoStart: true, maxValue: expected);
        using var dqlTask = task.Run();

        if (expected == 0)
        {
            task.MaxValue = 1;
            task.Value = 1;
            return;
        }

        await using var receiver = sb.Client.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = Math.Clamp((int)expected, 100, 10_000),
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
    }

    private async Task RetryDeadLetters(
        ServiceBusHandle sb,
        Context context,
        ErrorQueueHandler queue,
        string queueName,
        ProgressContext ctx,
        CancellationToken cancellationToken)
    {
        var queueStats = await sb.AdministrationClient.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);

        var expected = (int)queueStats.Value.DeadLetterMessageCount;
        var task = ctx.AddTask($"Retry {queueName} messages", autoStart: true, maxValue: expected);
        using var retryTask = task.Run();

        if (expected == 0)
        {
            task.MaxValue = 1;
            task.Value = 1;
            return;
        }

        await using var receiver = sb.Client.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = Math.Clamp((int)expected, 100, 10_000),
            SubQueue = SubQueue.DeadLetter,
        });

        await ProcessMessages(
            receiver,
            async (msg, cancellationToken) =>
            {
                foreach (var handler in queue.GetHandlers(msg))
                {
                    if (await handler.Handle(queue.Name, msg, context, cancellationToken))
                    {
                        await receiver.CompleteMessageAsync(msg, cancellationToken);
                        task.Increment(1);
                        return;
                    }
                }

                task.Increment(1);
                task.State.Update<double>("task.skipped", static s => s + 1);
            },
            cancellationToken);
    }

    private async Task ProcessMessages(ServiceBusReceiver receiver, Func<ServiceBusReceivedMessage, CancellationToken, ValueTask> process, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cts.Token;

        var channel = Channel.CreateBounded<ServiceBusReceivedMessage>(100);

        Task workersTask;
        if (!Debugger.IsAttached)
        {
            workersTask = Parallel.ForEachAsync(
                channel.Reader.ReadAllAsync(cancellationToken),
                cancellationToken,
                process);
        }
        else
        {
            workersTask = Task.Run(
                async () =>
                {
                    await foreach (var msg in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        await process(msg, cancellationToken);
                    }
                },
                cancellationToken);
        }

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

    private abstract class ErrorQueueMessageHandler(Utf8String messageUrn)
    {
        public Utf8String MessageUrn { get; } = messageUrn;

        public abstract ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage message, Context sender, CancellationToken cancellationToken);
    }

    private abstract class ErrorQueueMessageHandler<T>()
        : ErrorQueueMessageHandler(T.MessageUrn)
        where T : class, IFakeMassTransitMessage<T>
    {
        protected abstract ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, T message, Context sender, CancellationToken cancellationToken);

        public sealed override ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage message, Context sender, CancellationToken cancellationToken)
        {
            // we've already validated the message urn, so we can skip that
            var reader = new Utf8JsonReader(message.Body.ToMemory().Span);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return ValueTask.FromResult(false);
            }

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    return ValueTask.FromResult(false);
                }

                if (!reader.ValueTextEquals("message"u8))
                {
                    reader.Skip();
                    continue;
                }

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    return ValueTask.FromResult(false);
                }

                break;
            }

            // found message object
            T? body = null;
            try
            {
                body = JsonSerializer.Deserialize<T>(ref reader, Options);
            }
            catch (JsonException)
            {
                return ValueTask.FromResult(false);
            }

            if (body is null)
            {
                return ValueTask.FromResult(false);
            }

            return Handle(queueName, message, body, sender, cancellationToken);
        }
    }

    private abstract class ErrorQueueHandler(
        string queueName,
        ImmutableArray<ErrorQueueMessageHandler> handlers)
    {
        private readonly ImmutableArray<ErrorQueueMessageHandler> _handlers = handlers;

        public string Name { get; } = queueName;

        public string ErrorQueueName { get; } = $"{queueName}_error";

        public string SkippedQueueName { get; } = $"{queueName}_skipped";

        public bool Handles(ServiceBusReceivedMessage message)
            => GetHandlers(message).Any();

        public virtual IEnumerable<ErrorQueueMessageHandler> GetHandlers(ServiceBusReceivedMessage message)
        {
            if (message.ContentType != "application/vnd.masstransit+json")
            {
                return [];
            }

            var reader = new Utf8JsonReader(message.Body.ToMemory().Span);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return [];
            }

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    return [];
                }

                if (!reader.ValueTextEquals("messageType"u8))
                {
                    reader.Skip();
                    continue;
                }

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    return [];
                }

                break;
            }

            // found messageType array
            var handlers = new List<ErrorQueueMessageHandler>();
            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                foreach (var h in _handlers)
                {
                    if (reader.ValueTextEquals(h.MessageUrn))
                    {
                        if (!handlers.Contains(h, ReferenceEqualityComparer.Instance))
                        {
                            handlers.Add(h);
                        }
                    }
                }
            }

            return handlers;
        }
    }

    private sealed class ImportValidationQueueHandler()
        : ErrorQueueHandler(
            "register-party-import-validation",
            [
                new UpsertA2PartyCommandHandler(),
            ])
    {
        private sealed class UpsertA2PartyCommandHandler
            : ErrorQueueMessageHandler<UpsertPartyCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, UpsertPartyCommand message, Context sender, CancellationToken cancellationToken)
            {
                // we only handle imports from A2
                if (message.Tracking.JobName != "a2-party-import:party")
                {
                    return false;
                }

                var command = new ImportA2PartyCommand
                {
                    PartyUuid = message.Party.PartyUuid,
                    ChangeId = message.Tracking.Progress,
                    ChangedTime = DateTimeOffset.UtcNow,
                    Tracking = message.Tracking,
                };

                await sender.Send(command, cancellationToken);
                return true;
            }
        }

        private sealed record UpsertPartyCommand
            : BaseCommand
            , IFakeMassTransitMessage<UpsertPartyCommand>
        {
            static Utf8String IFakeMassTransitMessage<UpsertPartyCommand>.MessageUrn
                => "urn:message:Altinn.Register.PartyImport:UpsertPartyCommand"u8;

            static Guid IFakeMassTransitMessage<UpsertPartyCommand>.MessageId(UpsertPartyCommand message)
                => message.CommandId;

            static Guid IFakeMassTransitMessage<UpsertPartyCommand>.CorrelationId(UpsertPartyCommand message)
                => message.CorrelationId;

            public JobTrackig Tracking { get; init; }

            public PartialParty Party { get; init; }
        }
    }

    private sealed class ImportBatchErrorQueueHandler()
        : ErrorQueueHandler(
            "register-party-import-batch",
            [
                new UpsertValidatedA2PartyCommandHandler(),
                new UpsertValidatedSystemUserPartyCommandHandler(),
            ])
    {
        private sealed class UpsertValidatedA2PartyCommandHandler
            : ErrorQueueMessageHandler<UpsertValidatedPartyCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, UpsertValidatedPartyCommand message, Context sender, CancellationToken cancellationToken)
            {
                // we only handle imports from A2
                if (message.Tracking.JobName != "a2-party-import:party")
                {
                    return false;
                }

                var command = new ImportA2PartyCommand
                {
                    PartyUuid = message.Party.PartyUuid,
                    ChangeId = message.Tracking.Progress,
                    ChangedTime = DateTimeOffset.UtcNow,
                    Tracking = message.Tracking,
                };

                await sender.Send(command, cancellationToken);
                return true;
            }
        }

        private sealed class UpsertValidatedSystemUserPartyCommandHandler
            : ErrorQueueMessageHandler<UpsertValidatedPartyCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, UpsertValidatedPartyCommand message, Context sender, CancellationToken cancellationToken)
            {
                // we only handle imports from A2
                if (message.Tracking.JobName != "systemuser-import:systemuser")
                {
                    return false;
                }

                var command = new ImportA2PartyCommand
                {
                    PartyUuid = message.Party.PartyUuid,
                    ChangeId = message.Tracking.Progress,
                    ChangedTime = DateTimeOffset.UtcNow,
                    Tracking = message.Tracking,
                };

                await sender.Send(command, cancellationToken);
                return true;
            }
        }

        private sealed record UpsertValidatedPartyCommand
            : BaseCommand
            , IFakeMassTransitMessage<UpsertValidatedPartyCommand>
        {
            static Utf8String IFakeMassTransitMessage<UpsertValidatedPartyCommand>.MessageUrn
                => "urn:message:Altinn.Register.PartyImport:UpsertValidatedPartyCommand"u8;

            static Guid IFakeMassTransitMessage<UpsertValidatedPartyCommand>.MessageId(UpsertValidatedPartyCommand message)
                => message.CommandId;

            static Guid IFakeMassTransitMessage<UpsertValidatedPartyCommand>.CorrelationId(UpsertValidatedPartyCommand message)
                => message.CorrelationId;

            public JobTrackig Tracking { get; init; }

            public PartialParty Party { get; init; }
        }
    }

    private sealed class A2ImportErrorQueueHandler()
        : ErrorQueueHandler(
            QueueName,
            [
                new ImportA2PartyCommandHandler(),
                new ImportA2ProfileCommandHandler(),
                new ImportA2CCRRolesCommandHandler(),
                ////new ImportA2UserIdForPartyCommandHandler(),
                new EnrichA2PartyImportSagaCommandHandler(),
                new CompleteA2PartyImportSagaCommandHandler(),
                new RetryA2PartyImportSagaCommandHandler(),
            ])
    {
        public const string QueueName = "register-a2-party-import";

        private sealed class ImportA2PartyCommandHandler
            : ErrorQueueMessageHandler<ImportA2PartyCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, ImportA2PartyCommand message, Context sender, CancellationToken cancellationToken)
            {
                await sender.Send(message, cancellationToken);
                return true;
            }
        }

        public sealed class ImportA2ProfileCommandHandler
            : ErrorQueueMessageHandler<ImportA2UserProfileCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, ImportA2UserProfileCommand message, Context sender, CancellationToken cancellationToken)
            {
                await sender.Send(message, cancellationToken);
                return true;
            }
        }

        private sealed class ImportA2CCRRolesCommandHandler
            : ErrorQueueMessageHandler<ImportA2CCRRolesCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, ImportA2CCRRolesCommand message, Context sender, CancellationToken cancellationToken)
            {
                var newCommand = new ImportA2PartyCommand
                {
                    PartyUuid = message.PartyUuid,
                    ChangeId = 0,
                    ChangedTime = message.ChangedTime,
                    Tracking = new JobTrackig
                    {
                        JobName = "a2-party-import:party",
                        Progress = 1,
                    },
                };

                await sender.Send(newCommand, cancellationToken);
                return true;
            }
        }

        ////private sealed class ImportA2UserIdForPartyCommandHandler
        ////    : ErrorQueueMessageHandler<ImportA2UserIdForPartyCommand>
        ////{
        ////    protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, ImportA2UserIdForPartyCommand message, Context sender, CancellationToken cancellationToken)
        ////    {
        ////        await sender.Send(message, cancellationToken);
        ////        return true;
        ////    }
        ////}

        private sealed class EnrichA2PartyImportSagaCommandHandler
            : ErrorQueueMessageHandler<EnrichA2PartyImportSagaCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, EnrichA2PartyImportSagaCommand message, Context sender, CancellationToken cancellationToken)
            {
                var retry = new RetryA2PartyImportSagaCommand
                {
                    CorrelationId = message.CorrelationId,
                };

                await sender.Send(retry, cancellationToken);
                return true;
            }
        }

        private sealed class CompleteA2PartyImportSagaCommandHandler
            : ErrorQueueMessageHandler<CompleteA2PartyImportSagaCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, CompleteA2PartyImportSagaCommand message, Context sender, CancellationToken cancellationToken)
            {
                var retry = new RetryA2PartyImportSagaCommand
                {
                    CorrelationId = message.CorrelationId,
                };

                await sender.Send(retry, cancellationToken);
                return true;
            }
        }

        private sealed class RetryA2PartyImportSagaCommandHandler
            : ErrorQueueMessageHandler<RetryA2PartyImportSagaCommand>
        {
            protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, RetryA2PartyImportSagaCommand message, Context sender, CancellationToken cancellationToken)
            {
                var retry = new RetryA2PartyImportSagaCommand
                {
                    CorrelationId = message.CorrelationId,
                };

                await sender.Send(retry, cancellationToken);
                return true;
            }
        }

        ////private sealed class JunkHandler()
        ////    : ErrorQueueMessageHandler("junk"u8)
        ////{
        ////    public static JunkHandler Instance { get; } = new();

        ////    public override ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage message, Context sender, CancellationToken cancellationToken)
        ////    {
        ////        try
        ////        {
        ////            var command = message.Body.ToObjectFromJson<ImportA2PartyCommand>(Options);
        ////            if (command is null)
        ////            {
        ////                return ValueTask.FromResult(false);
        ////            }

        ////            return SendAndReturn(command, sender, cancellationToken);
        ////        }
        ////        catch (JsonException)
        ////        {
        ////            return ValueTask.FromResult(false);
        ////        }
        ////    }

        ////    private async ValueTask<bool> SendAndReturn(ImportA2PartyCommand command, Context sender, CancellationToken cancellationToken)
        ////    {
        ////        await sender.Send(command, cancellationToken);
        ////        return true;
        ////    }
        ////}

        public sealed record EnrichA2PartyImportSagaCommand
            : BaseCommand
            , IFakeMassTransitMessage<EnrichA2PartyImportSagaCommand>
        {
            static Utf8String IFakeMassTransitMessage<EnrichA2PartyImportSagaCommand>.MessageUrn
                => "urn:message:Altinn.Register.PartyImport.A2:EnrichA2PartyImportSagaCommand"u8;

            static Guid IFakeMassTransitMessage<EnrichA2PartyImportSagaCommand>.MessageId(EnrichA2PartyImportSagaCommand message)
                => message.CommandId;

            static Guid IFakeMassTransitMessage<EnrichA2PartyImportSagaCommand>.CorrelationId(EnrichA2PartyImportSagaCommand message)
                => message.CorrelationId;
        }

        public sealed record CompleteA2PartyImportSagaCommand
            : BaseCommand
            , IFakeMassTransitMessage<CompleteA2PartyImportSagaCommand>
        {
            static Utf8String IFakeMassTransitMessage<CompleteA2PartyImportSagaCommand>.MessageUrn
                => "urn:message:Altinn.Register.PartyImport.A2:CompleteA2PartyImportSagaCommand"u8;

            static Guid IFakeMassTransitMessage<CompleteA2PartyImportSagaCommand>.MessageId(CompleteA2PartyImportSagaCommand message)
                => message.CommandId;

            static Guid IFakeMassTransitMessage<CompleteA2PartyImportSagaCommand>.CorrelationId(CompleteA2PartyImportSagaCommand message)
                => message.CorrelationId;
        }

        public sealed record RetryA2PartyImportSagaCommand
            : BaseCommand
            , IFakeMassTransitMessage<RetryA2PartyImportSagaCommand>
        {
            static Utf8String IFakeMassTransitMessage<RetryA2PartyImportSagaCommand>.MessageUrn
                => "urn:message:Altinn.Register.PartyImport.A2:RetryA2PartyImportSagaCommand"u8;

            static Guid IFakeMassTransitMessage<RetryA2PartyImportSagaCommand>.MessageId(RetryA2PartyImportSagaCommand message)
                => message.CommandId;

            static Guid IFakeMassTransitMessage<RetryA2PartyImportSagaCommand>.CorrelationId(RetryA2PartyImportSagaCommand message)
                => message.CorrelationId;
        }
    }

    private sealed class A2ProfileImportErrorQueueHandler()
        : ErrorQueueHandler(
            QueueName,
            [
                new A2ImportErrorQueueHandler.ImportA2ProfileCommandHandler(),
            ])
    {
        public const string QueueName = "register-a2-profile-import";
    }

    private abstract record BaseCommand
    {
        private Guid? _correlationId;

        public Guid CommandId { get; } = Guid.CreateVersion7();

        public Guid CorrelationId
        {
            get => _correlationId ?? CommandId;
            init => _correlationId = value;
        }
    }

    ////private sealed class A2ProfileImportQueueHandler()
    ////    : ErrorQueueHandler(
    ////        "register-a2-profile-import",
    ////        [
    ////            new ImportA2UserProfileCommandHandler(),
    ////        ])
    ////{
    ////    private sealed class ImportA2UserProfileCommandHandler
    ////        : ErrorQueueMessageHandler<ImportA2UserProfileCommand>
    ////    {
    ////        protected override async ValueTask<bool> Handle(string queueName, ServiceBusReceivedMessage received, ImportA2UserProfileCommand message, Context sender, CancellationToken cancellationToken)
    ////        {
    ////            await sender.Send(message, cancellationToken);
    ////            return true;
    ////        }
    ////    }
    ////}

    private sealed record ImportA2PartyCommand
        : BaseCommand
        , IFakeMassTransitMessage<ImportA2PartyCommand>
    {
        static Utf8String IFakeMassTransitMessage<ImportA2PartyCommand>.MessageUrn
            => "urn:message:Altinn.Register.PartyImport.A2:ImportA2PartyCommand"u8;

        static Guid IFakeMassTransitMessage<ImportA2PartyCommand>.MessageId(ImportA2PartyCommand message)
                => message.CommandId;

        static Guid IFakeMassTransitMessage<ImportA2PartyCommand>.CorrelationId(ImportA2PartyCommand message)
            => message.CorrelationId;

        /// <summary>
        /// Gets the party UUID.
        /// </summary>
        public required Guid PartyUuid { get; init; }

        /// <summary>
        /// Gets the change ID.
        /// </summary>
        public required uint ChangeId { get; init; }

        /// <summary>
        /// Gets when the change was registered.
        /// </summary>
        public required DateTimeOffset ChangedTime { get; init; }

        /// <summary>
        /// Gets tracking information.
        /// </summary>
        public required JobTrackig Tracking { get; init; }
    }

    private sealed record ImportA2UserProfileCommand
        : BaseCommand
        , IFakeMassTransitMessage<ImportA2UserProfileCommand>
    {
        static Utf8String IFakeMassTransitMessage<ImportA2UserProfileCommand>.MessageUrn
            => "urn:message:Altinn.Register.PartyImport.A2:ImportA2UserProfileCommand"u8;

        static Guid IFakeMassTransitMessage<ImportA2UserProfileCommand>.MessageId(ImportA2UserProfileCommand message)
                => message.CommandId;

        static Guid IFakeMassTransitMessage<ImportA2UserProfileCommand>.CorrelationId(ImportA2UserProfileCommand message)
            => message.CorrelationId;

        /// <summary>
        /// Gets the user ID.
        /// </summary>
        public required ulong UserId { get; init; }

        /// <summary>
        /// Gets the owner party UUID.
        /// </summary>
        public required Guid OwnerPartyUuid { get; init; }

        /// <summary>
        /// Gets whether the user profile was deleted at the update time.
        /// </summary>
        public required bool IsDeleted { get; init; }

        /// <summary>
        /// Gets tracking information for the import job.
        /// </summary>
        public required JobTrackig Tracking { get; init; }
    }

    private sealed record ImportA2CCRRolesCommand
        : BaseCommand
        , IFakeMassTransitMessage<ImportA2CCRRolesCommand>
    {
        static Utf8String IFakeMassTransitMessage<ImportA2CCRRolesCommand>.MessageUrn
            => "urn:message:Altinn.Register.PartyImport.A2:ImportA2CCRRolesCommand"u8;

        static Guid IFakeMassTransitMessage<ImportA2CCRRolesCommand>.MessageId(ImportA2CCRRolesCommand message)
            => message.CommandId;

        static Guid IFakeMassTransitMessage<ImportA2CCRRolesCommand>.CorrelationId(ImportA2CCRRolesCommand message)
            => message.CorrelationId;

        /// <summary>
        /// Gets the party ID.
        /// </summary>
        /// <remarks>
        /// It's the callers responsibility to ensure that <see cref="PartyId"/> and <see cref="PartyUuid"/>
        /// is for the same party. Failing to do so will result in undefined behavior.
        /// </remarks>
        public required int PartyId { get; init; }

        /// <summary>
        /// Gets the party UUID.
        /// </summary>
        /// <remarks>
        /// It's the callers responsibility to ensure that <see cref="PartyId"/> and <see cref="PartyUuid"/>
        /// is for the same party. Failing to do so will result in undefined behavior.
        /// </remarks>
        public required Guid PartyUuid { get; init; }

        /// <summary>
        /// Gets the change ID.
        /// </summary>
        public required uint ChangeId { get; init; }

        /// <summary>
        /// Gets when the change was registered.
        /// </summary>
        public required DateTimeOffset ChangedTime { get; init; }
    }

    ////private sealed record ImportA2UserIdForPartyCommand
    ////    : IFakeMassTransitMessage<ImportA2UserIdForPartyCommand>
    ////{
    ////    static Utf8String IFakeMassTransitMessage<ImportA2UserIdForPartyCommand>.MessageUrn
    ////        => "urn:message:Altinn.Register.PartyImport.A2:ImportA2UserIdForPartyCommand"u8;

    ////    static Guid IFakeMassTransitMessage<ImportA2UserIdForPartyCommand>.MessageId(ImportA2UserIdForPartyCommand message)
    ////        => message.CommandId;

    ////    public Guid CommandId { get; } = Guid.CreateVersion7();

    ////    /// <summary>
    ////    /// Gets the party UUID.
    ////    /// </summary>
    ////    public required Guid PartyUuid { get; init; }

    ////    /// <summary>
    ////    /// Gets the party type.
    ////    /// </summary>
    ////    public required PartyRecordType PartyType { get; init; }

    ////    /// <summary>
    ////    /// Gets the tracking information for the upsert.
    ////    /// </summary>
    ////    public JobTrackig Tracking { get; init; }
    ////}

    private sealed record JobTrackig
    {
        public string JobName { get; init; }

        public uint Progress { get; init; }
    }

    private sealed record PartialParty
    {
        public Guid PartyUuid { get; init; }

        public Guid? OwnerUuid { get; init; }

        public PartialUser? User { get; init; }

        public bool IsDeleted { get; init; }
    }

    private sealed record PartialUser
    {
        public uint UserId { get; init; }
    }

    private sealed class MessageSender(ServiceBusClient client)
        : IAsyncDisposable
    {
        private readonly Dictionary<string, ServiceBusSender> _senders = new();
        private readonly Lock _lock = new();
        private Task _dispose;

        public Task SendMessageAsync(
            string queue,
            ServiceBusMessage message,
            CancellationToken cancellationToken)
        {
            var sender = GetSender(queue);
            return sender.SendMessageAsync(message, cancellationToken);
        }

        private ServiceBusSender GetSender(string queue)
        {
            lock (_lock)
            {
                if (_dispose is not null)
                {
                    ThrowHelper.ThrowObjectDisposedException(nameof(MessageSender));
                }

                ref var sender = ref CollectionsMarshal.GetValueRefOrAddDefault(_senders, queue, out var exists);
                if (!exists)
                {
                    sender = client.CreateSender(queue);
                }

                return sender;
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                if (_dispose is null)
                {
                    var senders = _senders.Values.ToArray();
                    _senders.Clear();
                    _dispose = DisposeAsyncCore(senders);
                }

                return new ValueTask(_dispose);
            }

            static async Task DisposeAsyncCore(ServiceBusSender[] senders)
            {
                foreach (var sender in senders)
                {
                    await sender.DisposeAsync();
                }
            }
        }
    }

    private sealed class Context(
        DbHelper db,
        MessageSender sender)
    {
        public async Task Send(ImportA2PartyCommand command, CancellationToken cancellationToken)
        {
            var data = new BinaryData(FakeMassTransitEnvelope.Create(command), Options, typeof(FakeMassTransitEnvelope<ImportA2PartyCommand>));
            var message = new ServiceBusMessage(data);
            message.ContentType = "application/vnd.masstransit+json";
            await sender.SendMessageAsync("register-a2-party-import", message, cancellationToken);
        }

        public async Task Send(ImportA2UserProfileCommand command, CancellationToken cancellationToken)
        {
            var data = new BinaryData(FakeMassTransitEnvelope.Create(command), Options, typeof(FakeMassTransitEnvelope<ImportA2UserProfileCommand>));
            var message = new ServiceBusMessage(data);
            message.ContentType = "application/vnd.masstransit+json";
            await sender.SendMessageAsync("register-a2-party-import", message, cancellationToken);
        }

        public async Task Send(A2ImportErrorQueueHandler.RetryA2PartyImportSagaCommand command, CancellationToken cancellationToken)
        {
            var data = new BinaryData(FakeMassTransitEnvelope.Create(command), Options, typeof(FakeMassTransitEnvelope<A2ImportErrorQueueHandler.RetryA2PartyImportSagaCommand>));
            var message = new ServiceBusMessage(data);
            message.ContentType = "application/vnd.masstransit+json";
            await sender.SendMessageAsync(A2ImportErrorQueueHandler.QueueName, message, cancellationToken);
        }

        public async Task<bool> Resend(string queueName, ServiceBusReceivedMessage originalMessage, CancellationToken cancellationToken)
        {
            using var doc = JsonDocument.Parse(originalMessage.Body.ToMemory());
            if (!doc.RootElement.TryGetProperty("message", out var innerMessage))
            {
                return false;
            }

            if (!doc.RootElement.TryGetProperty("messageType", out var messageType))
            {
                return false;
            }

            if (!doc.RootElement.TryGetProperty("messageId", out var messageId))
            {
                return false;
            }

            var envelope = new AnonymousFakeMessageEnvelope
            {
                MessageId = messageId,
                MessageType = messageType,
                Message = innerMessage,
            };

            var data = new BinaryData(envelope, Options, typeof(AnonymousFakeMessageEnvelope));
            var message = new ServiceBusMessage(data);
            message.ContentType = "application/vnd.masstransit+json";
            await sender.SendMessageAsync(queueName, message, cancellationToken);
            return true;
        }

        public async Task<int> GetPartyIdFromUuid(Guid partyUuid, CancellationToken cancellationToken)
        {
            const string QUERY =
                /*strpsql*/"""
                SELECT p.id
                FROM register.party p
                WHERE p.uuid = @partyUuid
                """;

            await using var conn = await db.Source.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = QUERY;

            var parameter = new NpgsqlParameter<Guid>("partyUuid", NpgsqlDbType.Uuid);
            parameter.TypedValue = partyUuid;
            cmd.Parameters.Add(parameter);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Party not found");
            }

            return reader.GetInt32(0);
        }

        public async Task<PartyRecordType> GetPartyTypeFromUuid(Guid partyUuid, CancellationToken cancellationToken)
        {
            const string QUERY =
                /*strpsql*/"""
                SELECT p.party_type
                FROM register.party p
                WHERE p.uuid = @partyUuid
                """;

            await using var conn = await db.Source.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = QUERY;

            var parameter = new NpgsqlParameter<Guid>("partyUuid", NpgsqlDbType.Uuid);
            parameter.TypedValue = partyUuid;
            cmd.Parameters.Add(parameter);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Party not found");
            }

            var rawString = reader.GetString(0);
            var jsonString = JsonSerializer.Serialize(rawString, Options);
            return JsonSerializer.Deserialize<PartyRecordType>(jsonString, Options);
        }

        public async Task<bool> GetPartyIsDeleted(Guid partyUuid, CancellationToken cancellationToken)
        {
            const string QUERY =
                /*strpsql*/"""
                SELECT p.is_deleted
                FROM register.party p
                WHERE p.uuid = @partyUuid
                """;

            await using var conn = await db.Source.OpenConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = QUERY;

            var parameter = new NpgsqlParameter<Guid>("partyUuid", NpgsqlDbType.Uuid);
            parameter.TypedValue = partyUuid;
            cmd.Parameters.Add(parameter);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Party not found");
            }

            return reader.GetBoolean(0);
        }
    }

    private sealed class SkippedColumn
        : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var skipped = task.State.Get<double>("task.skipped");
            var total = task.Value;
            var notSkipped = total - skipped;

            return Markup.FromInterpolated($"[green]{notSkipped:F0}[/]/[yellow]{skipped:F0}[/]/[blue]{task.MaxValue:F0}[/]");
        }
    }

    /// <summary>
    /// Represents a party type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PartyRecordType
    {
        /// <summary>
        /// Person party type.
        /// </summary>
        [JsonStringEnumMemberName("person")]
        Person = 1,

        /// <summary>
        /// Organization party type.
        /// </summary>
        [JsonStringEnumMemberName("organization")]
        Organization,

        /// <summary>
        /// Self-identified user party type.
        /// </summary>
        [JsonStringEnumMemberName("self-identified-user")]
        SelfIdentifiedUser,

        /// <summary>
        /// System user party type.
        /// </summary>
        [JsonStringEnumMemberName("system-user")]
        SystemUser,

        /// <summary>
        /// Enterprise user party type.
        /// </summary>
        [JsonStringEnumMemberName("enterprise-user")]
        EnterpriseUser,
    }

    /// <summary>
    /// Settings for the retry command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Gets the connection string to the service-bus.
        /// </summary>
        [Description("The connection string to the service-bus.")]
        [CommandArgument(0, "<CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? ServiceBus { get; init; }

        /// <summary>
        /// Gets the connection string to the database.
        /// </summary>
        [Description("The connection string to the database.")]
        [CommandArgument(1, "<CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? Database { get; init; }
    }
}
