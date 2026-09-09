using System.Collections.Generic;
using System.Text;
using Epoch.Core.Domain;
using Epoch.Core.Numerics;
using Epoch.Core.StateMachine;

namespace Epoch.Core.Events
{
    /// <summary>
    /// A concrete ordered record of something the Core did (Core Spec sec.5.16).
    ///
    /// The payload is a canonical, ordinal, culture-free key=value string rather than a
    /// typed object graph. That keeps the event stream directly hashable and diffable
    /// against the Node oracle's fixtures without a serializer, which is what the
    /// oracle-comparison contract needs. Richer typed payloads are a presentation
    /// concern and belong with the UI milestone, not the authoritative path.
    ///
    /// <see cref="SimulationEvent.Phase"/> is the phase the event was emitted in, so a
    /// replay can be stepped one canonical S-code at a time.
    /// </summary>
    public sealed record RecordedEvent(
        int Sequence,
        int Turn,
        RunPhase Phase,
        Side? Side,
        SimulationEventType EventType,
        string Payload)
        : SimulationEvent(Sequence, Turn, Phase, Side, EventType)
    {
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.Append(FixedValue.IntToString(Sequence));
            text.Append('|');
            text.Append(FixedValue.IntToString(Turn));
            text.Append('|');
            text.Append(Phase.ToString());
            text.Append('|');
            text.Append(Side is null ? "-" : Side.Value.ToString());
            text.Append('|');
            text.Append(EventType.ToString());
            text.Append('|');
            text.Append(Payload);
            return text.ToString();
        }
    }

    /// <summary>
    /// Builds the ordered event list for one turn. Sequence numbers are monotonic from 0
    /// across the whole run, so an event's position is itself part of the contract.
    /// </summary>
    public sealed class EventLog
    {
        private readonly List<SimulationEvent> _events = new List<SimulationEvent>();
        private int _sequence;

        public EventLog(int startingSequence)
        {
            _sequence = startingSequence;
        }

        public int NextSequence => _sequence;

        public IReadOnlyList<SimulationEvent> Events => _events;

        public void Add(int turn, RunPhase phase, Side? side, SimulationEventType type, string payload)
        {
            _events.Add(new RecordedEvent(_sequence, turn, phase, side, type, payload));
            _sequence++;
        }
    }

    /// <summary>Canonical, culture-free payload assembly. Ordinal throughout.</summary>
    public sealed class PayloadBuilder
    {
        private readonly StringBuilder _text = new StringBuilder();

        public PayloadBuilder Add(string key, string value)
        {
            if (_text.Length > 0)
            {
                _text.Append(';');
            }

            _text.Append(key).Append('=').Append(value);
            return this;
        }

        public PayloadBuilder Add(string key, int value) => Add(key, FixedValue.IntToString(value));

        public PayloadBuilder Add(string key, bool value) => Add(key, value ? "1" : "0");

        public override string ToString() => _text.ToString();
    }
}
