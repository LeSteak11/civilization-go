using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Epoch.Content.Json;
using Epoch.Core.Content;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Content.Hashing
{
    /// <summary>
    /// The canonical content hash recorded in <c>RunState.contentPoolHash</c>
    /// (Core Spec sec.5.2) and in every replay and snapshot.
    ///
    /// <para>It hashes the <b>meaning</b> of the content, not the file. Reformatting the
    /// manifest, reordering an entry's members, or editing a designer note must not change
    /// the hash, because none of those change a single match. Conversely anything that
    /// does change a match - a magnitude, an Age range, a unit class, or the declaration
    /// order that offer generation indexes into - must change it.</para>
    ///
    /// <para>That is why this serializes the parsed, validated model rather than the raw
    /// bytes. Hashing the file would make every whitespace edit invalidate every stored
    /// fixture, and would tell a designer their comment broke determinism.</para>
    /// </summary>
    public static class ContentHasher
    {
        private const string HexDigits = "0123456789abcdef";

        public static string Hash(JsonValue root, IReadOnlyList<CardDefinition> cards)
        {
            string canonical = Canonicalize(root, cards);
            byte[] bytes = new UTF8Encoding(false).GetBytes(canonical);
            using (SHA256 sha = SHA256.Create())
            {
                return "sha256:" + ToHex(sha.ComputeHash(bytes));
            }
        }

        /// <summary>
        /// The canonical text. Fixed field order, declaration order preserved for cards,
        /// ordinal ordering for effects within a card, and no locale-sensitive formatting.
        /// Exposed so a test can diff two canonical forms rather than two opaque digests.
        /// </summary>
        public static string Canonicalize(JsonValue root, IReadOnlyList<CardDefinition> cards)
        {
            StringBuilder text = new StringBuilder(8192);

            text.Append("schemaVersion=").Append(root.Member("schemaVersion").AsString()).Append('\n');
            text.Append("contentVersion=").Append(root.Member("contentVersion").AsString()).Append('\n');
            text.Append("compatibleRulesVersion=").Append(root.Member("compatibleRulesVersion").AsString()).Append('\n');

            // The Age table and offer weights are validated against the locked rules, but
            // they are still content bytes a run depends on, so they are hashed too.
            IReadOnlyList<JsonValue> ages = root.Member("ageTable").Items;
            for (int i = 0; i < ages.Count; i++)
            {
                JsonValue row = ages[i];
                text.Append("age=").Append(Int(row.Member("age").AsInt32()));
                text.Append(";perkCost=").Append(Int(row.Member("perkCost").AsInt32()));
                text.Append(";unitPower=").Append(Int(row.Member("unitPower").AsInt32()));
                text.Append(";unitCost=").Append(Int(row.Member("unitCost").AsInt32()));
                text.Append(";buildCost=").Append(Int(row.Member("buildCost").AsInt32()));
                text.Append(";tier=").Append(Int(row.Member("structureTier").AsInt32()));
                text.Append(";yield=").Append(Int(row.Member("structureBaseYield").AsInt32()));
                text.Append('\n');
            }

            IReadOnlyList<JsonValue> bands = root.Member("offerWeights").Items;
            for (int i = 0; i < bands.Count; i++)
            {
                JsonValue band = bands[i];
                IReadOnlyList<JsonValue> turns = band.Member("turns").Items;
                text.Append("band=").Append(Int(turns[0].AsInt32())).Append('-').Append(Int(turns[1].AsInt32()));
                text.Append(";BUILD=").Append(Int(band.Member("BUILD").AsInt32()));
                text.Append(";TRAIN=").Append(Int(band.Member("TRAIN").AsInt32()));
                text.Append(";ADVANCE=").Append(Int(band.Member("ADVANCE").AsInt32()));
                text.Append('\n');
            }

            // Cards in declaration order: offer generation indexes into the eligible
            // subset, so reordering the roster changes every offer sequence and must
            // therefore change the hash.
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                text.Append("card=").Append(card.CardId.Value);
                text.Append(";index=").Append(Int(i));
                text.Append(";type=").Append(card.CardType.ToString());
                text.Append(";content=").Append(card.ContentType.ToString());
                text.Append(";minAge=").Append(Int(card.MinAge));
                text.Append(";maxAge=").Append(Int(card.MaxAge));
                text.Append(";cost=").Append(card.CostResource.ToString());
                text.Append(";yieldType=").Append(card.YieldType is null ? "-" : card.YieldType.Value.ToString());
                text.Append(";unitClass=").Append(card.UnitClass is null ? "-" : card.UnitClass.Value.ToString());
                text.Append(";reach=").Append(card.GrantsReach ? "1" : "0");
                text.Append(";archetype=").Append(card.Archetype is null ? "-" : card.Archetype.Value.ToString());
                text.Append('\n');

                // Effects are sorted by ordinal effectId so that an author reordering the
                // array without changing its content does not change the hash. Their
                // resolution order is governed by the total order in the Core, not by the
                // order they appear in the file.
                List<ActiveEffect> effects = new List<ActiveEffect>(card.Effects);
                effects.Sort((a, b) => a.EffectId.CompareTo(b.EffectId));
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    text.Append("  effect=").Append(effect.EffectId.Value);
                    text.Append(";op=").Append(effect.Operation.ToString());
                    text.Append(";scope=").Append(effect.Scope.ToString());
                    text.Append(";scopeValue=").Append(effect.ScopeValue ?? "-");
                    text.Append(";stat=").Append(effect.TargetStat);
                    text.Append(";sourceType=").Append(effect.SourceType.ToString());
                    text.Append(";sourceId=").Append(effect.SourceId.Value);
                    text.Append(";trigger=").Append(effect.Trigger.ToString());
                    text.Append(";magnitude=").Append(Int(effect.Magnitude.Hundredths));
                    text.Append(";priority=").Append(Int(effect.Priority));
                    text.Append('\n');
                }
            }

            return text.ToString();
        }

        private static string Int(int value) => FixedValue.IntToString(value);

        private static string ToHex(byte[] digest)
        {
            char[] text = new char[digest.Length * 2];
            for (int i = 0; i < digest.Length; i++)
            {
                text[i * 2] = HexDigits[digest[i] >> 4];
                text[(i * 2) + 1] = HexDigits[digest[i] & 0x0F];
            }

            return new string(text);
        }
    }
}
