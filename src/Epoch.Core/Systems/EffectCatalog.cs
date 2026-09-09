using System;
using System.Collections.Generic;
using Epoch.Core.Domain;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Where an effect is applied, which decides what its scope can mean.
    /// </summary>
    public enum EffectApplication
    {
        /// <summary>Added to a side's per-turn income for one resource (step 7).</summary>
        SideIncome,

        /// <summary>Applied to one structure's yield as income counts it.</summary>
        StructureYield,

        /// <summary>Applied to the per-lane contested-tile yield at income.</summary>
        ContestedYield,

        /// <summary>Applied to one unit's Power before stack ordering (C2, decision L).</summary>
        UnitPower,

        /// <summary>Applied to a side's post-stacking effective Power at a combat site, before the counter at C4.</summary>
        LanePower,
    }

    /// <summary>
    /// The authoritative list of effect handlers this engine implements.
    ///
    /// <para>This is the contract the content validator checks every authored effect
    /// against. The Technical Plan makes it a mandatory gate - "Exhaustive
    /// content-to-handler validation fails build on uncovered effect" (sec.14) - because an
    /// effect the engine silently ignores produces a card that reads as if it does
    /// something and does nothing. That failure is invisible in play, invisible in a
    /// replay, and invisible in a balance report, so a build-time gate is the only place
    /// it can be caught. M2 found four such cards this way.</para>
    ///
    /// <para>A row is a <b>handler</b>, not a single combination: one row carries the set
    /// of scope values it accepts, because the engine's lane-scoped income handling is one
    /// code path parameterised by modifier rather than three separate paths. Reverse
    /// coverage therefore asserts that every <i>row</i> is exercised by the authored pool -
    /// a handler nothing uses is an untested code path claiming to be supported - without
    /// forcing content to author every parameter value of every row.</para>
    /// </summary>
    public static class EffectCatalog
    {
        /// <summary>Target stats the engine reads at income.</summary>
        public const string GrowthPerTurn = "growthPerTurn";

        public const string InsightPerTurn = "insightPerTurn";

        public const string StructureGrowthYield = "structureGrowthYield";

        public const string StructureInsightYield = "structureInsightYield";

        public const string ContestedTileGrowthYield = "contestedTileGrowthYield";

        public const string ContestedTileInsightYield = "contestedTileInsightYield";

        /// <summary>The one stat the engine reads in combat.</summary>
        public const string EffectivePower = "effectivePower";

        /// <summary>Authored scope values, as the V1 content manifest spells them.</summary>
        public const string ScopeOwner = "OWNER";

        public const string ScopeRiver = "RIVER";

        public const string ScopeHighland = "HIGHLAND";

        public const string ScopeCoast = "COAST";

        public const string ScopeOwnerHoldsContestedTile = "OWNER_HOLDS_CONTESTED_TILE";

        public const string ScopeTargetLane = "TARGET_LANE";

        public const string ScopeSword = "SWORD";

        public const string ScopeSpear = "SPEAR";

        public const string ScopeHorse = "HORSE";

        public const string ScopeReach = "REACH";

        private static readonly string[] Owner = { ScopeOwner };

        private static readonly string[] LaneModifiers = { ScopeRiver, ScopeHighland, ScopeCoast };

        private static readonly string[] UnitClasses = { ScopeSword, ScopeSpear, ScopeHorse, ScopeReach };

        private static readonly string[] LanePowerScopes =
        {
            ScopeRiver, ScopeHighland, ScopeCoast, ScopeOwnerHoldsContestedTile, ScopeTargetLane,
        };

        private static readonly SupportedEffect[] Supported =
        {
            // ---- Income ----------------------------------------------------------
            // Side-wide flat or multiplicative change to a resource's per-turn income.
            new SupportedEffect(GrowthPerTurn, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.SideIncome),
            new SupportedEffect(InsightPerTurn, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.SideIncome),

            // Lane-scoped income: granted when the owner holds at least one structure in a
            // lane carrying that modifier. A lane reaches a side's income only through a
            // structure, which is what the four authored perks' rules text says.
            new SupportedEffect(GrowthPerTurn, EffectTrigger.ON_INCOME, EffectScope.LANE, LaneModifiers, EffectApplication.SideIncome),
            new SupportedEffect(InsightPerTurn, EffectTrigger.ON_INCOME, EffectScope.LANE, LaneModifiers, EffectApplication.SideIncome),

            // Structure and contested-tile yield modifiers.
            new SupportedEffect(StructureGrowthYield, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.StructureYield),
            new SupportedEffect(StructureInsightYield, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.StructureYield),
            new SupportedEffect(ContestedTileGrowthYield, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.ContestedYield),
            new SupportedEffect(ContestedTileInsightYield, EffectTrigger.ON_INCOME, EffectScope.SIDE, Owner, EffectApplication.ContestedYield),

            // ---- Combat ----------------------------------------------------------
            // Per unit, before stack ordering and the stack multipliers. Owner-confirmed
            // decision (L): these may change stack rank and the dominant class at C3.
            new SupportedEffect(EffectivePower, EffectTrigger.ON_COMBAT_PRE, EffectScope.UNIT_CLASS, UnitClasses, EffectApplication.UnitPower),

            // Applied once to the side's post-stacking effective Power at a combat site,
            // after the dominant class is chosen and before the counter at C4 - which is
            // exactly what the authored rules text asks for: "after all effective-Power ADD
            // effects, multiply the owner's effective lane Power ... before counter and
            // damage resolution".
            new SupportedEffect(EffectivePower, EffectTrigger.ON_COMBAT_PRE, EffectScope.SIDE, Owner, EffectApplication.LanePower),
            new SupportedEffect(EffectivePower, EffectTrigger.ON_COMBAT_PRE, EffectScope.LANE, LanePowerScopes, EffectApplication.LanePower),
        };

        public static IReadOnlyList<SupportedEffect> All => Supported;

        /// <summary>
        /// Is this combination executable? The validator calls this for every authored
        /// effect and refuses the content when the answer is no.
        /// </summary>
        public static bool IsSupported(string targetStat, EffectTrigger trigger, EffectScope scope, string? scopeValue) =>
            Find(targetStat, trigger, scope, scopeValue) is not null;

        public static SupportedEffect? Find(string targetStat, EffectTrigger trigger, EffectScope scope, string? scopeValue)
        {
            for (int i = 0; i < Supported.Length; i++)
            {
                SupportedEffect candidate = Supported[i];
                if (string.Equals(candidate.TargetStat, targetStat, StringComparison.Ordinal) &&
                    candidate.Trigger == trigger &&
                    candidate.Scope == scope &&
                    candidate.Accepts(scopeValue))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Where the engine applies a given authored combination.</summary>
        public static EffectApplication ApplicationOf(string targetStat, EffectTrigger trigger, EffectScope scope, string? scopeValue)
        {
            SupportedEffect? found = Find(targetStat, trigger, scope, scopeValue);
            if (found is null)
            {
                throw new InvalidOperationException(
                    "No handler is registered for " + targetStat + "/" + trigger + "/" + scope + "/" + scopeValue +
                    ". Content validation must reject this before a run is created.");
            }

            return found.Value.Application;
        }
    }

    /// <summary>One executable effect handler, with the scope values it accepts.</summary>
    public readonly struct SupportedEffect
    {
        private readonly string[] _scopeValues;

        public SupportedEffect(
            string targetStat,
            EffectTrigger trigger,
            EffectScope scope,
            string[] scopeValues,
            EffectApplication application)
        {
            TargetStat = targetStat;
            Trigger = trigger;
            Scope = scope;
            _scopeValues = scopeValues;
            Application = application;
        }

        public string TargetStat { get; }

        public EffectTrigger Trigger { get; }

        public EffectScope Scope { get; }

        public EffectApplication Application { get; }

        public IReadOnlyList<string> ScopeValues => _scopeValues;

        public bool Accepts(string? scopeValue)
        {
            if (scopeValue is null)
            {
                return false;
            }

            for (int i = 0; i < _scopeValues.Length; i++)
            {
                if (string.Equals(_scopeValues[i], scopeValue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A stable identity for a handler, used by the reverse-coverage check.</summary>
        public override string ToString() => TargetStat + "/" + Trigger + "/" + Scope;
    }
}
