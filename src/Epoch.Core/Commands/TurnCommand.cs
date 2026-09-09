using Epoch.Core.Domain;

namespace Epoch.Core.Commands
{
    /// <summary>Core Spec sec.6 contract; Technical Plan sec.6.</summary>
    public abstract record TurnCommand(int Turn, Side Side);
}
