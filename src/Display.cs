using System;
using Qtl.DisplayCapture.Globals;
using Windows.Win32.Graphics.Dxgi;

namespace Qtl.DisplayCapture;

/// <summary>
/// An abstraction over <see cref="IDXGIAdapter1"/>* and <see cref="IDXGIOutput1"/>*.
/// </summary>
/// <remarks>
/// <para>Don't forget to dispose this object!</para>
/// </remarks>
public sealed unsafe class Display : IDisposable
{
	private readonly IDXGIAdapter1* _dxgiAdapter1;
	private readonly IDXGIOutput1* _dxgiOutput1;

	private GpuProperties? _gpuProperties;
	private bool _isDisposed;

	/// <summary>
	/// Initialize this <see cref="Display"/> using a <see cref="IDXGIAdapter1"/>* and <see cref="IDXGIOutput1"/>*, anything else could cause crashes.
	/// </summary>
	/// <remarks>
	/// <para>This initializer calls <see cref="IDXGIAdapter1.AddRef"/> and <see cref="IDXGIOutput1.AddRef"/>.</para>
	/// <para><see cref="Dispose()"/> calls <see cref="IDXGIAdapter1.Release"/> and <see cref="IDXGIOutput1.Release"/>.</para>
	/// </remarks>
	/// <param name="dxgiAdapter1"><see cref="IDXGIAdapter1"/>*</param>
	/// <param name="dxgiOutput1"><see cref="IDXGIOutput1"/>*</param>
	/// <exception cref="ArgumentNullException"></exception>
	public Display(nint dxgiAdapter1, nint dxgiOutput1)
	{
		ArgumentNullException.ThrowIfNull(dxgiAdapter1);
		ArgumentNullException.ThrowIfNull(dxgiOutput1);

		_dxgiAdapter1 = (IDXGIAdapter1*)dxgiAdapter1;
		_ = _dxgiAdapter1->AddRef();

		_dxgiOutput1 = (IDXGIOutput1*)dxgiOutput1;
		_ = _dxgiOutput1->AddRef();
	}

	/// <summary>
	/// Initializes and prepares a <see cref="DisplayCapturer"/> for this <see cref="Display"/>.
	/// </summary>
	/// <remarks>
	/// <para>Don't forget to dispose this object!</para>
	/// </remarks>
	/// <returns><see cref="DisplayCapturer"/></returns>
	/// <exception cref="ObjectDisposedException"></exception>
	public DisplayCapturer CreateCapturer()
	{
		if (_isDisposed) { throw new ObjectDisposedException(nameof(Display)); }

		var capturer = default(DisplayCapturer);
		try
		{
			capturer = new DisplayCapturer((nint)_dxgiAdapter1, (nint)_dxgiOutput1);
			capturer.PrepareForCapturing();
			return capturer;
		}
		catch
		{
			capturer?.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Returns all the properties of this <see cref="Display"/>.
	/// </summary>
	/// <remarks>
	/// <para>The returned <see cref="GpuProperties"/> doesn't update between calls.</para>
	/// </remarks>
	/// <returns><see cref="DisplayProperties"/></returns>
	/// <exception cref="ObjectDisposedException"></exception>
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

	/// <summary>
	/// Blocks thread until the next VBlank.
	/// </summary>
	/// <exception cref="ObjectDisposedException"></exception>
	public void Vsync()
	{
		if (_isDisposed) { throw new ObjectDisposedException(nameof(Display)); }

		_dxgiOutput1->WaitForVBlank();
	}

	private void Dispose(bool disposing)
	{
		_ = disposing;

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
