using System;
using Windows.Win32.System.Com;

namespace Qtl.DisplayCapture.Globals;

internal unsafe struct Releaser : IDisposable
{
    public static void ReleaseIfNotNull(nint ptr)
    {
        if (ptr is 0) { return; }
        var unknown = (IUnknown*)ptr;
        unknown->Release();
    }

    public static void ReleaseIfNotNull<T>(T* ptr) where T : unmanaged
    {
        if (ptr is null) { return; }
        var unknown = (IUnknown*)ptr;
        unknown->Release();
    }

    public static void SafeRelease<T>(T** ptr) where T : unmanaged
    {
        if (ptr is null) { return; }
        var unknown = (IUnknown*)*ptr;
        if (unknown is null) { return; }
        unknown->Release();
        *ptr = null;
    }

    public static Releaser For<T>(T** ptr) where T : unmanaged => new((IUnknown**)ptr);

    private readonly IUnknown** _unknownPtr;

    private Releaser(IUnknown** unknownPtr)
    {
        _unknownPtr = unknownPtr;
    }

    public void Dispose()
    {
        SafeRelease(_unknownPtr);
    }
}
