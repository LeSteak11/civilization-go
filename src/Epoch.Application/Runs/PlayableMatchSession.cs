using System;
using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Events;
using Epoch.Core.StateMachine;
using Epoch.Core.Systems;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// Turn-at-a-time orchestration for the M4 presentation. It is deliberately thin:
    /// the UI submits a <see cref="Selection"/>, the Core resolves the whole turn, and
    /// this class publishes the returned authoritative state and ordered events.
    /// </summary>
    public sealed class PlayableMatchSession : RunCoordinator
    {
        private readonly ValidatedContentSet _content;
        private readonly SelectionSource _snapshot;
        private readonly List<PresentationTurn> _turns = new List<PresentationTurn>();
        private readonly SeedCode _seed;
        private int _nextSequence;

        public PlayableMatchSession(
            SeedCode seed,
            ValidatedContentSet content,
            SelectionSource? snapshot = null)
        {
            _seed = seed;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _snapshot = snapshot ?? new LastLegalSelectionSource();
            State = RunFactory.Create(seed, content);
            Offers = MatchResolver.GenerateOffers(State, content);
        }

        public RunState State { get; private set; }

        public CardOfferSet Offers { get; private set; }

        public bool IsComplete => State.Result is not null;

        public bool IsForcedPass => !IsComplete &&
            !LegalitySystem.HasAnyLegalSelection(State, Side.PLAYER, Offers, _content);

        public IReadOnlyList<PresentationTurn> Turns => _turns;

        public IReadOnlyList<CardPresentation> Cards()
        {
            List<CardPresentation> cards = new List<CardPresentation>(Offers.Count);
            for (int i = 0; i < Offers.Count; i++)
            {
                CardDefinition definition = _content.ById(Offers[i].CardId);
                List<LaneId> lanes = LegalitySystem.LegalLanes(
                    State, Side.PLAYER, Offers, i, _content);
                ValidationError error;
                bool legal;

                if (definition.CardType == CardType.ADVANCE)
                {
                    error = LegalitySystem.Check(
                        State, Side.PLAYER, Offers, i, null, _content);
                    legal = error == ValidationError.NONE;
                }
                else if (lanes.Count > 0)
                {
                    error = ValidationError.NONE;
                    legal = true;
                }
                else
                {
                    error = LegalitySystem.Check(
                        State, Side.PLAYER, Offers, i, LaneId.A, _content);
                    legal = false;
                }

                cards.Add(new CardPresentation(
                    i,
                    definition,
                    AgeTable.ForAge(State.Age).CostFor(definition.CardType),
                    legal,
                    error,
                    lanes));
            }

            return cards;
        }

        public PresentationTurn Submit(Selection selection)
        {
            if (IsComplete)
            {
                throw new InvalidOperationException("The match has already completed.");
            }

            ValidationError playerError = LegalitySystem.Validate(
                State, Side.PLAYER, Offers, selection, _content);
            if (playerError != ValidationError.NONE)
            {
                throw new InvalidOperationException(
                    "The player selection is not legal: " + playerError + ".");
            }

            RunState before = State;
            CardOfferSet turnOffers = Offers;
            Selection snapshotSelection = _snapshot.Select(
                before, Side.SNAPSHOT, turnOffers, _content);

            ResolvedTurn resolved = MatchResolver.Resolve(
                before,
                turnOffers,
                selection,
                snapshotSelection,
                _content,
                _nextSequence);

            if (!resolved.IsAccepted || resolved.NextState is null)
            {
                throw new InvalidOperationException(
                    "The authoritative turn was rejected for " + resolved.RejectedSide +
                    ": " + resolved.Error + ".");
            }

            State = resolved.NextState;
            _nextSequence = resolved.NextSequence;
            PresentationTurn frame = new PresentationTurn(
                before,
                State,
                turnOffers,
                selection,
                snapshotSelection,
                resolved.Events,
                resolved.CanonicalState,
                StateHasher.Sha256Hex(resolved.CanonicalState));
            _turns.Add(frame);

            if (!IsComplete)
            {
                Offers = MatchResolver.GenerateOffers(State, _content);
            }

            return frame;
        }

        public PresentationTurn SubmitForcedPass()
        {
            if (!IsForcedPass)
            {
                throw new InvalidOperationException(
                    "PASS is never voluntary; at least one offered card is legal.");
            }

            return Submit(Selection.Pass);
        }

        public PlayableMatchSession RestartSameSeed() =>
            new PlayableMatchSession(_seed, _content, new LastLegalSelectionSource());

        public bool VerifyReplay()
        {
            if (!IsComplete || _turns.Count != RunState.TurnsPerRun)
            {
                return false;
            }

            List<Selection> player = new List<Selection>(_turns.Count);
            List<Selection> snapshot = new List<Selection>(_turns.Count);
            for (int i = 0; i < _turns.Count; i++)
            {
                player.Add(_turns[i].PlayerSelection);
                snapshot.Add(_turns[i].SnapshotSelection);
            }

            MatchRecord replay = HeadlessMatch.Run(
                _seed,
                _content,
                new RecordedSelectionSource(player),
                new RecordedSelectionSource(snapshot));

            return string.Equals(
                replay.Turns[replay.Turns.Count - 1].StateHash,
                _turns[_turns.Count - 1].StateHash,
                StringComparison.Ordinal);
        }
    }

    /// <summary>A disposable projection for one card. No value here is authoritative.</summary>
    public sealed record CardPresentation(
        int OfferIndex,
        CardDefinition Definition,
        int Cost,
        bool IsLegal,
        ValidationError IllegalReason,
        IReadOnlyList<LaneId> LegalLanes);

    /// <summary>
    /// A presentation frame around one already-resolved authoritative turn. Keeping both
    /// states lets event animation show movement/combat before snapping to PostState.
    /// </summary>
    public sealed record PresentationTurn(
        RunState PreState,
        RunState PostState,
        CardOfferSet Offers,
        Selection PlayerSelection,
        Selection SnapshotSelection,
        IReadOnlyList<SimulationEvent> Events,
        string CanonicalState,
        string StateHash);

    /// <summary>
    /// Engine-neutral animation cursor. Consuming, accelerating, or skipping it cannot
    /// mutate the already-resolved authoritative state.
    /// </summary>
    public sealed class PresentationEventQueue
    {
        private IReadOnlyList<SimulationEvent> _events = Array.Empty<SimulationEvent>();
        private int _index;

        public int Remaining => _events.Count - _index;

        public bool IsComplete => _index >= _events.Count;

        public void Begin(PresentationTurn turn)
        {
            _events = turn.Events;
            _index = 0;
        }

        public SimulationEvent? Advance()
        {
            if (IsComplete)
            {
                return null;
            }

            SimulationEvent next = _events[_index];
            _index++;
            return next;
        }

        public void Skip() => _index = _events.Count;
    }
}
