namespace Epoch.Core.StateMachine
{
    /// <summary>
    /// The 24-state run state machine, Core Spec sec.3.1. Numeric values are the
    /// spec's S-codes and are part of the serialized authoritative state, so they
    /// must never be renumbered without a rulesVersion change.
    /// </summary>
    public enum RunPhase
    {
        RUN_INIT = 0,
        SEED_INIT = 1,
        SNAPSHOT_LOAD = 2,
        LANE_MODIFIER_ASSIGN = 3,
        RUN_READY = 4,

        TURN_START = 10,
        CARD_GENERATION = 11,
        CHOICE_WINDOW = 12,
        ACTION_VALIDATION = 13,
        COST_PAYMENT = 14,
        CARD_EFFECT_APPLY = 15,
        INCOME = 16,
        MOVEMENT = 17,
        CONTESTED_RESOLUTION = 18,
        COMBAT = 19,
        SCORE_UPDATE = 20,
        AGE_TRANSITION_CHECK = 21,
        TURN_COMPLETE = 22,

        MATCH_COMPLETE = 30,
        REPLAY_RECORD_CREATE = 31,
        RUN_FINALIZED = 99,
    }
}
