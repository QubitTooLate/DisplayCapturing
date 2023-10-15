using System;
using Qtl.DisplayCapture.Globals;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Qtl.DisplayCapture;

/// <summary>
/// An abstraction over <see cref="IDXGIAdapter1"/>* and <see cref="IDXGIOutput1"/>*.
/// </summary>
/// <remarks>
/// <para>Don't forget to dispose this object!</para>
/// </remarks>
public sealed unsafe class DisplayCapturer : IDisposable
{
	private const string INVALID_OPERATION_MESSAGE = $"Call: {nameof(PrepareForCapturing)} first!";

	// A frame cannot be read on the cpu so there is a need for a frame buffer which can be read on the cpu.
	private static ID3D11Texture2D* CreateFrameBufferForFrame(ID3D11Device* d3d11Device, ID3D11Texture2D* d3d11Texture2DFrame, out uint width, out uint height)
	{
		ID3D11Texture2D* d3d11Texture2DFrameBuffer;
		try
		{
			D3D11_TEXTURE2D_DESC textureDesc;
			d3d11Texture2DFrame->GetDesc(&textureDesc);
			width = textureDesc.Width;
			height = textureDesc.Height;

			textureDesc.BindFlags = 0;
			textureDesc.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
			textureDesc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
			textureDesc.MiscFlags = 0;

			d3d11Device->CreateTexture2D(&textureDesc, null, &d3d11Texture2DFrameBuffer);

			return d3d11Texture2DFrameBuffer;
		}
		catch
		{
			Releaser.SafeRelease(&d3d11Texture2DFrameBuffer);
			throw;
		}
	}

	private static ID3D11Texture2D* CaptureFrame(IDXGIOutputDuplication* dxgiOutputDuplication, uint timeoutInMilliseconds)
	{
		IDXGIResource* dxgiResource;
		ID3D11Texture2D* d3d11Texture2D;
		try
		{
			DXGI_OUTDUPL_FRAME_INFO info;
			dxgiOutputDuplication->AcquireNextFrame(timeoutInMilliseconds, &info, &dxgiResource);

			var guid = typeof(ID3D11Texture2D).GUID;
			_ = dxgiResource->QueryInterface(&guid, (void**)&d3d11Texture2D).ThrowOnFailure();

			return d3d11Texture2D;
		}
		catch
		{
			Releaser.SafeRelease(&d3d11Texture2D);
			throw;
		}
		finally
		{
			Releaser.SafeRelease(&dxgiResource);
		}
	}

	private static uint GetStride(ID3D11DeviceContext* d3d11DeviceContext, ID3D11Texture2D* d3d11Texture2D)
	{
		D3D11_MAPPED_SUBRESOURCE mapped;
		d3d11DeviceContext->Map((ID3D11Resource*)d3d11Texture2D, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped);
		d3d11DeviceContext->Unmap((ID3D11Resource*)d3d11Texture2D, 0);
		return mapped.RowPitch;
	}

	public static bool TryExcludeWindowFromCapture(IntPtr windowHandle) => Native.SetWindowDisplayAffinity((HWND)windowHandle, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);

	public static bool TryIncludeExcludedWindowFromCapture(IntPtr windowHandle) => Native.SetWindowDisplayAffinity((HWND)windowHandle, WINDOW_DISPLAY_AFFINITY.WDA_NONE);

	private readonly IDXGIAdapter1* _dxgiAdapter1;
	private readonly IDXGIOutput1* _dxgiOutput1;

	private bool _areResourcesCreated;
	private ID3D11DeviceContext* _d3d11DeviceContext;
	private IDXGIOutputDuplication* _dxgiOutputDuplication;
	private ID3D11Texture2D* _d3d11Texture2DFrameBuffer;
	private uint _frameBufferWidth;
	private uint _frameBufferHeight;
	private uint _frameBufferStride;
	private bool _isDisposed;

	/// <summary>
	/// Initialize this <see cref="DisplayCapturer"/> using a <see cref="IDXGIAdapter1"/>* and <see cref="IDXGIOutput1"/>*, anything else could cause crashes.
	/// </summary>
	/// <remarks>
	/// <para>This initializer calls <see cref="IDXGIAdapter1.AddRef"/> and <see cref="IDXGIOutput1.AddRef"/>.</para>
	/// <para><see cref="Dispose()"/> calls <see cref="IDXGIAdapter1.Release"/> and <see cref="IDXGIOutput1.Release"/>.</para>
	/// </remarks>
	/// <param name="dxgiAdapter1"><see cref="IDXGIAdapter1"/>*</param>
	/// <param name="dxgiOutput1"><see cref="IDXGIOutput1"/>*</param>
	/// <exception cref="ArgumentNullException"></exception>
	public DisplayCapturer(nint dxgiAdapter1, nint dxgiOutput1)
	{
		ArgumentNullException.ThrowIfNull(dxgiAdapter1);
		ArgumentNullException.ThrowIfNull(dxgiOutput1);

		_dxgiAdapter1 = (IDXGIAdapter1*)dxgiAdapter1;
		_ = _dxgiAdapter1->AddRef();

		_dxgiOutput1 = (IDXGIOutput1*)dxgiOutput1;
		_ = _dxgiOutput1->AddRef();
	}

	/// <summary>
	/// Initializes the necessary objects to make capturing possible.
	/// </summary>
	/// <param name="timeoutInMilliseconds">If it takes longer than this to capture a frame, throw.</param>
	/// <exception cref="Exception"></exception>
	/// <exception cref="ObjectDisposedException"></exception>
	public void PrepareForCapturing(uint timeoutInMilliseconds = 1000)
	{
		if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayCapturer)); }

		if (_areResourcesCreated) { return; }
		_areResourcesCreated = true;

		ID3D11Device* d3d11Device;
		ID3D11DeviceContext* d3d11DeviceContext;
		IDXGIOutputDuplication* dxgiOutputDuplication;
		ID3D11Texture2D* d3d11Texture2DFrame;
		ID3D11Texture2D* d3d11Texture2DFrameBuffer;
		try
		{
			_ = Native.D3D11CreateDevice(
				(IDXGIAdapter*)_dxgiAdapter1,
				Windows.Win32.Graphics.Direct3D.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
				HMODULE.Null,
#if DEBUG
				D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG |
#endif
				D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_SINGLETHREADED | D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
				null,
				7,
				&d3d11Device,
				null,
				&d3d11DeviceContext
			).ThrowOnFailure();

			_dxgiOutput1->DuplicateOutput((IUnknown*)d3d11Device, &dxgiOutputDuplication);

			d3d11Texture2DFrame = CaptureFrame(dxgiOutputDuplication, timeoutInMilliseconds);

			d3d11Texture2DFrameBuffer = CreateFrameBufferForFrame(d3d11Device, d3d11Texture2DFrame, out var width, out var height);

			var stride = GetStride(d3d11DeviceContext, d3d11Texture2DFrameBuffer);

			_d3d11DeviceContext = d3d11DeviceContext;
			_dxgiOutputDuplication = dxgiOutputDuplication;
			_d3d11Texture2DFrameBuffer = d3d11Texture2DFrameBuffer;
			_frameBufferWidth = width;
			_frameBufferHeight = height;
			_frameBufferStride = stride;
		}
		catch
		{
			Releaser.SafeRelease(&d3d11DeviceContext);
			Releaser.SafeRelease(&dxgiOutputDuplication);
			Releaser.SafeRelease(&d3d11Texture2DFrameBuffer);
			throw;
		}
		finally
		{
			Releaser.SafeRelease(&d3d11Device);
			Releaser.SafeRelease(&d3d11Texture2DFrame);
		}
	}

	private void CaptureFrameInFrameBuffer(uint timeoutInMilliseconds)
	{
		ID3D11Texture2D* d3d11Texture2DFrame;
		try
		{
			_dxgiOutputDuplication->ReleaseFrame();
			d3d11Texture2DFrame = CaptureFrame(_dxgiOutputDuplication, timeoutInMilliseconds);
			_d3d11DeviceContext->CopyResource((ID3D11Resource*)_d3d11Texture2DFrameBuffer, (ID3D11Resource*)d3d11Texture2DFrame);
		}
		finally
		{
			Releaser.SafeRelease(&d3d11Texture2DFrame);
		}
	}

	/// <summary>
	/// Captures a frame and copies it to the provided buffer.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="buffer">Buffer to copy the pixels of the captured frame to. (Recommended byte size of buffer: <see cref="RequiredBufferSizeInBytes"/>)</param>
	/// <param name="timeoutInMilliseconds">If it takes longer than this to capture a frame, throw.</param>
	/// <exception cref="Exception"></exception>
	/// <exception cref="ObjectDisposedException"></exception>
	/// <exception cref="ArgumentException"></exception>
	public void CaptureInBuffer<T>(Span<T> buffer, uint timeoutInMilliseconds = 1000) where T : unmanaged
	{
		if (_isDisposed) { throw new ObjectDisposedException(nameof(DisplayCapturer)); }

		PrepareForCapturing(timeoutInMilliseconds);
		CaptureFrameInFrameBuffer(timeoutInMilliseconds);

		try
		{
			D3D11_MAPPED_SUBRESOURCE mapped;
			_d3d11DeviceContext->Map((ID3D11Resource*)_d3d11Texture2DFrameBuffer, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped);

			new Span<T>(mapped.pData, (int)(mapped.RowPitch * _frameBufferHeight) / sizeof(T)).CopyTo(buffer);
		}
		finally
		{
			_d3d11DeviceContext->Unmap((ID3D11Resource*)_d3d11Texture2DFrameBuffer, 0);
		}
	}

	/// <exception cref="InvalidOperationException"></exception>
	public int FrameWidth => _areResourcesCreated ? unchecked((int)_frameBufferWidth) : throw new InvalidOperationException(INVALID_OPERATION_MESSAGE);

	/// <exception cref="InvalidOperationException"></exception>
	public int FrameHeight => _areResourcesCreated ? unchecked((int)_frameBufferHeight) : throw new InvalidOperationException(INVALID_OPERATION_MESSAGE);

	/// <exception cref="InvalidOperationException"></exception>
	public int FrameStride => _areResourcesCreated ? unchecked((int)_frameBufferStride) : throw new InvalidOperationException(INVALID_OPERATION_MESSAGE);

	/// <exception cref="InvalidOperationException"></exception>
	public int RequiredBufferSizeInBytes => _areResourcesCreated ? unchecked((int)(_frameBufferStride * _frameBufferHeight)) : throw new InvalidOperationException(INVALID_OPERATION_MESSAGE);

	private void Dispose(bool disposing)
	{
		_ = disposing;

		if (_isDisposed) { return; }
		_isDisposed = true;

		Releaser.ReleaseIfNotNull(_dxgiAdapter1);
		Releaser.ReleaseIfNotNull(_dxgiOutput1);

		Releaser.ReleaseIfNotNull(_d3d11DeviceContext);
		Releaser.ReleaseIfNotNull(_dxgiOutputDuplication);
		Releaser.ReleaseIfNotNull(_d3d11Texture2DFrameBuffer);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~DisplayCapturer()
	{
		Dispose(false);
	}
}
