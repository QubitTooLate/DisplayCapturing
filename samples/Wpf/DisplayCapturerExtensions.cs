using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Qtl.DisplayCapture;

namespace Qtl.Samples.Wpf;

internal static class DisplayCapturerExtensions
{
	public static void CreateBufferAndBitmap(this DisplayCapturer self, out byte[] buffer, out WriteableBitmap bitmap)
	{
		buffer = new byte[self.RequiredBufferSizeInBytes];

		bitmap = new WriteableBitmap(
			self.FrameWidth,
			self.FrameHeight,
			0.0,
			0.0,
			PixelFormats.Pbgra32,
			null
		);
	}

	public static void CaptureInBitmap(this DisplayCapturer self, byte[] buffer, WriteableBitmap bitmap)
	{
		self.CaptureInBuffer(buffer.AsSpan());

		try
		{
			bitmap.Lock();

			bitmap.WritePixels(
				new Int32Rect(0, 0, self.FrameWidth, self.FrameHeight),
				buffer,
				self.FrameStride,
				0
			);
		}
		finally
		{
			bitmap.Unlock();
		}
	}
}
