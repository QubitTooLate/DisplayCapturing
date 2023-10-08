using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

/// <summary>
/// Record with all the properties of a display.
/// </summary>
/// <remarks>
/// <para><see href="https://learn.microsoft.com/en-us/windows/win32/api/dxgi/ns-dxgi-dxgi_output_desc">Read more on docs.microsoft.com</see>.</para>
/// </remarks>
/// <param name="Gpu"><inheritdoc cref="GpuProperties"/></param>
/// <param name="AttachedToDesktop">True if the output is attached to the desktop; otherwise, false.</param>
/// <param name="DesktopCoordinates">A Rect structure containing the bounds of the output in desktop coordinates.</param>
/// <param name="DeviceName">A string that contains the name of the output device.</param>
/// <param name="MonitorHandle">An HMONITOR handle that represents the display monitor.</param>
/// <param name="Rotation">A member of the DisplayRotation enumerated type describing on how an image is rotated by the output.</param>
public record DisplayProperties(
    GpuProperties Gpu,
    bool AttachedToDesktop,
    Rect DesktopCoordinates,
    string DeviceName,
    nint MonitorHandle,
    DisplayRotation Rotation
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
        (DisplayRotation)desc->Rotation
    )
    {

    }
}
