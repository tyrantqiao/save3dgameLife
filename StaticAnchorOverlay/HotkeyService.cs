using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace StaticAnchorOverlay;

public sealed class HotkeyService : IDisposable
{
    private readonly HwndSource source;
    private readonly Dictionary<int, Action> actions = new();
    private HotkeySettings? current;
    public HotkeyService(Action toggle, Action settings, Action quit)
    {
        source = new HwndSource(new HwndSourceParameters("StaticAnchorOverlay.Hotkeys") { ParentWindow = new IntPtr(-3), Width = 0, Height = 0 });
        source.AddHook(Hook);
        actions[1] = toggle; actions[2] = settings; actions[3] = quit;
    }
    public static (uint Modifiers, uint Key) Parse(string text)
    {
        uint modifiers = 0;
        Key key = Key.None;
        foreach (var part in (text ?? "").Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            uint flag = part.ToUpperInvariant() switch { "ALT" => 1, "CTRL" => 2, "SHIFT" => 4, "WIN" => 8, _ => 0 };
            if (flag != 0)
            {
                if ((modifiers & flag) != 0) throw new ArgumentException("热键修饰键重复。");
                modifiers |= flag;
            }
            else
            {
                if (key != Key.None || !Enum.TryParse(part, true, out key) || !(key >= Key.A && key <= Key.Z || key >= Key.F1 && key <= Key.F24 || key >= Key.D0 && key <= Key.D9))
                    throw new ArgumentException("热键格式：Alt+Shift+A；主键支持 A–Z、D0–D9、F1–F24。");
            }
        }
        if (modifiers == 0 || key == Key.None) throw new ArgumentException("热键至少包含一个修饰键和一个主键。");
        return (modifiers | 0x4000, (uint)KeyInterop.VirtualKeyFromKey(key));
    }
    public void Apply(HotkeySettings value)
    {
        var parsed = new[] { Parse(value.Toggle), Parse(value.Settings), Parse(value.Quit) };
        if (parsed.Distinct().Count() != 3) throw new ArgumentException("三个热键不能重复。");
        Clear();
        try { Register(parsed); }
        catch
        {
            Clear();
            if (current is not null) Register(new[] { Parse(current.Toggle), Parse(current.Settings), Parse(current.Quit) });
            throw;
        }
        current = new() { Toggle = value.Toggle, Settings = value.Settings, Quit = value.Quit };
    }
    private void Register((uint Modifiers, uint Key)[] values)
    {
        for (int i = 0; i < values.Length; i++)
            if (!NativeMethods.RegisterHotKey(source.Handle, i + 1, values[i].Modifiers, values[i].Key))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法注册热键，可能已被其他程序占用。");
    }
    private void Clear() { foreach (int id in actions.Keys) NativeMethods.UnregisterHotKey(source.Handle, id); }
    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && actions.TryGetValue(wParam.ToInt32(), out var action)) { handled = true; action(); }
        return IntPtr.Zero;
    }
    public void Dispose() { Clear(); source.RemoveHook(Hook); source.Dispose(); }
}
