using System;
using System.IO;
using Microsoft.Win32;

namespace StaticAnchorOverlay;

// Only this user's Run entry is changed, and only after an explicit checkbox click.
public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "StaticAnchorOverlay";

    public static string ExecutablePath
    {
        get
        {
            var path = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定程序路径。");
            if (!string.Equals(Path.GetFileName(path), "StaticAnchorOverlay.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("请双击 StaticAnchorOverlay.exe 启动后设置开机自启。");
            LocalFilePolicy.Check(path);
            return path;
        }
    }
    public static string Command => $"\"{ExecutablePath}\" --autostart";
    public static string? RegisteredCommand
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(ValueName) as string;
        }
    }
    public static bool IsEnabled => !string.IsNullOrEmpty(RegisteredCommand);
    public static void SetEnabled(bool enabled)
    {
        string? command = enabled ? Command : null;
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true)
            ?? throw new InvalidOperationException("无法打开当前用户的启动设置。");
        if (enabled) key.SetValue(ValueName, command!, RegistryValueKind.String);
        else key.DeleteValue(ValueName, false);
    }
}
