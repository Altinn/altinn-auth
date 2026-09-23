#nullable enable

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
    /// Events are stored as a 128-bit prefix of a SHA-256 hash in two generations of at most
    /// <see cref="AuditLogDeduplicationSettings.MaxTrackedEvents"/> entries each. A generation is
    /// discarded once everything in it is older than the window, so memory stays bounded without a
    /// background timer.
    /// </para>
    /// </remarks>
    public sealed class AuthorizationEventDuplicateTracker
    {
        private readonly object _gate = new();
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _window;
        private readonly int _maxTrackedEvents;

        private Dictionary<UInt128, DateTimeOffset> _current = [];
        private Dictionary<UInt128, DateTimeOffset> _previous = [];
        private DateTimeOffset _currentStarted = DateTimeOffset.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationEventDuplicateTracker"/> class.
        /// </summary>
        /// <param name="settings">Window and capacity settings</param>
        /// <param name="timeProvider">Clock used to age out remembered events</param>
        public AuthorizationEventDuplicateTracker(IOptions<AuditLogDeduplicationSettings> settings, TimeProvider timeProvider)
        {
            _window = settings.Value.Window;
            _maxTrackedEvents = settings.Value.MaxTrackedEvents;
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Classifies the event against those seen within the window, and remembers it.
        /// </summary>
        /// <param name="authorizationEvent">The event about to be queued for the audit log</param>
        /// <returns>Whether, and how, the event repeats one already seen</returns>
        public AuthorizationEventDuplicateKind Track(AuthorizationEvent authorizationEvent)
        {
            (UInt128 eventKey, UInt128 traceKey) = ComputeKeys(authorizationEvent);
            DateTimeOffset now = _timeProvider.GetUtcNow();

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

        private void RotateIfExpired(DateTimeOffset now)
        {
            if (now - _currentStarted < _window)
            {
                return;
            }

            // The previous generation only holds events from before the current one started, at least a
            // window ago, so it can be dropped. Its dictionary is reused for the new generation.
            (_previous, _current) = (_current, _previous);
            _current.Clear();
            _currentStarted = now;
        }

        private bool IsSeen(UInt128 key, DateTimeOffset now)
        {
            // Entries in the current generation are always within the window. Entries in the previous
            // generation may have aged out.
            return _current.ContainsKey(key)
                || (_previous.TryGetValue(key, out DateTimeOffset seen) && now - seen < _window);
        }

        private static (UInt128 EventKey, UInt128 TraceKey) ComputeKeys(AuthorizationEvent authorizationEvent)
        {
            // Hashed in one go rather than incrementally: a single native call per key is several times
            // cheaper than one per field.
            ArrayBufferWriter<byte> buffer = new(512);

            WriteString(buffer, authorizationEvent.Resource);
            WriteString(buffer, authorizationEvent.InstanceId);
            WriteInt(buffer, authorizationEvent.ResourcePartyId);
            WriteInt(buffer, authorizationEvent.SubjectUserId);
            WriteInt(buffer, authorizationEvent.SubjectParty);
            WriteString(buffer, authorizationEvent.SubjectPartyUuid);
            WriteString(buffer, authorizationEvent.SubjectOrgCode);
            WriteInt(buffer, authorizationEvent.SubjectOrgNumber);
            WriteString(buffer, authorizationEvent.SessionId);
            WriteString(buffer, authorizationEvent.IpAdress);
            WriteString(buffer, authorizationEvent.Operation);
            WriteInt(buffer, (int?)authorizationEvent.Decision);
            int eventLength = buffer.WrittenCount;

            // The trace key is the event key input followed by the trace id.
            WriteString(buffer, authorizationEvent.TraceId);

            Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(buffer.WrittenSpan[..eventLength], digest);
            UInt128 eventKey = BinaryPrimitives.ReadUInt128LittleEndian(digest);

            SHA256.HashData(buffer.WrittenSpan, digest);
            UInt128 traceKey = BinaryPrimitives.ReadUInt128LittleEndian(digest);

            return (eventKey, traceKey);
        }

        private static void WriteString(ArrayBufferWriter<byte> buffer, string? value)
        {
            // The length prefix keeps adjacent fields apart ("ab" + "c" differs from "a" + "bc"), and
            // separates null from the empty string.
            WriteInt(buffer, value?.Length);
            if (!string.IsNullOrEmpty(value))
            {
                buffer.Write(MemoryMarshal.AsBytes(value.AsSpan()));
            }
        }

        private static void WriteInt(ArrayBufferWriter<byte> buffer, int? value)
        {
            Span<byte> span = buffer.GetSpan(1 + sizeof(int));
            span[0] = value.HasValue ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteInt32LittleEndian(span[1..], value.GetValueOrDefault());
            buffer.Advance(1 + sizeof(int));
        }
    }
}
