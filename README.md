# Qtl.DisplayCapturing

A C# wrapper over [The Desktop Duplication API](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/desktop-dup-api), part of [DirectX 11](https://learn.microsoft.com/en-us/windows/win32/getting-started-with-directx-graphics).

This is one of the libraries I wish I had when I started programming in C# and messing around with the Windows APIs. It would have saved me from a lot of issues I had with [GDI+](https://learn.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-gdi-start).

**Why?** So C# developers would stop using `Graphics.CopyFromScreen` or anything else of [the GDI+ API](https://learn.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-gdi-start) which shouldn't really be used past [__**Windows 7**__](https://learn.microsoft.com/en-us/windows/win32/direct2d/comparing-direct2d-and-gdi#conclusion)!

## Table of Contents

* [Goals](#goals)
* [Samples](#samples)
* [License](#license)

### Samples

* [The WPF Sample](/samples/Wpf) shows how to capture the display on which the WPF window itself is and how fast and effortlessly the display can repeatadly be captured.

### Goals

`Qtl.DisplayCapturing` is aiming to reduce the usage of [`System.Drawing.Graphics`](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics?view=dotnet-plat-ext-7.0) and use newer more performant APIs instead.

* Reduce the usage of [The GDI+ API](https://learn.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-gdi-start).
* Making this an easy to use and integrate API.
* Integrating with WPF and WinForms.

### License

Copyright © Jester and Contributors. Licensed under the MIT License
(MIT). See [LICENSE](/LICENSE) in the repository root for more information.
