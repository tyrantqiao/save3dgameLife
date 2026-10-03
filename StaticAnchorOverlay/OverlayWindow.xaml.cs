using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace StaticAnchorOverlay;

public partial class OverlayWindow : Window
{
    private readonly AnchorSurface surface = new();
    private IntPtr handle;
    private string device = "";
    private HwndSource? source;
    private bool queued;
    public string? ImageError => surface.ImageError;
    public event Action? DisplaysChanged;
    public OverlayWindow()
    {
        InitializeComponent(); SurfaceHost.Children.Add(surface);
        SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(this).Handle;
            NativeMethods.MakeOverlay(handle);
            source = HwndSource.FromHwnd(handle); source.AddHook(Hook);
            Position();
        };
        Closed += (_, _) => source?.RemoveHook(Hook);
    }
    public void Update(Configuration config)
    {
        device = config.MonitorDevice;
        if (handle != IntPtr.Zero) Position();
        surface.Update(config.Active);
    }
    private void Position()
    {
        var screens = NativeMethods.GetMonitors();
        NativeMethods.Place(handle, screens.FirstOrDefault(m => m.Device == device) ?? screens[0]);
    }
    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_NCHITTEST) { handled = true; return new IntPtr(-1); }
        if (msg == NativeMethods.WM_MOUSEACTIVATE) { handled = true; return new IntPtr(3); }
        if ((msg == NativeMethods.WM_DPICHANGED || msg == NativeMethods.WM_DISPLAYCHANGE) && !queued)
        {
            queued = true;
            // Let WPF finish handling its DPI message before correcting physical bounds.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                queued = false; Position(); surface.InvalidateVisual(); DisplaysChanged?.Invoke();
            }));
        }
        return IntPtr.Zero;
    }
}
