using System;
using Qtl.DisplayCapture.Globals;
using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

public sealed unsafe class Display : IDisposable
{
    private readonly IDXGIAdapter1* _dxgiAdapter1;
    private readonly IDXGIOutput1* _dxgiOutput1;

    private GpuProperties? _gpuProperties;
    private bool _isDisposed;

    public Display(void* dxgiAdapter1, void* dxgiOutput1)
    {
        ArgumentNullException.ThrowIfNull(dxgiAdapter1, nameof(dxgiAdapter1));
        ArgumentNullException.ThrowIfNull(dxgiOutput1, nameof(dxgiOutput1));

        _dxgiAdapter1 = (IDXGIAdapter1*)dxgiAdapter1;
        _dxgiOutput1 = (IDXGIOutput1*)dxgiOutput1;
    }

    public DisplayCapturer CreateCapturer()
    {
        if (_isDisposed) { throw new ObjectDisposedException(nameof(Display)); }

        try
        {
            _ = _dxgiAdapter1->AddRef();
            _ = _dxgiOutput1->AddRef();
            var capturer = new DisplayCapturer(_dxgiAdapter1, _dxgiOutput1);
            capturer.PrepareForCapturing();
            return capturer;
        }
        catch
        {
            _ = _dxgiAdapter1->Release();
            _ = _dxgiOutput1->Release();
            throw;
        }
    }

    public DisplayProperties Properties
    {
        get
        {
            if (_isDisposed) { throw new ObjectDisposedException(nameof(Display)); }

            if (_gpuProperties is null)
            {
                DXGI_ADAPTER_DESC1 adapterDesc;
                _dxgiAdapter1->GetDesc1(&adapterDesc);
                _gpuProperties = new GpuProperties(&adapterDesc);
            }

            DXGI_OUTPUT_DESC outputDesc;
            _dxgiOutput1->GetDesc(&outputDesc);
            return new DisplayProperties(_gpuProperties, &outputDesc);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) { return; }
        _isDisposed = true;

        Releaser.ReleaseIfNotNull(_dxgiAdapter1);
        Releaser.ReleaseIfNotNull(_dxgiOutput1);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Display()
    {
        Dispose(false);
    }
}
