using Qtl.DisplayCapture;
using System;
using System.Collections.Generic;

namespace Qtl.DisplayCapturing.Extensions;

public static class IEnumerableDisplaysExtensions
{
    public static Display? FirstOrDefaultAndDisposeOthers(this IEnumerable<Display> displays, Func<Display, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(displays, nameof(displays));
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        foreach (var display in displays)
        {
            if (predicate(display))
            {
                return display;
            }

            display.Dispose();
        }

        return null;
    }

    public static IEnumerable<TResult> SelectAndDispose<TResult>(this IEnumerable<Display> displays, Func<Display, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(displays, nameof(displays));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        foreach (var display in displays)
        {
            yield return selector(display);
            display.Dispose();
        }
    }
}
