using System;
using System.Collections.Generic;
using Qtl.DisplayCapture;

namespace Qtl.DisplayCapturing.Extensions;

public static class IEnumerableDisplaysExtensions
{
	/// <summary>
	/// Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.
	/// In addition it also disposes all elements up to the element that satisfies the condition.
	/// </summary>
	/// <param name="displays">An <see cref="IEnumerable{Display}" /> to return an element from.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns>Display or null if none satisfy the condition.</returns>
	public static Display? FirstOrDefaultAndDisposeOthers(this IEnumerable<Display> displays, Func<Display, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(displays);
		ArgumentNullException.ThrowIfNull(predicate);

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

	/// <summary>
	/// Projects each element of a sequence into a form.
	/// In addition it also disposes every element.
	/// </summary>
	/// <typeparam name="TResult">The result should not be a Display.</typeparam>
	/// <param name="displays">An <see cref="IEnumerable{Display}" /> to return results from.</param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static IEnumerable<TResult> SelectAndDispose<TResult>(this IEnumerable<Display> displays, Func<Display, TResult> selector)
	{
		ArgumentNullException.ThrowIfNull(displays);
		ArgumentNullException.ThrowIfNull(selector);

		foreach (var display in displays)
		{
			yield return selector(display);
			display.Dispose();
		}
	}
}
