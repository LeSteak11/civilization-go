namespace Epoch.Core.Domain
{
    /// <summary>Core Spec sec.5.1. Terminology is locked (Core Spec sec.0.3) and must not be renamed.</summary>
    public enum Side
    {
        PLAYER,
        SNAPSHOT,
    }

    public enum CardType
    {
        BUILD,
        TRAIN,
        ADVANCE,
    }

    public enum PerkArchetype
    {
        ECONOMIC,
        MILITARY,
        TEMPO,
        CONVERSION,
        KEYSTONE,
    }

    public enum UnitClass
    {
        SWORD,
        SPEAR,
        HORSE,
    }

    public enum LaneModifier
    {
        RIVER,
        HIGHLAND,
        COAST,
    }

    /// <summary>A = left, B = centre, C = right (MR sec.1.3).</summary>
    public enum LaneId
    {
        A,
        B,
        C,
    }

    public enum ResourceType
    {
        GROWTH,
        INSIGHT,
    }

    public enum MatchOutcome
    {
        VICTORY,
        DEFEAT,
        TIE,
    }

    /// <summary>Core Spec sec.5.13.</summary>
    public enum EffectSourceType
    {
        LANE_MODIFIER,
        STRUCTURE,
        PERK,
        COMMANDER,
    }

    public enum EffectTrigger
    {
        ON_INCOME,
        ON_MOVEMENT,
        ON_COMBAT_PRE,
        ON_COMBAT_POST,
        ON_TURN_END,
        CONTINUOUS,
    }

    public enum EffectScope
    {
        GLOBAL,
        LANE,
        UNIT_CLASS,
        SIDE,
    }

    public enum EffectOperation
    {
        ADD,
        MULTIPLY,
    }
}
