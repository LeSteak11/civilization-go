using System;
using System.Collections.Generic;
using Epoch.Content.Json;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Systems;

namespace Epoch.Content.Validation
{
    /// <summary>
    /// A content document that parsed but breaks a content rule.
    /// Distinct from <see cref="ContentFormatException"/> so a caller can tell "this file
    /// is not JSON we understand" from "this file is well-formed and wrong".
    /// </summary>
    public sealed class ContentValidationException : Exception
    {
        public ContentValidationException(string problem)
            : base("Content validation failed: " + problem + ".")
        {
            Problem = problem;
        }

        public string Problem { get; }
    }

    /// <summary>
    /// Every content rule the engine relies on, checked once at load and never again.
    ///
    /// <para>The rule that matters most is <b>effect coverage</b>. The Technical Plan makes
    /// it a mandatory build gate - "Exhaustive content-to-handler validation fails build on
    /// uncovered effect" (sec.14) - because an effect the engine does not implement
    /// produces a card that reads as if it does something and does nothing. That failure
    /// is invisible in play, invisible in a replay, and invisible in a balance report; the
    /// only place it can be caught is here.</para>
    ///
    /// <para>Costs are never authored (Content Manifest sec.1): BUILD, TRAIN and ADVANCE
    /// all read the resolving Age. A numeric cost on a card is refused rather than
    /// ignored, because ignoring it would let a designer believe they had changed a price.</para>
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>The V1 roster shape (Content Manifest sec.4.1).</summary>
        private const int ExpectedBuilds = 4;

        private const int ExpectedTrains = 16;

        private const int ExpectedPerks = 24;

        private const int ExpectedKeystones = 4;

        /// <summary>Members that would mean a card had authored its own price.</summary>
        private static readonly string[] ForbiddenCostMembers =
        {
            "cost", "costGrowth", "costInsight", "costValue", "price", "buildCost", "unitCost", "perkCost",
        };

        public static void Validate(JsonValue root, IReadOnlyList<CardDefinition> cards)
        {
            ValidateRosterCounts(root);
            ValidateNoAuthoredCosts(root);
            ValidateAgeTableAgreesWithRules(root);
            ValidateOfferWeightsAgreeWithRules(root);

            ValidateUniqueIds(cards);
            ValidateCardShapes(cards);
            ValidateEffectIntegrity(cards);
            ValidateEffectCoverage(cards);
            ValidateOfferSufficiency(cards);
        }

        private static void ValidateRosterCounts(JsonValue root)
        {
            RequireCount(root, "builds", ExpectedBuilds);
            RequireCount(root, "trains", ExpectedTrains);
            RequireCount(root, "perks", ExpectedPerks);
            RequireCount(root, "keystones", ExpectedKeystones);
        }

        private static void RequireCount(JsonValue root, string arrayName, int expected)
        {
            int actual = root.Member(arrayName).Items.Count;
            if (actual != expected)
            {
                throw new ContentValidationException(
                    arrayName + " must hold " + expected + " entries but holds " + actual);
            }
        }

        private static void ValidateNoAuthoredCosts(JsonValue root)
        {
            foreach (string arrayName in new[] { "builds", "trains", "perks", "keystones" })
            {
                IReadOnlyList<JsonValue> entries = root.Member(arrayName).Items;
                for (int i = 0; i < entries.Count; i++)
                {
                    JsonValue entry = entries[i];
                    for (int f = 0; f < ForbiddenCostMembers.Length; f++)
                    {
                        if (entry.Has(ForbiddenCostMembers[f]))
                        {
                            throw new ContentValidationException(
                                "entry '" + entry.Member("id").AsString() + "' authors a numeric cost via '" +
                                ForbiddenCostMembers[f] + "'; costs are read from the resolving Age (MR sec.2.2)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The Age table in content must agree with the locked table compiled into the
        /// Core. Content is prototype-tunable; the Age table is not.
        /// </summary>
        private static void ValidateAgeTableAgreesWithRules(JsonValue root)
        {
            IReadOnlyList<JsonValue> rows = root.Member("ageTable").Items;
            if (rows.Count != RunState.AgeCount)
            {
                throw new ContentValidationException(
                    "ageTable must hold " + RunState.AgeCount + " rows but holds " + rows.Count);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                JsonValue row = rows[i];
                int age = row.Member("age").AsInt32();
                AgeRow locked = AgeTable.ForAge(age);

                RequireEquals(locked.PerkCost, row.Member("perkCost").AsInt32(), age, "perkCost");
                RequireEquals(locked.UnitPower, row.Member("unitPower").AsInt32(), age, "unitPower");
                RequireEquals(locked.UnitCost, row.Member("unitCost").AsInt32(), age, "unitCost");
                RequireEquals(locked.BuildCost, row.Member("buildCost").AsInt32(), age, "buildCost");
                RequireEquals(locked.StructureTier, row.Member("structureTier").AsInt32(), age, "structureTier");
                RequireEquals(locked.TierYield, row.Member("structureBaseYield").AsInt32(), age, "structureBaseYield");
            }
        }

        private static void RequireEquals(int locked, int authored, int age, string field)
        {
            if (locked != authored)
            {
                throw new ContentValidationException(
                    "ageTable Age " + age + " " + field + " is " + authored +
                    " but the locked rules say " + locked + "; the Age table may not be rebalanced (MR sec.2.2)");
            }
        }

        private static void ValidateOfferWeightsAgreeWithRules(JsonValue root)
        {
            IReadOnlyList<JsonValue> rows = root.Member("offerWeights").Items;
            OfferWeightBand[] locked = OfferWeights.AllBands();

            if (rows.Count != locked.Length)
            {
                throw new ContentValidationException(
                    "offerWeights must hold " + locked.Length + " bands but holds " + rows.Count);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                JsonValue row = rows[i];
                IReadOnlyList<JsonValue> turns = row.Member("turns").Items;
                int firstTurn = turns[0].AsInt32();
                int lastTurn = turns[1].AsInt32();

                OfferWeightBand band = locked[i];
                if (band.FirstTurn != firstTurn || band.LastTurn != lastTurn)
                {
                    throw new ContentValidationException(
                        "offerWeights band " + i + " covers turns " + firstTurn + "-" + lastTurn +
                        " but the locked table says " + band.FirstTurn + "-" + band.LastTurn);
                }

                if (band.Build != row.Member("BUILD").AsInt32() ||
                    band.Train != row.Member("TRAIN").AsInt32() ||
                    band.Advance != row.Member("ADVANCE").AsInt32())
                {
                    throw new ContentValidationException(
                        "offerWeights for turns " + firstTurn + "-" + lastTurn +
                        " disagree with the locked V1 effective table (Core Spec sec.2.6)");
                }
            }
        }

        private static void ValidateUniqueIds(IReadOnlyList<CardDefinition> cards)
        {
            HashSet<string> cardIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> effectIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (!cardIds.Add(card.CardId.Value))
                {
                    throw new ContentValidationException("duplicate card id '" + card.CardId.Value + "'");
                }

                for (int e = 0; e < card.Effects.Count; e++)
                {
                    string effectId = card.Effects[e].EffectId.Value;
                    if (!effectIds.Add(effectId))
                    {
                        throw new ContentValidationException("duplicate effect id '" + effectId + "'");
                    }
                }
            }
        }

        private static void ValidateCardShapes(IReadOnlyList<CardDefinition> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                string where = "card '" + card.CardId.Value + "'";

                if (card.MinAge < 1 || card.MaxAge > RunState.AgeCount || card.MinAge > card.MaxAge)
                {
                    throw new ContentValidationException(
                        where + " has an ill-formed Age range " + card.MinAge + ".." + card.MaxAge);
                }

                switch (card.CardType)
                {
                    case CardType.BUILD:
                        // BUILD is paid in Growth (MR sec.1.2) and must name what it yields;
                        // its tier comes from the resolving Age, never from the card.
                        Require(card.CostResource == ResourceType.GROWTH, where + " must cost Growth");
                        Require(card.YieldType is not null, where + " must declare a yieldType");
                        Require(card.UnitClass is null, where + " must not declare a unitClass");
                        break;

                    case CardType.TRAIN:
                        Require(card.CostResource == ResourceType.GROWTH, where + " must cost Growth");
                        Require(card.UnitClass is not null, where + " must declare a unitClass");
                        // Each TRAIN belongs to exactly one Age: Power is fixed at training
                        // from that Age's row, so a multi-Age TRAIN would be ambiguous.
                        Require(card.MinAge == card.MaxAge, where + " must be eligible in exactly one Age");
                        break;

                    case CardType.ADVANCE:
                        Require(card.CostResource == ResourceType.INSIGHT, where + " must cost Insight");
                        Require(card.Archetype is not null, where + " must declare an archetype");
                        Require(card.UnitClass is null, where + " must not declare a unitClass");
                        if (card.IsKeystone)
                        {
                            // K1: Keystones are Age III+ and never offered earlier.
                            Require(card.MinAge >= 3, where + " is a Keystone and must have minAge >= 3");
                            Require(
                                card.Archetype == PerkArchetype.KEYSTONE,
                                where + " is a Keystone and must carry the KEYSTONE archetype");
                        }
                        else
                        {
                            Require(
                                card.Archetype != PerkArchetype.KEYSTONE,
                                where + " carries the KEYSTONE archetype but is not in the keystone roster");
                        }

                        break;

                    default:
                        throw new ContentValidationException(where + " has an unknown cardType");
                }
            }
        }

        private static void ValidateEffectIntegrity(IReadOnlyList<CardDefinition> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                for (int e = 0; e < card.Effects.Count; e++)
                {
                    ActiveEffect effect = card.Effects[e];
                    string where = "effect '" + effect.EffectId.Value + "' on card '" + card.CardId.Value + "'";

                    // An effect must name its own card, or the total order's sourceId
                    // tie-break would sort by something unrelated to where it came from.
                    Require(
                        effect.SourceId == card.CardId,
                        where + " names sourceId '" + effect.SourceId.Value + "' instead of its own card");

                    Require(
                        effect.SourceType != EffectSourceType.COMMANDER,
                        where + " uses the reserved COMMANDER source type");

                    Require(effect.Priority >= 0, where + " has a negative priority");
                }
            }
        }

        /// <summary>
        /// The gate. Every authored effect must be one the engine actually executes, and
        /// every combination the engine advertises must be exercised by the pool.
        ///
        /// The second direction matters as much as the first: a catalog entry nothing
        /// authors is an untested code path claiming to be supported.
        /// </summary>
        private static void ValidateEffectCoverage(IReadOnlyList<CardDefinition> cards)
        {
            HashSet<string> exercised = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                for (int e = 0; e < card.Effects.Count; e++)
                {
                    ActiveEffect effect = card.Effects[e];
                    SupportedEffect? supported = EffectCatalog.Find(
                        effect.TargetStat, effect.Trigger, effect.Scope, effect.ScopeValue);

                    if (supported is null)
                    {
                        throw new ContentValidationException(
                            "effect '" + effect.EffectId.Value + "' on card '" + card.CardId.Value +
                            "' is not executable: no handler is registered for " +
                            effect.TargetStat + "/" + effect.Trigger + "/" + effect.Scope + "/" +
                            (effect.ScopeValue ?? "null") +
                            ". An unimplemented effect would load silently and do nothing");
                    }

                    exercised.Add(supported.Value.ToString());
                }
            }

            IReadOnlyList<SupportedEffect> catalog = EffectCatalog.All;
            List<string> unexercised = new List<string>();
            for (int i = 0; i < catalog.Count; i++)
            {
                if (!exercised.Contains(catalog[i].ToString()))
                {
                    unexercised.Add(catalog[i].ToString());
                }
            }

            if (unexercised.Count > 0)
            {
                throw new ContentValidationException(
                    "the effect catalog advertises " + unexercised.Count +
                    " handler(s) no authored card uses, so they are untested code paths: " +
                    string.Join(", ", unexercised.ToArray()));
            }
        }

        /// <summary>
        /// Offer generation must always be able to fill a three-card hand without a
        /// duplicate id, wherever a card type has non-zero weight (Core Spec sec.6.0).
        /// </summary>
        private static void ValidateOfferSufficiency(IReadOnlyList<CardDefinition> cards)
        {
            for (int turn = 1; turn <= RunState.TurnsPerRun; turn++)
            {
                int age = RunState.AgeForTurn(turn);
                OfferWeightBand band = OfferWeights.ForTurn(turn);

                CheckPool(cards, CardType.BUILD, age, band.Build, turn);
                CheckPool(cards, CardType.TRAIN, age, band.Train, turn);
                CheckPool(cards, CardType.ADVANCE, age, band.Advance, turn);
            }
        }

        private static void CheckPool(
            IReadOnlyList<CardDefinition> cards,
            CardType type,
            int age,
            int weight,
            int turn)
        {
            if (weight <= 0)
            {
                return;
            }

            int eligible = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].CardType == type && cards[i].IsEligibleInAge(age))
                {
                    eligible++;
                }
            }

            if (eligible < RunState.CardsOfferedPerTurn)
            {
                throw new ContentValidationException(
                    type + " has non-zero weight on turn " + turn + " (Age " + age + ") but only " +
                    eligible + " eligible card(s); a three-card hand of distinct ids could not be drawn");
            }
        }

        private static void Require(bool condition, string problem)
        {
            if (!condition)
            {
                throw new ContentValidationException(problem);
            }
        }
    }
}
