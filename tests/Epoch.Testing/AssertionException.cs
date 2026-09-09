using System;

namespace Epoch.Testing
{
    public sealed class AssertionException : Exception
    {
        public AssertionException(string message)
            : base(message)
        {
        }
    }
}
