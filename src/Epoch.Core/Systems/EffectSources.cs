using System;
using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Effects;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Collects the effects a side currently owns, from every source that can carry one.
    ///
    /// <para>M1 gathered from perks only, with a side-wide scope context. That silently
    /// dropped every lane-scoped income perk and every structure-borne effect: the cards
    /// loaded, read as if they did something, and did nothing. M2's coverage gate exists
    /// to make that class of failure impossible to ship, and this class is where the
    /// missing sources are actually read.</para>
    ///
    /// <para>Effects are gathered by <b>application point</b> rather than by scope, because
    /// the same target stat means different things in different places: a UNIT_CLASS Power
    /// bonus is per unit and pre-stacking, while a LANE Power bonus is one addition to the
    /// side's post-stacking lane total.</para>
    /// </summary>
    public static class EffectSources
    {
        /// <summary>
        /// Side-wide income effects for one resource: the SIDE-scoped flat changes plus
        /// the lane-scoped ones the side currently qualifies for.
        ///
        /// A lane-scoped income effect requires the owner to hold at least one structure
        /// in a lane carrying that modifier - the authored rules text for all four such
        /// perks says exactly that, and a lane reaches a side's income only through a
        /// structure.
        /// </summary>
        public static List<ActiveEffect> IncomeEffects(RunState state, PlayerState side, ResourceType resource)
        {
            string stat = resource == ResourceType.GROWTH
                ? EffectCatalog.GrowthPerTurn
                : EffectCatalog.InsightPerTurn;

            List<ActiveEffect> gathered = new List<ActiveEffect>();

            for (int p = 0; p < side.Perks.Count; p++)
            {
                IReadOnlyList<ActiveEffect> effects = side.Perks[p].Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (!Matches(effect, stat, EffectTrigger.ON_INCOME))
                    {
                        continue;
                    }

                    if (effect.Scope == EffectScope.SIDE || effect.Scope == EffectScope.GLOBAL)
                    {
                        gathered.Add(effect);
                        continue;
                    }

                    if (effect.Scope == EffectScope.LANE &&
                        TryModifierFromScope(effect.ScopeValue, out LaneModifier modifier) &&
                        HasStructureInLaneWithModifier(state, side, modifier))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            return gathered;
        }

        /// <summary>Multipliers applied to one structure's yield as income counts it.</summary>
        public static List<ActiveEffect> StructureYieldEffects(PlayerState side, ResourceType resource)
        {
            string stat = resource == ResourceType.GROWTH
                ? EffectCatalog.StructureGrowthYield
                : EffectCatalog.StructureInsightYield;

            return SideScoped(side, stat, EffectTrigger.ON_INCOME);
        }

        /// <summary>Multipliers applied to a held contested tile's yield.</summary>
        public static List<ActiveEffect> ContestedYieldEffects(PlayerState side, ResourceType resource)
        {
            string stat = resource == ResourceType.GROWTH
                ? EffectCatalog.ContestedTileGrowthYield
                : EffectCatalog.ContestedTileInsightYield;

            return SideScoped(side, stat, EffectTrigger.ON_INCOME);
        }

        /// <summary>
        /// Per-unit Power effects, applied before stack ordering and the stack multipliers
        /// (owner-confirmed decision L). Only UNIT_CLASS and GLOBAL scopes belong here:
        /// a lane-scoped bonus applied per unit would be multiplied by the stack size and
        /// then scaled by 1.00/0.75/0.50, which is not what "add N to lane Power" means.
        /// </summary>
        public static List<ActiveEffect> UnitPowerEffects(PlayerState side, UnitInstance unit)
        {
            List<ActiveEffect> gathered = new List<ActiveEffect>();

            for (int p = 0; p < side.Perks.Count; p++)
            {
                IReadOnlyList<ActiveEffect> effects = side.Perks[p].Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (!Matches(effect, EffectCatalog.EffectivePower, EffectTrigger.ON_COMBAT_PRE))
                    {
                        continue;
                    }

                    if (effect.Scope == EffectScope.GLOBAL)
                    {
                        gathered.Add(effect);
                        continue;
                    }

                    if (effect.Scope == EffectScope.UNIT_CLASS && MatchesUnit(effect.ScopeValue, unit))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            return gathered;
        }

        /// <summary>
        /// Lane Power effects: added once to the side's post-stacking effective Power at a
        /// combat site in this lane.
        ///
        /// Three sources contribute. A SIDE-scoped perk applies in every lane the side
        /// fights in ("multiply the owner's effective lane Power by 1.10"). A perk carrying
        /// a modifier scope, or the previous-turn-holder scope, applies in any lane that
        /// qualifies. A structure's own effect applies only in the lane it was built in,
        /// and applies once **per structure**, because structures have no cap and each one
        /// is its own source.
        /// </summary>
        public static List<ActiveEffect> LanePowerEffects(PlayerState side, LaneState lane)
        {
            List<ActiveEffect> gathered = new List<ActiveEffect>();

            for (int p = 0; p < side.Perks.Count; p++)
            {
                IReadOnlyList<ActiveEffect> effects = side.Perks[p].Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (!Matches(effect, EffectCatalog.EffectivePower, EffectTrigger.ON_COMBAT_PRE))
                    {
                        continue;
                    }

                    if (effect.Scope == EffectScope.SIDE || effect.Scope == EffectScope.GLOBAL)
                    {
                        gathered.Add(effect);
                        continue;
                    }

                    if (effect.Scope == EffectScope.LANE &&
                        LaneScopeApplies(effect.ScopeValue, side, lane, structureLane: null))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            for (int s = 0; s < side.Structures.Count; s++)
            {
                StructureInstance structure = side.Structures[s];
                IReadOnlyList<ActiveEffect> effects = structure.Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (!Matches(effect, EffectCatalog.EffectivePower, EffectTrigger.ON_COMBAT_PRE))
                    {
                        continue;
                    }

                    if (effect.Scope == EffectScope.LANE &&
                        LaneScopeApplies(effect.ScopeValue, side, lane, structure.LaneId))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            return gathered;
        }

        private static bool LaneScopeApplies(string? scopeValue, PlayerState side, LaneState lane, LaneId? structureLane)
        {
            switch (scopeValue)
            {
                case EffectCatalog.ScopeRiver:
                    return lane.Modifier == LaneModifier.RIVER;
                case EffectCatalog.ScopeHighland:
                    return lane.Modifier == LaneModifier.HIGHLAND;
                case EffectCatalog.ScopeCoast:
                    return lane.Modifier == LaneModifier.COAST;
                case EffectCatalog.ScopeOwnerHoldsContestedTile:
                    return lane.Tile3HolderPrevTurn == side.Side;
                case EffectCatalog.ScopeTargetLane:
                    // Only meaningful for a source that has a lane of its own.
                    return structureLane is not null && structureLane.Value == lane.Id;
                default:
                    return false;
            }
        }

        private static List<ActiveEffect> SideScoped(PlayerState side, string stat, EffectTrigger trigger)
        {
            List<ActiveEffect> gathered = new List<ActiveEffect>();
            for (int p = 0; p < side.Perks.Count; p++)
            {
                IReadOnlyList<ActiveEffect> effects = side.Perks[p].Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (Matches(effect, stat, trigger) &&
                        (effect.Scope == EffectScope.SIDE || effect.Scope == EffectScope.GLOBAL))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            return gathered;
        }

        private static bool Matches(ActiveEffect effect, string stat, EffectTrigger trigger) =>
            effect.Trigger == trigger && string.Equals(effect.TargetStat, stat, StringComparison.Ordinal);

        private static bool MatchesUnit(string? scopeValue, UnitInstance unit)
        {
            switch (scopeValue)
            {
                case EffectCatalog.ScopeSword:
                    return unit.UnitClass == UnitClass.SWORD;
                case EffectCatalog.ScopeSpear:
                    return unit.UnitClass == UnitClass.SPEAR;
                case EffectCatalog.ScopeHorse:
                    return unit.UnitClass == UnitClass.HORSE;
                case EffectCatalog.ScopeReach:
                    return unit.HasReach;
                default:
                    return false;
            }
        }

        private static bool TryModifierFromScope(string? scopeValue, out LaneModifier modifier)
        {
            switch (scopeValue)
            {
                case EffectCatalog.ScopeRiver:
                    modifier = LaneModifier.RIVER;
                    return true;
                case EffectCatalog.ScopeHighland:
                    modifier = LaneModifier.HIGHLAND;
                    return true;
                case EffectCatalog.ScopeCoast:
                    modifier = LaneModifier.COAST;
                    return true;
                default:
                    modifier = LaneModifier.RIVER;
                    return false;
            }
        }

        private static bool HasStructureInLaneWithModifier(RunState state, PlayerState side, LaneModifier modifier)
        {
            for (int i = 0; i < side.Structures.Count; i++)
            {
                if (state.Lane(side.Structures[i].LaneId).Modifier == modifier)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
