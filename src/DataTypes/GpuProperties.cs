using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

public record GpuProperties(
    long Luid,
    nuint DedicatedSystemMemory,
    nuint DedicatedVideoMemory,
    string Description,
    uint DeviceId,
    uint Flags,
    uint Revision,
    nuint SharedSystemMemory,
    uint SubSysId,
    uint VendorId
)
{
    internal unsafe GpuProperties(DXGI_ADAPTER_DESC1* desc) : this(
        (long)desc->AdapterLuid.HighPart << 32 | desc->AdapterLuid.LowPart,
        desc->DedicatedSystemMemory,
        desc->DedicatedVideoMemory,
        new string(desc->Description.Value, 0, desc->Description.Length),
        desc->DeviceId,
        desc->Flags,
        desc->Revision,
        desc->SharedSystemMemory,
        desc->SubSysId,
        desc->VendorId
    )
    {

    }
}
