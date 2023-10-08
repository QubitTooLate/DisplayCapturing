using Qtl.DisplayCapture.Extensions;
using Qtl.DisplayCapture.Globals;
using Qtl.DisplayCapturing.Extensions;
using System;
using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Gdi;

namespace Qtl.DisplayCapture;

public sealed unsafe class DisplayEnumerator : IDisposable
{
    public static DisplayEnumerator Create()
    {
        IDXGIFactory1* dxgiFactory1;
        try
        {
            var guid = typeof(IDXGIFactory1).GUID;
            _ = Native.CreateDXGIFactory1(
                &guid,
                (void**)&dxgiFactory1
            ).ThrowOnFailure();

            return new(dxgiFactory1);
        }
        finally
        {
            Releaser.SafeRelease(&dxgiFactory1);
        }
    }

    public static IntPtr GetHandleOfPrimaryMonitor() => Native.MonitorFromPoint(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

    public static IntPtr GetHandleOfMonitorWithWindow(IntPtr windowHandle) => Native.MonitorFromWindow((HWND)windowHandle, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);

    private static bool TryGetDisplay(uint i, nint gpu, out nint display)
    {
        IDXGIOutput1* dxgiOutput1;
        var result = IDXGIAdapter1Extensions.EnumOutputs1((IDXGIAdapter1*)gpu, i, &dxgiOutput1).Succeeded;
        display = default;
        if (!result) { return false; }

        display = (nint)dxgiOutput1;
        return true;
    }

    private readonly IDXGIFactory1* _dxgiFactory1;

    private bool _isDisposed;

    private bool TryGetGpu(uint i, out nint gpu)
    {
        IDXGIAdapter1* dxgiAdapter1;
        var result = _dxgiFactory1->EnumAdapters1(i, &dxgiAdapter1).Succeeded;
        gpu = default;
        if (!result) { return false; }

        gpu = (nint)dxgiAdapter1;
        return true;
    }

    public DisplayEnumerator(void* dxgiFactory1)
    {
        ArgumentNullException.ThrowIfNull(dxgiFactory1, nameof(dxgiFactory1));

        _dxgiFactory1 = (IDXGIFactory1*)dxgiFactory1;
        _dxgiFactory1->AddRef();
    }

    public IEnumerable<Display> GetAvailableDisplays()
    {
        for (var ai = 0u; TryGetGpu(ai, out var gpu); ai++)
        {
            for (var oi = 0u; TryGetDisplay(oi, gpu, out var display); oi++)
            {
                yield return new Display(gpu, display);

                Releaser.ReleaseIfNotNull(display);
            }

            Releaser.ReleaseIfNotNull(gpu);
        }
    }

    public IEnumerable<DisplayProperties> GetPropertiesOfAvailableDisplays() => GetAvailableDisplays()
        .SelectAndDispose(display => display.Properties);

    public Display GetDisplayWithMonitorHandle(IntPtr monitorHandle) => GetAvailableDisplays()
        .FirstOrDefaultAndDisposeOthers(display => display.Properties.MonitorHandle == monitorHandle) ?? throw new KeyNotFoundException();

    private void Dispose(bool disposing)
    {
        if (_isDisposed) { return; }
        _isDisposed = true;

        Releaser.ReleaseIfNotNull(_dxgiFactory1);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DisplayEnumerator()
    {
        Dispose(false);
    }
}
