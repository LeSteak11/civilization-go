namespace Epoch.Application.Runs
{
    /// <summary>
    /// Orchestration only: creates a run, asks the Snapshot provider for its command,
    /// accepts one human command, invokes the Core once, records the result, and
    /// publishes a presentation model. It owns no gameplay outcome
    /// (Technical Plan sec.3.4). Implemented at M3.
    /// </summary>
    public interface RunCoordinator
    {
        Core.Domain.RunState State { get; }

        Core.Domain.CardOfferSet Offers { get; }

        PresentationTurn Submit(Core.Domain.Selection selection);
    }
}
