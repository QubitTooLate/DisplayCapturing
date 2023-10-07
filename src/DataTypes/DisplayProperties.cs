using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

public record DisplayProperties(
    GpuProperties Gpu,
    bool AttachedToDesktop,
    Rect DesktopCoordinates,
    string DeviceName,
    nint MonitorHandle,
    Rotation Rotation
)
{
    internal unsafe DisplayProperties(GpuProperties gpu, DXGI_OUTPUT_DESC* desc) : this(
        gpu,
        desc->AttachedToDesktop,
        new Rect(
            desc->DesktopCoordinates.left,
            desc->DesktopCoordinates.top,
            desc->DesktopCoordinates.right,
            desc->DesktopCoordinates.bottom
        ),
        new string(desc->DeviceName.AsSpan()),
        desc->Monitor,
        (Rotation)desc->Rotation
    )
    {

    }
}
