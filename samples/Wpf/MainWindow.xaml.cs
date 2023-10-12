using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Qtl.DisplayCapture;

namespace Qtl.Samples.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private const double FPS = 60.0;

	private DisplayCapturer? _displayCapturer;
	private byte[]? _data;
	private WriteableBitmap? _writableBitmap;
	private DispatcherTimer? _updateTimer;

	public MainWindow()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		var windowHandle = new WindowInteropHelper(this).Handle;
		var monitorHandle = DisplayEnumerator.GetHandleOfMonitorWithWindow(windowHandle);

		using var displayEnumerator = DisplayEnumerator.Create();
		using var display = displayEnumerator.GetDisplayWithMonitorHandle(monitorHandle);

		var displayProperties = display.Properties;
		ShowProperties(displayProperties);

		_displayCapturer = display.CreateCapturer();
		_displayCapturer.CreateBufferAndBitmap(out _data, out _writableBitmap);

		RenderOptions.SetBitmapScalingMode(CapturedImage, BitmapScalingMode.HighQuality);
		RenderOptions.SetEdgeMode(CapturedImage, EdgeMode.Aliased);
		CapturedImage.Source = _writableBitmap;

		_updateTimer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / FPS), DispatcherPriority.Background, Update, Dispatcher);
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		_displayCapturer?.Dispose();
		_data = null;
		_updateTimer?.Stop();
	}

	private void Update(object? sender, EventArgs e)
	{
		if (_displayCapturer is null || _data is null || _writableBitmap is null) { return; }

		_displayCapturer.CaptureInBitmap(_data, _writableBitmap);
	}

	private void ShowProperties(DisplayProperties properties)
	{
		GpuDescriptionLabel.Content = $"Gpu description: {properties.Gpu.Description},";
		DeviceNameLabel.Content = $"Display name: {properties.DeviceName},";
		MonitorHandleLabel.Content = $"Monitor handle: {properties.MonitorHandle},";
	}
}
