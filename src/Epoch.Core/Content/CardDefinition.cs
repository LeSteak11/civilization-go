using System;
using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Effects;

namespace Epoch.Core.Content
{
    /// <summary>
    /// Which authored roster an entry came from. KEYSTONE is an ADVANCE subtype, not a
    /// fourth CardType (Core Spec sec.6.5) — the distinction lives here so that the
    /// max-one-per-run rule has something to test without inventing a card type.
    /// </summary>
    public enum ContentType
    {
        BUILD,
        TRAIN,
        PERK,
        KEYSTONE,
    }

    /// <summary>
    /// Core Spec sec.5.10. An immutable value the Content layer hands inward after
    /// validation — the Core never loads or parses anything itself.
    ///
    /// **Cost is deliberately absent.** It is read from <see cref="AgeTable"/> for the
    /// resolving Age (MR sec.2.2). A card that authored its own numeric cost would be a
    /// content validation failure, not a supported case.
    /// </summary>
    public sealed record CardDefinition(
        StableId CardId,
        CardType CardType,
        ContentType ContentType,
        int MinAge,
        int MaxAge,
        ResourceType CostResource,
        ResourceType? YieldType,
        UnitClass? UnitClass,
        bool GrantsReach,
        PerkArchetype? Archetype,
        IReadOnlyList<ActiveEffect> Effects,
        string DisplayName,
        string ShortEffectLine,
        string RulesText)
    {
        /// <summary>Offer eligibility for an Age (Core Spec sec.6.0).</summary>
        public bool IsEligibleInAge(int age) => age >= MinAge && age <= MaxAge;

        public bool IsKeystone => ContentType == ContentType.KEYSTONE;
    }

    /// <summary>
    /// The frozen, validated content pool for the life of a run
    /// (Technical Plan sec.3.3: "Freeze a ValidatedContentSet for the life of a run").
    ///
    /// <para><b>Card order is authoritative.</b> Offer generation selects a card by index
    /// into the eligible subset (Core Spec sec.6.0), so the declaration order of the
    /// authored roster is part of the deterministic contract. Reordering the content file
    /// changes every offer sequence and therefore requires a contentVersion change.</para>
    /// </summary>
    public sealed class ValidatedContentSet
    {
        private readonly CardDefinition[] _cards;
        private readonly Dictionary<string, CardDefinition> _byId;

        public ValidatedContentSet(
            string schemaVersion,
            string contentVersion,
            string compatibleRulesVersion,
            string contentHash,
            IReadOnlyList<CardDefinition> cardsInDeclarationOrder)
        {
            if (cardsInDeclarationOrder is null)
            {
                throw new ArgumentNullException(nameof(cardsInDeclarationOrder));
            }

            SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
            ContentVersion = contentVersion ?? throw new ArgumentNullException(nameof(contentVersion));
            CompatibleRulesVersion = compatibleRulesVersion ?? throw new ArgumentNullException(nameof(compatibleRulesVersion));
            ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));

            _cards = new CardDefinition[cardsInDeclarationOrder.Count];
            for (int i = 0; i < cardsInDeclarationOrder.Count; i++)
            {
                _cards[i] = cardsInDeclarationOrder[i];
            }

            _byId = new Dictionary<string, CardDefinition>(_cards.Length, StringComparer.Ordinal);
            for (int i = 0; i < _cards.Length; i++)
            {
                _byId.Add(_cards[i].CardId.Value, _cards[i]);
            }
        }

        public string SchemaVersion { get; }

        public string ContentVersion { get; }

        public string CompatibleRulesVersion { get; }

        public string ContentHash { get; }

        public IReadOnlyList<CardDefinition> Cards => _cards;

        public CardDefinition ById(StableId cardId) => _byId[cardId.Value];

        public bool TryById(StableId cardId, out CardDefinition card) =>
            _byId.TryGetValue(cardId.Value, out card!);

        /// <summary>
        /// The offer-eligible cards of a type in an Age, **in declaration order**.
        /// A fresh list is built rather than a cached dictionary enumerated, because
        /// "never enumerate hash maps where order could affect state" (Technical Plan sec.3.2).
        /// </summary>
        public List<CardDefinition> EligiblePool(CardType cardType, int age)
        {
            List<CardDefinition> pool = new List<CardDefinition>();
            for (int i = 0; i < _cards.Length; i++)
            {
                CardDefinition card = _cards[i];
                if (card.CardType == cardType && card.IsEligibleInAge(age))
                {
                    pool.Add(card);
                }
            }

            return pool;
        }
    }
}
