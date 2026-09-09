using System;
using System.Collections.Generic;
using System.Linq;
using Epoch.Core.Commands;
using Epoch.Core.Domain;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    public static class DomainPrimitiveTests
    {
        [TestCase("ID-01", "StableId orders ordinally, not by culture")]
        public static void StableIdOrdersOrdinally()
        {
            List<StableId> ids = new List<StableId>
            {
                new StableId("TRAIN_A1_SWORD"),
                new StableId("BUILD_GROWTH_WORKS"),
                new StableId("PERK_ECO_GROWTH_1"),
                new StableId("BUILD_GARRISON_POST"),
            };

            List<string> sorted = ids.OrderBy(id => id).Select(id => id.Value).ToList();

            Assert.SequenceEqual(
                new[] { "BUILD_GARRISON_POST", "BUILD_GROWTH_WORKS", "PERK_ECO_GROWTH_1", "TRAIN_A1_SWORD" },
                sorted,
                "every tie-break chain in the spec ends in ascending stable id, so the order must be culture-independent");
        }

        [TestCase("ID-02", "An empty StableId is rejected at construction")]
        public static void StableIdRejectsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new StableId(string.Empty), "an id must identify something");
        }

        [TestCase("PASS-01", "PASS is offer index -1 and nothing else")]
        public static void PassIsMinusOne()
        {
            CardSelectionCommand pass = new CardSelectionCommand(9, Side.PLAYER, CardSelectionCommand.PassOfferIndex, null);
            CardSelectionCommand pick = new CardSelectionCommand(9, Side.PLAYER, 1, LaneId.B);

            Assert.Equal(-1, CardSelectionCommand.PassOfferIndex, "Core Spec sec.5.4 [Lock 12]");
            Assert.True(pass.IsPass, "offerIndex -1 is a forced PASS");
            Assert.False(pick.IsPass, "a real selection is never a PASS");
        }

        [TestCase("VAL-01", "A valid result carries no errors and reports valid")]
        public static void ValidationResultShape()
        {
            Assert.True(ValidationResult.Valid.IsValid, "the shared valid instance");
            Assert.Empty(ValidationResult.Valid.Errors, "no errors on success");

            ValidationResult invalid = ValidationResult.Invalid(ValidationError.ERR_UNAFFORDABLE);
            Assert.False(invalid.IsValid, "a rejected selection");
            Assert.True(invalid.Contains(ValidationError.ERR_UNAFFORDABLE), "the reason is surfaced to the UI");
        }
    }
}
