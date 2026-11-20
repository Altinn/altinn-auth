#nullable enable

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Telemetry;
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
    /// subject, resource, instance, resource party, action, decision, session and IP address. So must the
    /// resource instance id (<c>urn:altinn:resource:instance-id</c>): it is not a column, but tells apart,
    /// for example, reads of two different messages of the same resource. The timestamp, trace id and the
    /// rest of the context request are not part of the comparison.
    /// </para>
    /// <para>
    /// The window is fixed from the first occurrence: an event that repeats continuously is reported as
    /// new once per window, not suppressed indefinitely.
    /// </para>
    /// <para>
    /// Events are stored as a 64-bit XxHash3, seeded randomly per process, in two generations of at most
    /// <see cref="AuditLogDeduplicationSettings.MaxTrackedEvents"/> entries each. A new generation is
    /// started when the window has passed, or early when the current one is full, and the oldest is
    /// dropped. A key found in the previous generation is copied into the current one, so keys that keep
    /// repeating survive, and the tracker holds on to the most recent events rather than the first ones.
    /// Under load the oldest events are therefore forgotten before their window ends, which is counted in
    /// <c>altinn.pdp.auditlog.tracker.capacity_rotations</c>. Both generations are sized for the maximum
    /// on first use and reused, so memory use is fixed and nothing is allocated per event.
    /// </para>
    /// <para>
    /// The measurement (<see cref="Track"/>) and dropping (<see cref="Classify"/> and
    /// <see cref="Remember"/>) keep separate state, each allocated only when used. The measurement
    /// remembers every event, queued or not, so its state must never decide that an event is dropped,
    /// also when dropping is switched on while the application runs.
    /// </para>
    /// </remarks>
    public sealed class AuthorizationEventDuplicateTracker
    {
        // An event stores at most two keys: its trace key and, the first time it is seen, its event key.
        private const int MaxKeysPerEvent = 2;

        // A random seed keeps the keys, which are not a cryptographic hash, from being predictable.
        private static readonly long Seed = Random.Shared.NextInt64();
        private static readonly ThreadLocal<ArrayBufferWriter<byte>> KeyBuffer = new(() => new ArrayBufferWriter<byte>(512));

        private readonly Lock _gate = new();
        private readonly TimeProvider _timeProvider;
        private readonly DecisionTelemetry _telemetry;

        // Separate state for the measurement and for dropping. The measurement remembers every event
        // without knowing whether it was queued, so its state must never decide that an event is dropped.
        private readonly Generations _observed;
        private readonly Generations _confirmed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationEventDuplicateTracker"/> class.
        /// </summary>
        /// <param name="settings">Window and capacity settings</param>
        /// <param name="timeProvider">Clock used to age out remembered events</param>
        /// <param name="telemetry">PDP telemetry, for counting generations started early</param>
        public AuthorizationEventDuplicateTracker(IOptions<AuditLogDeduplicationSettings> settings, TimeProvider timeProvider, DecisionTelemetry telemetry)
        {
            long windowTicks = settings.Value.Window.Ticks;
            int maxTrackedEvents = Math.Max(settings.Value.MaxTrackedEvents, MaxKeysPerEvent);
            _observed = new Generations(windowTicks, maxTrackedEvents);
            _confirmed = new Generations(windowTicks, maxTrackedEvents);
            _timeProvider = timeProvider;
            _telemetry = telemetry;
        }

        /// <summary>
        /// Classifies the event against those seen within the window, and remembers it.
        /// </summary>
        /// <param name="authorizationEvent">The event about to be queued for the audit log</param>
        /// <param name="resourceInstanceIds">The resource instance ids of the request the event is for</param>
        /// <returns>Whether, and how, the event repeats one already seen</returns>
        public AuthorizationEventDuplicateKind Track(AuthorizationEvent authorizationEvent, IReadOnlyList<string> resourceInstanceIds) =>
            ClassifyCore(_observed, authorizationEvent, resourceInstanceIds, remember: true).Kind;

        /// <summary>
        /// Classifies the event against those remembered within the window, without remembering it. Used
        /// when duplicates are dropped: the event is remembered with <see cref="Remember"/> only once it
        /// has been queued, so that a send that fails never makes a later event look like a duplicate.
        /// Only events remembered that way count, not those seen by <see cref="Track"/>.
        /// </summary>
        /// <param name="authorizationEvent">The event about to be queued for the audit log</param>
        /// <param name="resourceInstanceIds">The resource instance ids of the request the event is for</param>
        /// <returns>The classification, to pass to <see cref="Remember"/></returns>
        public AuthorizationEventClassification Classify(AuthorizationEvent authorizationEvent, IReadOnlyList<string> resourceInstanceIds) =>
            ClassifyCore(_confirmed, authorizationEvent, resourceInstanceIds, remember: false);

        /// <summary>
        /// Remembers a classified event from the start of its window: when it was classified, or, for a
        /// repeat from another trace, when the event it repeats was first seen. The window therefore runs
        /// from when the event happened rather than from when it was queued.
        /// </summary>
        /// <param name="classification">The classification returned by <see cref="Classify"/></param>
        public void Remember(AuthorizationEventClassification classification)
        {
            if (classification.Kind == AuthorizationEventDuplicateKind.SameTrace)
            {
                // Both keys are already remembered.
                return;
            }

            long now = _timeProvider.GetUtcNow().UtcTicks;
            bool rotatedEarly;

            lock (_gate)
            {
                rotatedEarly = _confirmed.RotateIfNeeded(now);
                _confirmed.AddIfNotSeen(classification.TraceKey, classification.WindowStartTicks, now);

                if (classification.Kind == AuthorizationEventDuplicateKind.None)
                {
                    _confirmed.AddIfNotSeen(classification.EventKey, classification.WindowStartTicks, now);
                }
            }

            if (rotatedEarly)
            {
                _telemetry.RecordAuditLogTrackerCapacityRotation();
            }
        }

        private AuthorizationEventClassification ClassifyCore(Generations generations, AuthorizationEvent authorizationEvent, IReadOnlyList<string> resourceInstanceIds, bool remember)
        {
            (ulong eventKey, ulong traceKey) = ComputeKeys(authorizationEvent, resourceInstanceIds);
            long now = _timeProvider.GetUtcNow().UtcTicks;
            bool rotatedEarly;
            AuthorizationEventDuplicateKind duplicateKind;
            long windowStart;

            lock (_gate)
            {
                // Rotating before the lookups leaves room for every key this call can store, including
                // keys copied from the previous generation.
                rotatedEarly = generations.RotateIfNeeded(now);

                if (generations.TryGetSeen(traceKey, now, out long traceSeen))
                {
                    duplicateKind = AuthorizationEventDuplicateKind.SameTrace;
                    windowStart = traceSeen;
                }
                else
                {
                    // A repeat from another trace belongs to the window of the event it repeats, so its
                    // trace is remembered from that event's time: further repeats in the trace are
                    // duplicates only for as long as that event is.
                    bool seenInWindow = generations.TryGetSeen(eventKey, now, out long eventSeen);
                    windowStart = seenInWindow ? eventSeen : now;

                    if (remember)
                    {
                        generations.Set(traceKey, windowStart);

                        if (!seenInWindow)
                        {
                            generations.Set(eventKey, now);
                        }
                    }

                    duplicateKind = seenInWindow ? AuthorizationEventDuplicateKind.Window : AuthorizationEventDuplicateKind.None;
                }
            }

            if (rotatedEarly)
            {
                _telemetry.RecordAuditLogTrackerCapacityRotation();
            }

            return new AuthorizationEventClassification(duplicateKind, eventKey, traceKey, windowStart);
        }

        private static (ulong EventKey, ulong TraceKey) ComputeKeys(AuthorizationEvent authorizationEvent, IReadOnlyList<string> resourceInstanceIds)
        {
            ArrayBufferWriter<byte> buffer = KeyBuffer.Value!;
            buffer.ResetWrittenCount();

            // Ids are never 0, and null and empty strings mean the same here, so neither needs a marker of
            // its own. The decision does: Permit is 0.
            WriteString(buffer, authorizationEvent.Resource);
            WriteString(buffer, authorizationEvent.InstanceId);

            // The count keeps the list apart from the fields that follow it.
            WriteInt(buffer, resourceInstanceIds.Count);
            for (int i = 0; i < resourceInstanceIds.Count; i++)
            {
                WriteString(buffer, resourceInstanceIds[i]);
            }

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

        /// <summary>
        /// Keys and the time their window started, in two generations. Not thread-safe; the tracker's lock
        /// guards it. The dictionaries are allocated on first use, so a state that is never used costs
        /// nothing.
        /// </summary>
        private sealed class Generations(long windowTicks, int maxEntries)
        {
            private Dictionary<ulong, long> _current = [];
            private Dictionary<ulong, long> _previous = [];

            // DateTime.MinValue, so that the first event starts a generation.
            private long _currentStartedTicks;

            public bool RotateIfNeeded(long now)
            {
                if (now - _currentStartedTicks >= windowTicks)
                {
                    Rotate(now);
                    return false;
                }

                if (_current.Count + MaxKeysPerEvent > maxEntries)
                {
                    Rotate(now);
                    return true;
                }

                return false;
            }

            public bool TryGetSeen(ulong key, long now, out long windowStart)
            {
                // Keys keep the time their window started, also when copied from the previous generation,
                // so entries in either generation may have aged out.
                if (_current.TryGetValue(key, out windowStart))
                {
                    return now - windowStart < windowTicks;
                }

                if (_previous.TryGetValue(key, out windowStart) && now - windowStart < windowTicks)
                {
                    // Copy the key into the current generation, so that a key that keeps repeating survives
                    // the next rotation. The original time keeps its window from being extended.
                    _current[key] = windowStart;
                    return true;
                }

                return false;
            }

            public void Set(ulong key, long windowStart) => _current[key] = windowStart;

            public void AddIfNotSeen(ulong key, long windowStart, long now)
            {
                // A key remembered meanwhile, by a concurrent event, keeps its earlier time.
                if (!TryGetSeen(key, now, out _))
                {
                    _current[key] = windowStart;
                }
            }

            private void Rotate(long now)
            {
                // The oldest generation is dropped. After a rotation on time, everything in it is older than
                // the window. After a rotation because the current generation was full, it may still hold
                // events within their window, which are forgotten early. Its dictionary is reused for the new
                // generation, and sized for the maximum the first time, so it never grows while in use.
                (_previous, _current) = (_current, _previous);
                _current.Clear();
                _current.EnsureCapacity(maxEntries);
                _currentStartedTicks = now;
            }
        }
    }
}
