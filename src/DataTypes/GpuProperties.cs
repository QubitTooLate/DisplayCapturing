using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

/// <summary>
/// Record with all the properties of a gpu.
/// </summary>
/// <remarks>
/// <para><see href="https://learn.microsoft.com/en-us/windows/win32/api/dxgi/ns-dxgi-dxgi_adapter_desc1">Read more on docs.microsoft.com</see>.</para>
/// </remarks>
/// <param name="Luid">A unique value that identifies the adapter.</param>
/// <param name="DedicatedSystemMemory">The number of bytes of dedicated system memory that are not shared with the CPU.</param>
/// <param name="DedicatedVideoMemory">The number of bytes of dedicated video memory that are not shared with the CPU.</param>
/// <param name="Description">A string that contains the adapter description.</param>
/// <param name="DeviceId">The PCI ID of the hardware device.</param>
/// <param name="Flags">A value that describes the adapter type.</param>
/// <param name="Revision">The PCI ID of the revision number of the adapter.</param>
/// <param name="SharedSystemMemory">The number of bytes of shared system memory.</param>
/// <param name="SubSysId">The PCI ID of the sub system.</param>
/// <param name="VendorId">The PCI ID of the hardware vendor.</param>
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
		((long)desc->AdapterLuid.HighPart << 32) | desc->AdapterLuid.LowPart,
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
