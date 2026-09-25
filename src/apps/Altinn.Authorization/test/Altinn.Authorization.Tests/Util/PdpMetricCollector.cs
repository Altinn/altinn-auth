using System.Diagnostics.Metrics;
using Altinn.Platform.Authorization.Telemetry;

namespace Altinn.Authorization.Tests.Util
{
    /// <summary>
    /// Captures the tags of every measurement on one instrument of the PDP meter.
    /// The instrument is resolved through the given <see cref="IMeterFactory"/>; since the
    /// factory caches meters by name, this is the very same <see cref="Meter"/> instance
    /// <see cref="DecisionTelemetry"/> records on, and a different host (other test class)
    /// has a different instance.
    /// </summary>
    public sealed class PdpMetricCollector : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly List<IReadOnlyDictionary<string, object?>> _measurements = [];
        private readonly object _gate = new();

        public PdpMetricCollector(IMeterFactory meterFactory, string instrumentName)
        {
            Meter meter = meterFactory.Create(DecisionTelemetry.MeterName);

            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (ReferenceEquals(instrument.Meter, meter)
                        && instrument.Name == instrumentName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            {
                Dictionary<string, object?> snapshot = new(tags.Length);
                foreach (KeyValuePair<string, object?> tag in tags)
                {
                    snapshot[tag.Key] = tag.Value;
                }

                lock (_gate)
                {
                    _measurements.Add(snapshot);
                }
            });

            // Start() also replays already-published instruments, so this works
            // whether or not the DecisionTelemetry singleton was constructed by
            // an earlier request on this shared host.
            _listener.Start();
        }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Measurements
        {
            get
            {
                lock (_gate)
                {
                    return _measurements.ToArray();
                }
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
