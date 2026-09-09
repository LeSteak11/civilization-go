using System.Collections.Generic;
using Epoch.Application.Runs;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Core.StateMachine;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Application.Tests
{
    public static class PlayablePresentationTests
    {
        [TestCase("M4-01", "The turn-at-a-time presentation path completes a 24-turn match")]
        public static void PlayablePathCompletesMatch()
        {
            ValidatedContentSet content = ContentFixtureLoader.Load();
            PlayableMatchSession session = new PlayableMatchSession(
                new SeedCode(SeedCodec.Encode(42)), content);

            int turns = 0;
            while (!session.IsComplete)
            {
                IReadOnlyList<CardPresentation> cards = session.Cards();
                Selection selection = FirstLegal(cards);
                PresentationTurn resolved = selection.IsPass
                    ? session.SubmitForcedPass()
                    : session.Submit(selection);

                turns++;
                Assert.Equal(turns, resolved.PreState.Turn, "the UI resolves exactly the displayed turn");
                Assert.True(resolved.Events.Count > 0, "each turn returns presentation events");
            }

            Assert.Equal(RunState.TurnsPerRun, turns, "a complete presentation playthrough is 24 turns");
            Assert.True(session.State.Result is not null, "turn 24 produces the result screen model");
            Assert.True(session.VerifyReplay(), "the completed presentation run replays exactly");

            PlayableMatchSession restarted = session.RestartSameSeed();
            Assert.Equal(session.State.Seed.MasterSeed.Text, restarted.State.Seed.MasterSeed.Text,
                "same-seed Restart preserves the Seed");
            Assert.Equal(1, restarted.State.Turn, "same-seed Restart returns to turn 1");
        }

        [TestCase("M4-02", "Skipping every presentation event preserves the authoritative result")]
        public static void SkipDoesNotChangeResult()
        {
            ValidatedContentSet content = ContentFixtureLoader.Load();
            SeedCode seed = new SeedCode(SeedCodec.Encode(91));
            PlayableMatchSession animated = new PlayableMatchSession(seed, content);
            PlayableMatchSession skipped = new PlayableMatchSession(seed, content);
            PresentationEventQueue queue = new PresentationEventQueue();

            while (!animated.IsComplete)
            {
                Selection selection = FirstLegal(animated.Cards());
                PresentationTurn animatedTurn = selection.IsPass
                    ? animated.SubmitForcedPass()
                    : animated.Submit(selection);
                PresentationTurn skippedTurn = selection.IsPass
                    ? skipped.SubmitForcedPass()
                    : skipped.Submit(selection);

                queue.Begin(animatedTurn);
                while (!queue.IsComplete)
                {
                    _ = queue.Advance();
                }

                queue.Begin(skippedTurn);
                queue.Skip();
                Assert.True(queue.IsComplete, "Skip consumes presentation events only");
            }

            Assert.Equal(animated.State.Result!.PlayerScore, skipped.State.Result!.PlayerScore,
                "player score is animation-independent");
            Assert.Equal(animated.State.Result.SnapshotScore, skipped.State.Result.SnapshotScore,
                "Snapshot score is animation-independent");
            Assert.Equal(
                animated.Turns[animated.Turns.Count - 1].StateHash,
                skipped.Turns[skipped.Turns.Count - 1].StateHash,
                "the final authoritative hash is animation-independent");
        }

        private static Selection FirstLegal(IReadOnlyList<CardPresentation> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardPresentation card = cards[i];
                if (!card.IsLegal)
                {
                    continue;
                }

                return card.Definition.CardType == CardType.ADVANCE
                    ? new Selection(card.OfferIndex, null)
                    : new Selection(card.OfferIndex, card.LegalLanes[0]);
            }

            return Selection.Pass;
        }
    }
}
