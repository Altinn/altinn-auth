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
        /// <param name="telemetry">PDP telemetry, for counting generations started early</param>
        public AuthorizationEventDuplicateTracker(IOptions<AuditLogDeduplicationSettings> settings, TimeProvider timeProvider, DecisionTelemetry telemetry)
        {
            _windowTicks = settings.Value.Window.Ticks;
            _maxTrackedEvents = Math.Max(settings.Value.MaxTrackedEvents, MaxKeysPerEvent);
            _timeProvider = timeProvider;
            _telemetry = telemetry;
        }

        /// <summary>
        /// Classifies the event against those seen within the window, and remembers it.
        /// </summary>
        /// <param name="authorizationEvent">The event about to be queued for the audit log</param>
        /// <param name="resourceInstanceIds">The resource instance ids of the request the event is for</param>
        /// <returns>Whether, and how, the event repeats one already seen</returns>
        public AuthorizationEventDuplicateKind Track(AuthorizationEvent authorizationEvent, IReadOnlyList<string> resourceInstanceIds)
        {
            (ulong eventKey, ulong traceKey) = ComputeKeys(authorizationEvent, resourceInstanceIds);
            long now = _timeProvider.GetUtcNow().UtcTicks;
            bool rotatedEarly;
            AuthorizationEventDuplicateKind duplicateKind;

            lock (_gate)
            {
                // Rotating before the lookups leaves room for every key this call can store, including
                // keys copied from the previous generation.
                rotatedEarly = RotateIfNeeded(now);

                if (IsSeen(traceKey, now))
                {
                    duplicateKind = AuthorizationEventDuplicateKind.SameTrace;
                }
                else
                {
                    bool seenInWindow = IsSeen(eventKey, now);

                    // Remember the trace even when the event was seen in another trace, so that further
                    // repeats in this trace are classified as SameTrace.
                    _current[traceKey] = now;

                    if (seenInWindow)
                    {
                        duplicateKind = AuthorizationEventDuplicateKind.Window;
                    }
                    else
                    {
                        _current[eventKey] = now;
                        duplicateKind = AuthorizationEventDuplicateKind.None;
                    }
                }
            }

            if (rotatedEarly)
            {
                _telemetry.RecordAuditLogTrackerCapacityRotation();
            }

            return duplicateKind;
        }

        private bool RotateIfNeeded(long now)
        {
            if (now - _currentStartedTicks >= _windowTicks)
            {
                Rotate(now);
                return false;
            }

            if (_current.Count + MaxKeysPerEvent > _maxTrackedEvents)
            {
                Rotate(now);
                return true;
            }

            return false;
        }

        private void Rotate(long now)
        {
            // The oldest generation is dropped. After a rotation on time, everything in it is older than
            // the window. After a rotation because the current generation was full, it may still hold
            // events within their window, which are forgotten early. Its dictionary is reused for the new
            // generation, and sized for the maximum the first time, so it never grows while in use.
            (_previous, _current) = (_current, _previous);
            _current.Clear();
            _current.EnsureCapacity(_maxTrackedEvents);
            _currentStartedTicks = now;
        }

        private bool IsSeen(ulong key, long now)
        {
            // Keys copied from the previous generation keep the time they were first seen, so entries in
            // either generation may have aged out.
            if (_current.TryGetValue(key, out long seen))
            {
                return now - seen < _windowTicks;
            }

            if (_previous.TryGetValue(key, out seen) && now - seen < _windowTicks)
            {
                // Copy the key into the current generation, so that a key that keeps repeating survives
                // the next rotation. The original time keeps its window from being extended.
                _current[key] = seen;
                return true;
            }

            return false;
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
    }
}
