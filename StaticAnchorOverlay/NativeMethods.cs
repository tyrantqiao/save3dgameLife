using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StaticAnchorOverlay;

public sealed record MonitorInfo(string Device, string Label, int Left, int Top, int Width, int Height);

public static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312, WM_DPICHANGED = 0x02E0, WM_DISPLAYCHANGE = 0x007E;
    public const int WM_NCHITTEST = 0x0084, WM_MOUSEACTIVATE = 0x0021;
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x80;
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorData
    {
        public int Size; public Rect Monitor; public Rect Work; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
    }
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorData data);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong32(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLong64(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLong64(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string windowName);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(uint processId);

    public static void ActivateWindow(IntPtr hwnd)
    {
        ShowWindow(hwnd, 9); // SW_RESTORE
        SetForegroundWindow(hwnd);
    }
    public static bool ActivateExistingSettings()
    {
        var hwnd = FindWindow(null, "StaticAnchorOverlay 设置");
        if (hwnd == IntPtr.Zero) return false;
        GetWindowThreadProcessId(hwnd, out uint processId);
        AllowSetForegroundWindow(processId);
        ActivateWindow(hwnd);
        return true;
    }

    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var result = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (handle, _, _, _) =>
        {
            var data = new MonitorData { Size = Marshal.SizeOf<MonitorData>(), Device = "" };
            if (GetMonitorInfo(handle, ref data))
            {
                var item = new MonitorInfo(data.Device, $"{data.Device} · {data.Monitor.Right - data.Monitor.Left} × {data.Monitor.Bottom - data.Monitor.Top}" + ((data.Flags & 1) != 0 ? " · 主屏" : ""), data.Monitor.Left, data.Monitor.Top, data.Monitor.Right - data.Monitor.Left, data.Monitor.Bottom - data.Monitor.Top);
                if ((data.Flags & 1) != 0) result.Insert(0, item); else result.Add(item);
            }
            return true;
        }, IntPtr.Zero);
        if (result.Count == 0) throw new InvalidOperationException("无法枚举显示器。");
        return result;
    }
    public static void MakeOverlay(IntPtr hwnd)
    {
        long style = IntPtr.Size == 8 ? GetWindowLong64(hwnd, GWL_EXSTYLE).ToInt64() : GetWindowLong32(hwnd, GWL_EXSTYLE);
        style |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        if (IntPtr.Size == 8) SetWindowLong64(hwnd, GWL_EXSTYLE, new IntPtr(style));
        else SetWindowLong32(hwnd, GWL_EXSTYLE, unchecked((int)style));
    }
    public static void Place(IntPtr hwnd, MonitorInfo monitor)
    {
        // Monitor rectangles are physical pixels; SetWindowPos avoids cross-monitor DIP conversion errors.
        if (!SetWindowPos(hwnd, new IntPtr(-1), monitor.Left, monitor.Top, monitor.Width, monitor.Height, 0x0010 | 0x0020))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
}
