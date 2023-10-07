using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture.Extensions;

internal static unsafe class IDXGIAdapter1Extensions
{
    internal static HRESULT EnumOutputs1(IDXGIAdapter1* self, uint i, IDXGIOutput1** outDxgiOutput1)
    {
        IDXGIOutput* dxgiOutput;
        var hresult = self->EnumOutputs(i, &dxgiOutput);
        if (hresult.Failed) { return hresult; }

        var output1Guid = typeof(IDXGIOutput1).GUID;
        hresult = dxgiOutput->QueryInterface(&output1Guid, (void**)outDxgiOutput1);
        _ = dxgiOutput->Release();
        return hresult;
    }
}
