using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Testing
{
    /// <summary>
    /// Builds a <see cref="ValidatedContentSet"/> from the authored manifest so that M1
    /// can run complete matches against the real 48-entry pool.
    ///
    /// <para><b>This is a test fixture loader, not the production loader.</b> M2 owns
    /// strict deserialization, exhaustive validation, effect-coverage proof, compatibility
    /// guards and the canonical content hash, in <c>Epoch.Content</c>. This class does the
    /// minimum needed to feed the simulation and deliberately does not pretend to
    /// validate - if it accepted something malformed, M2's job is to reject it.</para>
    ///
    /// <para>Declaration order is preserved exactly - builds, then trains, then perks,
    /// then keystones - because offer generation indexes into the eligible subset, which
    /// makes that order part of the deterministic contract.</para>
    /// </summary>
    public static class ContentFixtureLoader
    {
        private static ValidatedContentSet? _cached;

        public static ValidatedContentSet Load()
        {
            return _cached ??= LoadFrom(FixturePaths.ContentManifest);
        }

        public static ValidatedContentSet LoadFrom(string path)
        {
            string json = File.ReadAllText(path);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            List<CardDefinition> cards = new List<CardDefinition>();
            AppendAll(root, "builds", cards);
            AppendAll(root, "trains", cards);
            AppendAll(root, "perks", cards);
            AppendAll(root, "keystones", cards);

            return new ValidatedContentSet(
                root.GetProperty("schemaVersion").GetString()!,
                root.GetProperty("contentVersion").GetString()!,
                root.GetProperty("compatibleRulesVersion").GetString()!,
                // A placeholder hash: the canonical content hash is M2's deliverable.
                "sha256:pending-m2",
                cards);
        }

        private static void AppendAll(JsonElement root, string arrayName, List<CardDefinition> cards)
        {
            foreach (JsonElement entry in root.GetProperty(arrayName).EnumerateArray())
            {
                cards.Add(ReadCard(entry));
            }
        }

        private static CardDefinition ReadCard(JsonElement entry)
        {
            string id = entry.GetProperty("id").GetString()!;
            CardType cardType = ParseEnum<CardType>(entry.GetProperty("cardType").GetString()!);
            ContentType contentType = ParseEnum<ContentType>(entry.GetProperty("contentType").GetString()!);

            ResourceType costResource = ParseEnum<ResourceType>(entry.GetProperty("resourceType").GetString()!);

            ResourceType? yieldType = entry.TryGetProperty("yieldType", out JsonElement yieldElement)
                ? ParseEnum<ResourceType>(yieldElement.GetString()!)
                : (ResourceType?)null;

            UnitClass? unitClass = entry.TryGetProperty("unitClass", out JsonElement classElement)
                ? ParseEnum<UnitClass>(classElement.GetString()!)
                : (UnitClass?)null;

            bool grantsReach = entry.TryGetProperty("grantsReach", out JsonElement reachElement) &&
                               reachElement.GetBoolean();

            PerkArchetype? archetype = entry.TryGetProperty("archetype", out JsonElement archetypeElement)
                ? ParseEnum<PerkArchetype>(archetypeElement.GetString()!)
                : (PerkArchetype?)null;

            List<ActiveEffect> effects = new List<ActiveEffect>();
            foreach (JsonElement effect in entry.GetProperty("effects").EnumerateArray())
            {
                effects.Add(ReadEffect(effect));
            }

            return new CardDefinition(
                new StableId(id),
                cardType,
                contentType,
                entry.GetProperty("minAge").GetInt32(),
                entry.GetProperty("maxAge").GetInt32(),
                costResource,
                yieldType,
                unitClass,
                grantsReach,
                archetype,
                effects,
                entry.GetProperty("displayName").GetString()!);
        }

        private static ActiveEffect ReadEffect(JsonElement effect)
        {
            EffectOperation operation = ParseEnum<EffectOperation>(effect.GetProperty("operation").GetString()!);

            // Magnitudes are authored as whole numbers in V1; they become fixed-point
            // hundredths on the way in so the Core never sees a float.
            JsonElement magnitude = effect.GetProperty("magnitude");
            FixedValue value = magnitude.TryGetInt32(out int whole)
                ? FixedValue.FromInt(whole)
                : new FixedValue((int)Math.Round(
                    magnitude.GetDouble() * FixedValue.Scale, MidpointRounding.AwayFromZero));

            return new ActiveEffect(
                new StableId(effect.GetProperty("effectId").GetString()!),
                ParseEnum<EffectSourceType>(effect.GetProperty("sourceType").GetString()!),
                new StableId(effect.GetProperty("sourceId").GetString()!),
                ParseEnum<EffectTrigger>(effect.GetProperty("trigger").GetString()!),
                ParseEnum<EffectScope>(effect.GetProperty("scope").GetString()!),
                effect.TryGetProperty("scopeValue", out JsonElement scopeValue) ? scopeValue.GetString() : null,
                operation,
                effect.GetProperty("targetStat").GetString()!,
                value,
                effect.TryGetProperty("priority", out JsonElement priority)
                    ? priority.GetInt32()
                    : operation == EffectOperation.ADD
                        ? ActiveEffect.DefaultAddPriority
                        : ActiveEffect.DefaultMultiplyPriority,
                // Effects are never retroactive but are always live once acquired
                // (Core Spec sec.6.4 A4); acquisition turn is enforced by ownership.
                0);
        }

        private static T ParseEnum<T>(string text)
            where T : struct
        {
            if (!Enum.TryParse(text, ignoreCase: false, out T value))
            {
                throw new InvalidDataException(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' is not a member of {1}.",
                    text,
                    typeof(T).Name));
            }

            return value;
        }
    }
}
