using Epoch.Core.Domain;
using Epoch.Core.StateMachine;

namespace Epoch.Core.Events
{
    /// <summary>Core Spec sec.5.16 ReplayEvent.eventType.</summary>
    public enum SimulationEventType
    {
        OFFER_GENERATED,
        CARD_SELECTED,
        COST_PAID,
        EFFECT_APPLIED,
        INCOME_GRANTED,
        UNIT_MOVED,
        OWNERSHIP_CHANGED,
        COMBAT_RESOLVED,
        UNIT_DESTROYED,
        SCORE_CHANGED,
        AGE_ADVANCED,
        TURN_COMPLETED,
    }

    /// <summary>
    /// One ordered, immutable record of something the Core did. Presentation animates
    /// events; it never calls a gameplay system (Technical Plan sec.8).
    /// The payload shape is per event type and is defined in M1.
    /// </summary>
    public abstract record SimulationEvent(
        int Sequence,
        int Turn,
        RunPhase Phase,
        Side? Side,
        SimulationEventType EventType);
}
