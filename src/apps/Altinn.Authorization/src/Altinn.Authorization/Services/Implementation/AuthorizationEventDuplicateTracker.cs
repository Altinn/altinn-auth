#nullable enable

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.EventLog;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Remembers recently seen authorization events, per instance, so that repeats of the same event can
    /// be identified before they are queued for the audit log.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two events are the same when every field that is persisted as a column in the audit log is equal:
    /// subject, resource, instance, resource party, action, decision, session and IP address. The
    /// timestamp, trace id and the full context request are not part of the comparison.
    /// </para>
    /// <para>
    /// The window is fixed from the first occurrence: an event that repeats continuously is reported as
    /// new once per window, not suppressed indefinitely.
    /// </para>
    /// <para>
    /// Events are stored as a 64-bit XxHash3, seeded randomly per process, in two generations of at most
    /// <see cref="AuditLogDeduplicationSettings.MaxTrackedEvents"/> entries each. A generation is
    /// discarded once everything in it is older than the window, so memory stays bounded without a
    /// background timer. Both generations are sized for the maximum on first use and reused afterwards,
    /// so memory use is fixed and nothing is allocated per event.
    /// </para>
    /// </remarks>
    public sealed class AuthorizationEventDuplicateTracker
    {
        // A random seed keeps the keys, which are not a cryptographic hash, from being predictable.
        private static readonly long Seed = Random.Shared.NextInt64();
        private static readonly ThreadLocal<ArrayBufferWriter<byte>> KeyBuffer = new(() => new ArrayBufferWriter<byte>(512));

        private readonly Lock _gate = new();
        private readonly TimeProvider _timeProvider;
        private readonly long _windowTicks;
        private readonly int _maxTrackedEvents;

        private Dictionary<ulong, long> _current = [];
        private Dictionary<ulong, long> _previous = [];

        // DateTime.MinValue, so that the first event starts a generation.
        private long _currentStartedTicks;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationEventDuplicateTracker"/> class.
        /// </summary>
        /// <param name="settings">Window and capacity settings</param>
        /// <param name="timeProvider">Clock used to age out remembered events</param>
        public AuthorizationEventDuplicateTracker(IOptions<AuditLogDeduplicationSettings> settings, TimeProvider timeProvider)
        {
            _windowTicks = settings.Value.Window.Ticks;
            _maxTrackedEvents = Math.Max(settings.Value.MaxTrackedEvents, 0);
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Classifies the event against those seen within the window, and remembers it.
        /// </summary>
        /// <param name="authorizationEvent">The event about to be queued for the audit log</param>
        /// <returns>Whether, and how, the event repeats one already seen</returns>
        public AuthorizationEventDuplicateKind Track(AuthorizationEvent authorizationEvent)
        {
            (ulong eventKey, ulong traceKey) = ComputeKeys(authorizationEvent);
            long now = _timeProvider.GetUtcNow().UtcTicks;

            lock (_gate)
            {
                RotateIfExpired(now);

                if (IsSeen(traceKey, now))
                {
                    return AuthorizationEventDuplicateKind.SameTrace;
                }

                bool seenInWindow = IsSeen(eventKey, now);

                // Only the keys that are missing need room: the trace key always, the event key when it
                // was not seen. Without room for the trace key, further repeats in this trace could not
                // be told apart from repeats in other traces, so the event is reported as untracked
                // rather than given a classification that later events would contradict.
                int missingKeys = seenInWindow ? 1 : 2;
                if (_current.Count + missingKeys > _maxTrackedEvents)
                {
                    return AuthorizationEventDuplicateKind.Untracked;
                }

                _current[traceKey] = now;

                if (seenInWindow)
                {
                    return AuthorizationEventDuplicateKind.Window;
                }

                _current[eventKey] = now;
                return AuthorizationEventDuplicateKind.None;
            }
        }

        private void RotateIfExpired(long now)
        {
            if (now - _currentStartedTicks < _windowTicks)
            {
                return;
            }

            // The previous generation only holds events from before the current one started, at least a
            // window ago, so it can be dropped. Its dictionary is reused for the new generation, and sized
            // for the maximum the first time, so that it never grows while events are tracked.
            (_previous, _current) = (_current, _previous);
            _current.Clear();
            _current.EnsureCapacity(_maxTrackedEvents);
            _currentStartedTicks = now;
        }

        private bool IsSeen(ulong key, long now)
        {
            // Entries in the current generation are always within the window. Entries in the previous
            // generation may have aged out.
            return _current.ContainsKey(key)
                || (_previous.TryGetValue(key, out long seen) && now - seen < _windowTicks);
        }

        private static (ulong EventKey, ulong TraceKey) ComputeKeys(AuthorizationEvent authorizationEvent)
        {
            ArrayBufferWriter<byte> buffer = KeyBuffer.Value!;
            buffer.ResetWrittenCount();

            // Ids are never 0, and null and empty strings mean the same here, so neither needs a marker of
            // its own. The decision does: Permit is 0.
            WriteString(buffer, authorizationEvent.Resource);
            WriteString(buffer, authorizationEvent.InstanceId);
            WriteInt(buffer, authorizationEvent.ResourcePartyId ?? 0);
            WriteInt(buffer, authorizationEvent.SubjectUserId ?? 0);
            WriteInt(buffer, authorizationEvent.SubjectParty ?? 0);
            WriteString(buffer, authorizationEvent.SubjectPartyUuid);
            WriteString(buffer, authorizationEvent.SubjectOrgCode);
            WriteInt(buffer, authorizationEvent.SubjectOrgNumber ?? 0);
            WriteString(buffer, authorizationEvent.SessionId);
            WriteString(buffer, authorizationEvent.IpAdress);
            WriteString(buffer, authorizationEvent.Operation);
            WriteInt(buffer, (int?)authorizationEvent.Decision ?? -1);
            int eventLength = buffer.WrittenCount;

            // The trace key is the event key input followed by the trace id.
            WriteString(buffer, authorizationEvent.TraceId);

            return (
                XxHash3.HashToUInt64(buffer.WrittenSpan[..eventLength], Seed),
                XxHash3.HashToUInt64(buffer.WrittenSpan, Seed));
        }

        private static void WriteString(ArrayBufferWriter<byte> buffer, string? value)
        {
            // The length prefix keeps adjacent fields apart: "ab" + "c" differs from "a" + "bc".
            WriteInt(buffer, value?.Length ?? 0);
            if (!string.IsNullOrEmpty(value))
            {
                buffer.Write(MemoryMarshal.AsBytes(value.AsSpan()));
            }
        }

        private static void WriteInt(ArrayBufferWriter<byte> buffer, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.GetSpan(sizeof(int)), value);
            buffer.Advance(sizeof(int));
        }
    }
}
