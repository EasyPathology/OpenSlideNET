using System;
#nullable enable
namespace OpenSlideNET.Extensions;

internal static class NotNullExtension
{
    public static T NotNull<T>(this T? value) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return value;
    }
}