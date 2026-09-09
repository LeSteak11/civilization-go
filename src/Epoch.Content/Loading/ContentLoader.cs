using System;
using System.Collections.Generic;
using Epoch.Content.Hashing;
using Epoch.Content.Json;
using Epoch.Content.Validation;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Content.Loading
{
    /// <summary>
    /// Strict deserialization of the authored content manifest into the immutable value
    /// types the Core consumes (Technical Plan sec.3.3).
    ///
    /// <para>"Strict" is load-bearing: every object rejects unknown members, every enum
    /// must be an exact member name, duplicate keys are a parse error, and a card that
    /// authors its own numeric cost is refused outright. Content is the one input a
    /// designer edits by hand, so it is the one input most likely to drift silently.</para>
    ///
    /// <para>The loader deliberately takes the document **text**, not a file path. Reading
    /// the file is the host's job - Unity, a test, a tool - and keeping I/O out of here
    /// means the same loader serves all three without any of them inheriting a filesystem
    /// dependency they did not ask for.</para>
    /// </summary>
    public static class ContentLoader
    {
        /// <summary>The only schema this loader speaks.</summary>
        public const string SupportedSchemaVersion = "1.0.0";

        /// <summary>Members a card entry may carry. Anything else fails the load.</summary>
        private static readonly string[] KnownEntryMembers =
        {
            "id", "displayName", "contentType", "cardType", "archetype", "minAge", "maxAge",
            "offerEligibility", "resourceType", "costRule", "targetRequirements", "yieldType",
            "tierRule", "unitClass", "powerRule", "grantsReach", "effects", "effectTiming",
            "effectPriority", "tags", "localizationKeys", "shortEffectLine", "rulesText",
            "validationConstraints", "balanceRationale", "ruleSources", "sourceClassification",
        };

        private static readonly string[] KnownEffectMembers =
        {
            "effectId", "operation", "scope", "scopeValue", "targetStat", "sourceType",
            "sourceId", "trigger", "magnitude", "priority",
        };

        private static readonly string[] KnownRootMembers =
        {
            "schemaVersion", "contentVersion", "compatibleRulesVersion", "idConvention",
            "effectDefaults", "ageTable", "offerWeights", "builds", "trains", "perks", "keystones",
        };

        /// <summary>
        /// Parse, validate and freeze. Throws <see cref="ContentFormatException"/> for a
        /// malformed document and <see cref="ContentValidationException"/> for one that
        /// parses but breaks a content rule.
        /// </summary>
        public static ValidatedContentSet Load(string documentText, string expectedRulesVersion)
        {
            if (documentText is null)
            {
                throw new ArgumentNullException(nameof(documentText));
            }

            JsonValue root = JsonReader.Parse(documentText);
            root.RejectUnknownMembers(KnownRootMembers);

            string schemaVersion = root.Member("schemaVersion").AsString();
            string contentVersion = root.Member("contentVersion").AsString();
            string compatibleRulesVersion = root.Member("compatibleRulesVersion").AsString();

            if (!string.Equals(schemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
            {
                throw new ContentValidationException(
                    "schemaVersion '" + schemaVersion + "' is not supported; this loader speaks '" +
                    SupportedSchemaVersion + "'");
            }

            // [Lock 18] in spirit: content compiled against different rules is not loaded.
            // There is no migration path, and guessing would be worse than refusing.
            if (!string.Equals(compatibleRulesVersion, expectedRulesVersion, StringComparison.Ordinal))
            {
                throw new ContentValidationException(
                    "content declares compatibleRulesVersion '" + compatibleRulesVersion +
                    "' but the engine implements '" + expectedRulesVersion + "'");
            }

            List<CardDefinition> cards = new List<CardDefinition>();

            // Declaration order is part of the deterministic contract: offer generation
            // selects a card by index into the eligible subset (Implementation Lock I).
            ReadEntries(root, "builds", cards);
            ReadEntries(root, "trains", cards);
            ReadEntries(root, "perks", cards);
            ReadEntries(root, "keystones", cards);

            ContentValidator.Validate(root, cards);

            string contentHash = ContentHasher.Hash(root, cards);

            return new ValidatedContentSet(
                schemaVersion,
                contentVersion,
                compatibleRulesVersion,
                contentHash,
                cards);
        }

        private static void ReadEntries(JsonValue root, string arrayName, List<CardDefinition> cards)
        {
            JsonValue array = root.Member(arrayName);
            IReadOnlyList<JsonValue> items = array.Items;
            for (int i = 0; i < items.Count; i++)
            {
                cards.Add(ReadCard(items[i]));
            }
        }

        private static CardDefinition ReadCard(JsonValue entry)
        {
            entry.RejectUnknownMembers(KnownEntryMembers);

            string id = entry.Member("id").AsString();
            CardType cardType = ParseEnum<CardType>(entry.Member("cardType"), "cardType");
            ContentType contentType = ParseEnum<ContentType>(entry.Member("contentType"), "contentType");
            ResourceType costResource = ParseEnum<ResourceType>(entry.Member("resourceType"), "resourceType");

            JsonValue? yieldTypeMember = entry.OptionalMember("yieldType");
            ResourceType? yieldType = yieldTypeMember is null
                ? (ResourceType?)null
                : ParseEnum<ResourceType>(yieldTypeMember, "yieldType");

            JsonValue? unitClassMember = entry.OptionalMember("unitClass");
            UnitClass? unitClass = unitClassMember is null
                ? (UnitClass?)null
                : ParseEnum<UnitClass>(unitClassMember, "unitClass");

            JsonValue? archetypeMember = entry.OptionalMember("archetype");
            PerkArchetype? archetype = archetypeMember is null
                ? (PerkArchetype?)null
                : ParseEnum<PerkArchetype>(archetypeMember, "archetype");

            JsonValue? reachMember = entry.OptionalMember("grantsReach");
            bool grantsReach = reachMember is not null && reachMember.AsBoolean();

            List<ActiveEffect> effects = new List<ActiveEffect>();
            IReadOnlyList<JsonValue> effectItems = entry.Member("effects").Items;
            for (int i = 0; i < effectItems.Count; i++)
            {
                effects.Add(ReadEffect(effectItems[i], id));
            }

            return new CardDefinition(
                new StableId(id),
                cardType,
                contentType,
                entry.Member("minAge").AsInt32(),
                entry.Member("maxAge").AsInt32(),
                costResource,
                yieldType,
                unitClass,
                grantsReach,
                archetype,
                effects,
                entry.Member("displayName").AsString(),
                entry.Member("shortEffectLine").AsString(),
                entry.Member("rulesText").AsString());
        }

        private static ActiveEffect ReadEffect(JsonValue effect, string owningEntryId)
        {
            effect.RejectUnknownMembers(KnownEffectMembers);

            EffectOperation operation = ParseEnum<EffectOperation>(effect.Member("operation"), "operation");

            JsonValue? scopeValueMember = effect.OptionalMember("scopeValue");
            string? scopeValue = scopeValueMember is null || scopeValueMember.IsNull
                ? null
                : scopeValueMember.AsString();

            // Priorities are explicit in V1 content, but the documented defaults are
            // applied rather than guessed if one is ever omitted.
            JsonValue? priorityMember = effect.OptionalMember("priority");
            int priority = priorityMember is not null
                ? priorityMember.AsInt32()
                : operation == EffectOperation.ADD
                    ? ActiveEffect.DefaultAddPriority
                    : ActiveEffect.DefaultMultiplyPriority;

            // Magnitude becomes fixed-point hundredths by integer arithmetic; no float
            // ever exists, even transiently, on the path from the file to the Core.
            int magnitudeHundredths = effect.Member("magnitude").AsHundredths();

            _ = owningEntryId;

            return new ActiveEffect(
                new StableId(effect.Member("effectId").AsString()),
                ParseEnum<EffectSourceType>(effect.Member("sourceType"), "sourceType"),
                new StableId(effect.Member("sourceId").AsString()),
                ParseEnum<EffectTrigger>(effect.Member("trigger"), "trigger"),
                ParseEnum<EffectScope>(effect.Member("scope"), "scope"),
                scopeValue,
                operation,
                effect.Member("targetStat").AsString(),
                new FixedValue(magnitudeHundredths),
                priority,
                // Effects are never retroactive (A4); a perk is live from the turn it is
                // acquired, which ownership already expresses.
                0);
        }

        private static T ParseEnum<T>(JsonValue value, string member)
            where T : struct
        {
            string text = value.AsString();

            // Case-sensitive on purpose: "sword" is a typo, not a synonym.
            if (!Enum.TryParse(text, ignoreCase: false, out T parsed) || !Enum.IsDefined(typeof(T), parsed))
            {
                throw new ContentFormatException(
                    value.Path, "'" + text + "' is not a valid " + typeof(T).Name + " for member '" + member + "'");
            }

            return parsed;
        }
    }
}
