using System;
using Windows.Win32.System.Com;

namespace Qtl.DisplayCapture.Globals;

/// <summary>
/// A collection of functions to safely release <see cref="IUnknown"/>s.
/// </summary>
internal static unsafe class Releaser
{
	public static void ReleaseIfNotNull(nint ptr)
	{
		if (ptr is 0) { return; }
		var unknown = (IUnknown*)ptr;
		_ = unknown->Release();
	}

	public static void ReleaseIfNotNull<T>(T* ptr) where T : unmanaged
	{
		if (ptr is null) { return; }
		var unknown = (IUnknown*)ptr;
		_ = unknown->Release();
	}

	/// <summary>
	/// Releases <see cref="IUnknown"/> if it's not null and sets it to null after release.
	/// </summary>
	/// <remarks>
	/// <para><see href="https://learn.microsoft.com/en-us/windows/win32/medfound/saferelease">Read more on learn.microsoft.com</see></para>
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	/// <param name="ptr"></param>
	public static void SafeRelease<T>(T** ptr) where T : unmanaged
	{
		if (ptr is null) { return; }
		var unknown = (IUnknown*)*ptr;
		if (unknown is null) { return; }
		_ = unknown->Release();
		*ptr = null;
	}
}
