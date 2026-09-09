// netstandard2.1 does not ship System.Runtime.CompilerServices.IsExternalInit,
// which C# 9+ record and init-only members require. Compiler-only shim.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
