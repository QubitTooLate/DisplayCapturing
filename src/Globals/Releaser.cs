using Windows.Win32.System.Com;

namespace Qtl.DisplayCapture.Globals;

internal static unsafe class Releaser
{
    internal static void ReleaseIfNotNull<T>(T* ptr) where T : unmanaged
    {
        if (ptr is null) { return; }
        var unknown = (IUnknown*)ptr;
        unknown->Release();
    }

    internal static void SafeRelease<T>(T** ptr) where T : unmanaged
    {
        if (ptr is null) { return; }
        var unknown = (IUnknown*)*ptr;
        if (unknown is null) { return; }
        unknown->Release();
        *ptr = null;
    }
}
