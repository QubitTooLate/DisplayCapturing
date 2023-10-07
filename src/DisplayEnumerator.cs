using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Qtl.DisplayCapture.Collections;
using Qtl.DisplayCapture.Extensions;
using Qtl.DisplayCapture.Globals;
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

        var guid = typeof(IDXGIFactory1).GUID;
        _ = Native.CreateDXGIFactory1(
            &guid,
            (void**)&dxgiFactory1
        ).ThrowOnFailure();

        return new(dxgiFactory1);
    }

    private readonly IDXGIFactory1* _dxgiFactory1;

    private bool _isDisposed;

    public DisplayEnumerator(void* dxgiFactory1)
    {
        ArgumentNullException.ThrowIfNull(dxgiFactory1, nameof(dxgiFactory1));

        _dxgiFactory1 = (IDXGIFactory1*)dxgiFactory1;
    }

    public static IntPtr GetHandleOfPrimaryMonitor() => Native.MonitorFromPoint(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

    public static IntPtr GetHandleOfMonitorWithWindow(IntPtr windowHandle) => Native.MonitorFromWindow((HWND)windowHandle, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);

    public IReadOnlyList<DisplayProperties> GetPropertiesOfAvailableDisplays()
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayEnumerator)); }

        var propertiesOfDisplays = new List<DisplayProperties>();

        IDXGIAdapter1* dxgiAdapter1;
        for (var ai = 0u; _dxgiFactory1->EnumAdapters1(ai, &dxgiAdapter1).Succeeded; ai++)
        {
            try
            {
                DXGI_ADAPTER_DESC1 adapterDesc;
                dxgiAdapter1->GetDesc1(&adapterDesc);
                var gpuProperties = new GpuProperties(&adapterDesc);

                IDXGIOutput1* dxgiOutput1;
                for (var oi = 0u; IDXGIAdapter1Extensions.EnumOutputs1(dxgiAdapter1, oi, &dxgiOutput1).Succeeded; oi++)
                {
                    try
                    {
                        DXGI_OUTPUT_DESC outputDesc;
                        dxgiOutput1->GetDesc(&outputDesc);
                        var displayProperties = new DisplayProperties(gpuProperties, &outputDesc);
                        propertiesOfDisplays.Add(displayProperties);
                    }
                    finally
                    {
                        Releaser.SafeRelease(&dxgiOutput1);
                    }
                }
            }
            finally
            {
                Releaser.SafeRelease(&dxgiAdapter1);
            }
        }

        return propertiesOfDisplays;
    }

    public IEnumerable<DisplayProperties> EnumeratePropertiesOfAvailableDisplays()
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayEnumerator)); }

        using var displays = GetDisplays();

        foreach (var display in displays)
        {
            yield return display.Properties;
        }
    }

    public IDisposableReadonlyList<Display> GetDisplays()
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayEnumerator)); }

        var displays = new List<Display>();

        IDXGIAdapter1* dxgiAdapter1;
        for (var ai = 0u; _dxgiFactory1->EnumAdapters1(ai, &dxgiAdapter1).Succeeded; ai++)
        {
            try
            {
                IDXGIOutput1* dxgiOutput1;
                for (var oi = 0u; IDXGIAdapter1Extensions.EnumOutputs1(dxgiAdapter1, oi, &dxgiOutput1).Succeeded; oi++)
                {
                    _ = dxgiAdapter1->AddRef();
                    var display = new Display(dxgiAdapter1, dxgiOutput1);
                    displays.Add(display);
                }
            }
            finally
            {
                Releaser.SafeRelease(&dxgiAdapter1);
            }
        }

        return new DisposableReadonlyList<Display>(displays);
    }

    public bool TryGetDisplayWithMonitorHandle(IntPtr monitorHandle, [NotNullWhen(true)] out Display? display)
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayEnumerator)); }

        IDXGIAdapter1* dxgiAdapter1;
        for (var ai = 0u; _dxgiFactory1->EnumAdapters1(ai, &dxgiAdapter1).Succeeded; ai++)
        {
            try
            {
                IDXGIOutput1* dxgiOutput1;
                for (var oi = 0u; IDXGIAdapter1Extensions.EnumOutputs1(dxgiAdapter1, oi, &dxgiOutput1).Succeeded; oi++)
                {
                    try
                    {
                        DXGI_OUTPUT_DESC outputDesc;
                        dxgiOutput1->GetDesc(&outputDesc);
                        if (outputDesc.Monitor != monitorHandle)
                        {
                            continue;
                        }

                        _ = dxgiAdapter1->AddRef();
                        _ = dxgiOutput1->AddRef();
                        display = new Display(dxgiAdapter1, dxgiOutput1);
                        return true;
                    }
                    finally
                    {
                        Releaser.SafeRelease(&dxgiOutput1);
                    }
                }
            }
            finally
            {
                Releaser.SafeRelease(&dxgiAdapter1);
            }
        }

        display = null;
        return false;
    }

    public Display GetDisplayWithMonitorHandle(IntPtr monitorHandle)
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayEnumerator)); }

        if (TryGetDisplayWithMonitorHandle(monitorHandle, out var display))
        {
            return display;
        }

        throw new KeyNotFoundException();
    }

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
