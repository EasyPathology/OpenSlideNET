using System.Diagnostics.CodeAnalysis;
using OpenSlideNET.Interop;

namespace OpenSlideNET
{
    internal static class ThrowHelper
    {
        internal static void CheckAndThrowError(OpenSlideImageSafeHandle osr)
        {
            var message = OpenSlideInterop.GetError(osr);
            if (message != null) ThrowOpenSlideException(message);
        }

        private static void ThrowOpenSlideException(string message) => throw new OpenSlideException(message);

        internal static bool TryCheckError(OpenSlideImageSafeHandle osr,[NotNullWhen(false)] out string? message) =>
            (message = OpenSlideInterop.GetError(osr)) == null;
    }
}
