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

/// <summary>
/// An abstraction over <see cref="IDXGIFactory1"/>*.
/// </summary>
/// <remarks>
/// <para>Don't forget to dispose this object!</para>
/// </remarks>
public sealed unsafe class DisplayEnumerator : IDisposable
{
    /// <summary>
    /// Creates a <see cref="DisplayEnumerator"/>.
    /// </summary>
    /// <returns><see cref="DisplayEnumerator"/></returns>
    /// <exception cref="Exception"></exception>
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

            return new((nint)dxgiFactory1);
        }
        finally
        {
            Releaser.SafeRelease(&dxgiFactory1);
        }
    }

    /// <summary>
    /// The monitor handle can be used to get a <see cref="Display"/> from <see cref="GetDisplayWithMonitorHandle(nint)"/>.
    /// </summary>
    /// <returns>HMONITOR</returns>
    public static IntPtr GetHandleOfPrimaryMonitor() => Native.MonitorFromPoint(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

    /// <summary>
    /// The monitor handle can be used to get a <see cref="Display"/> from <see cref="GetDisplayWithMonitorHandle(nint)"/>.
    /// If no monitor is found with the <paramref name="windowHandle"/> it returns the primary monitor.
    /// </summary>
    /// <param name="windowHandle">A HWND</param>
    /// <returns>HMONITOR</returns>
    public static IntPtr GetHandleOfMonitorWithWindow(IntPtr windowHandle) => Native.MonitorFromWindow((HWND)windowHandle, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);

    // using nint instead of pointers so the code is "safe"
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

    // using nint instead of pointers so the code is "safe"
    private bool TryGetGpu(uint i, out nint gpu)
    {
        IDXGIAdapter1* dxgiAdapter1;
        var result = _dxgiFactory1->EnumAdapters1(i, &dxgiAdapter1).Succeeded;
        gpu = default;
        if (!result) { return false; }

        gpu = (nint)dxgiAdapter1;
        return true;
    }

    /// <summary>
    /// Initialize this <see cref="DisplayEnumerator"/> using a <see cref="IDXGIFactory1"/>*, anything else could cause crashes.
    /// </summary>
    /// <remarks>
    /// <para>This initializer calls <see cref="IDXGIFactory1.AddRef"/>.</para>
    /// <para><see cref="Dispose()"/> calls <see cref="IDXGIFactory1.Release"/>.</para>
    /// </remarks>
    /// <param name="dxgiFactory1"><see cref="IDXGIAdapter1"/>*</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DisplayEnumerator(nint dxgiFactory1)
    {
        ArgumentNullException.ThrowIfNull(dxgiFactory1, nameof(dxgiFactory1));

        _dxgiFactory1 = (IDXGIFactory1*)dxgiFactory1;
        _dxgiFactory1->AddRef();
    }

    /// <summary>
    /// Enumerates over the available displays.
    /// </summary>
    /// <remarks>
    /// <para>Don't forget to dispose each <see cref="Display"/>!</para>
    /// </remarks>
    /// <returns><see cref="IEnumerable{T}"/></returns>
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

    /// <summary>
    /// Enumerates over the properties of the available displays.
    /// The <see cref="DisplayProperties.MonitorHandle"/> can be used in <see cref="GetDisplayWithMonitorHandle(nint)"/> to get the <see cref="Display"/> for the <see cref="DisplayProperties"/>.
    /// </summary>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    public IEnumerable<DisplayProperties> GetPropertiesOfAvailableDisplays() => GetAvailableDisplays()
        .SelectAndDispose(display => display.Properties);

    /// <summary>
    /// Enumerates over the available displays to get the one with the monitor handle.
    /// </summary>
    /// <param name="monitorHandle">HMONITOR of which to get the display.</param>
    /// <returns><see cref="Display"/></returns>
    /// <exception cref="KeyNotFoundException"></exception>
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
