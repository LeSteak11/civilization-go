namespace Epoch.Debug.Inspection
{
    /// <summary>
    /// Human-readable projections of authoritative state for the debug panel.
    /// Debug output must never enter a state hash or consume authoritative RNG
    /// (Technical Plan sec.3.6). Implemented at M5.
    /// </summary>
    public interface StateFormatter
    {
    }
}
