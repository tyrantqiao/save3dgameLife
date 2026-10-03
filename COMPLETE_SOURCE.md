# StaticAnchorOverlay 完整项目交付
## 1. 项目结构和文件清单
```text
.gitignore
Package.ps1
GenerateIcon.ps1
StaticAnchorOverlay/AnchorSurface.cs
StaticAnchorOverlay/app.manifest
StaticAnchorOverlay/App.xaml
StaticAnchorOverlay/App.xaml.cs
StaticAnchorOverlay/config.example.json
StaticAnchorOverlay/ConfigStore.cs
StaticAnchorOverlay/HotkeyService.cs
StaticAnchorOverlay/LocalFilePolicy.cs
StaticAnchorOverlay/Models.cs
StaticAnchorOverlay/NativeMethods.cs
StaticAnchorOverlay/OverlayWindow.xaml
StaticAnchorOverlay/OverlayWindow.xaml.cs
StaticAnchorOverlay/README.md
StaticAnchorOverlay/SettingsWindow.xaml
StaticAnchorOverlay/SettingsWindow.xaml.cs
StaticAnchorOverlay/StartupService.cs
StaticAnchorOverlay/StaticAnchorOverlay.csproj
StaticAnchorOverlay/TrayService.cs
StaticAnchorOverlay/Assets/App.ico
StaticAnchorOverlay/Properties/PublishProfiles/WindowsPortable.pubxml
Verification/Program.cs
Verification/Verification.csproj
```
## 2. 每个文件的完整代码
### .gitignore
```text
**/bin/
**/obj/
.vs/
*.user
dist/
```
### Package.ps1
```powershell
param([switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if (-not $SkipPublish) {
    dotnet publish ./StaticAnchorOverlay/StaticAnchorOverlay.csproj -p:PublishProfile=WindowsPortable -o ./dist/win-x64 --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw '发布失败。' }
}
$taskFiles = @('.gitignore', 'Package.ps1', 'GenerateIcon.ps1') + @(Get-ChildItem ./StaticAnchorOverlay,./Verification -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    ForEach-Object { [System.IO.Path]::GetRelativePath($PSScriptRoot, $_.FullName).Replace('\','/') })
$taskDoc = [System.Text.StringBuilder]::new()
[void]$taskDoc.AppendLine('# StaticAnchorOverlay 完整项目交付')
[void]$taskDoc.AppendLine('## 1. 项目结构和文件清单')
[void]$taskDoc.AppendLine('```text')
foreach ($taskFile in $taskFiles) { [void]$taskDoc.AppendLine($taskFile) }
[void]$taskDoc.AppendLine('```')
[void]$taskDoc.AppendLine('## 2. 每个文件的完整代码')
foreach ($taskFile in $taskFiles | Where-Object { $_ -notlike '*README.md' -and $_ -notlike '*.json' -and $_ -notlike '*.ico' }) {
    [void]$taskDoc.AppendLine('### ' + $taskFile)
    $taskLanguage = switch ([System.IO.Path]::GetExtension($taskFile)) { '.cs' { 'csharp' } '.ps1' { 'powershell' } '.xaml' { 'xml' } '.csproj' { 'xml' } '.pubxml' { 'xml' } '.manifest' { 'xml' } default { 'text' } }
    [void]$taskDoc.AppendLine('```' + $taskLanguage)
    [void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $taskFile)).TrimEnd())
    [void]$taskDoc.AppendLine('```')
}
[void]$taskDoc.AppendLine('## 3. 构建命令和运行步骤 / 4. 使用说明')
[void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StaticAnchorOverlay/README.md')))
[void]$taskDoc.AppendLine('## 5. 默认配置示例')
[void]$taskDoc.AppendLine('```json')
[void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StaticAnchorOverlay/config.example.json')))
[void]$taskDoc.AppendLine('```')
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'COMPLETE_SOURCE.md'), $taskDoc.ToString(), [System.Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression
$taskZipStream = [System.IO.File]::Create((Join-Path $PSScriptRoot 'StaticAnchorOverlay-source.zip'))
$taskArchive = [System.IO.Compression.ZipArchive]::new($taskZipStream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in $taskFiles + @('COMPLETE_SOURCE.md')) {
        $taskEntry = $taskArchive.CreateEntry($taskFile)
        $taskOutput = $taskEntry.Open()
        $taskInput = [System.IO.File]::OpenRead((Join-Path $PSScriptRoot $taskFile))
        try { $taskInput.CopyTo($taskOutput) } finally { $taskInput.Dispose(); $taskOutput.Dispose() }
    }
} finally { $taskArchive.Dispose(); $taskZipStream.Dispose() }
Copy-Item -LiteralPath ./StaticAnchorOverlay/README.md -Destination ./dist/win-x64/README.md
Compress-Archive -Path ./dist/win-x64/* -DestinationPath ./dist/StaticAnchorOverlay-windows-x64.zip -Force
Get-Item ./dist/win-x64/StaticAnchorOverlay.exe,./dist/StaticAnchorOverlay-windows-x64.zip | Select-Object Name,Length
```
### GenerateIcon.ps1
```powershell
Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path $PSScriptRoot 'StaticAnchorOverlay/Assets'
[void][System.IO.Directory]::CreateDirectory($assetDir)
$frames = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
 $bmp = [System.Drawing.Bitmap]::new($size,$size)
 $g = [System.Drawing.Graphics]::FromImage($bmp)
 $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $g.Clear([System.Drawing.Color]::Transparent)
 $scale = $size / 64.0
 $g.ScaleTransform($scale,$scale)
 $bg = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#14243B'))
 $accent = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#51DAD2'),4)
 $white = [System.Drawing.Pen]::new([System.Drawing.Color]::White,4)
 $g.FillEllipse($bg,2,2,60,60)
 $g.DrawArc($accent,13,13,38,38,15,60); $g.DrawArc($accent,13,13,38,38,105,60); $g.DrawArc($accent,13,13,38,38,195,60); $g.DrawArc($accent,13,13,38,38,285,60)
 $g.DrawLine($white,32,19,32,26); $g.DrawLine($white,32,38,32,45); $g.DrawLine($white,19,32,26,32); $g.DrawLine($white,38,32,45,32)
 $g.FillEllipse([System.Drawing.Brushes]::White,29,29,6,6)
 $stream = [System.IO.MemoryStream]::new(); $bmp.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
 $frames += ,@{Size=$size;Data=$stream.ToArray()}
 $stream.Dispose(); $white.Dispose(); $accent.Dispose(); $bg.Dispose(); $g.Dispose(); $bmp.Dispose()
}
$out = [System.IO.File]::Create((Join-Path $assetDir 'App.ico')); $writer = [System.IO.BinaryWriter]::new($out)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
 $dimension = if ($frame.Size -eq 256) {0} else {$frame.Size}
 $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
 $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Data.Length); $writer.Write([uint32]$offset)
 $offset += $frame.Data.Length
}
foreach ($frame in $frames) {$writer.Write([byte[]]$frame.Data)}
$writer.Dispose()
```
### StaticAnchorOverlay/AnchorSurface.cs
```csharp
using System;
using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StaticAnchorOverlay;

// WPF retains this drawing. No timer, render-loop handler, input polling, or animation.
public sealed class AnchorSurface : FrameworkElement
{
    private Preset preset = new();
    private BitmapSource? png;
    private string loadedPath = "";
    public string? ImageError { get; private set; }
    public void Update(Preset value)
    {
        preset = value;
        if (value.Png.ImagePath != loadedPath)
        {
            loadedPath = value.Png.ImagePath;
            png = null; ImageError = null;
            if (loadedPath.Length > 0)
            {
                try
                {
                    LocalFilePolicy.Check(loadedPath);
                    using var stream = File.OpenRead(loadedPath);
                    if (stream.Length > 16 * 1024 * 1024) throw new InvalidOperationException("PNG 不得超过 16 MB。");
                    Span<byte> header = stackalloc byte[24]; stream.ReadExactly(header);
                    if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                        BinaryPrimitives.ReadUInt32BigEndian(header[16..20]) is 0 or > 4096 ||
                        BinaryPrimitives.ReadUInt32BigEndian(header[20..24]) is 0 or > 4096)
                        throw new InvalidOperationException("PNG 格式无效或单边超过 4096 像素。");
                    stream.Position = 0;
                    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];
                    if (frame.PixelWidth > 4096 || frame.PixelHeight > 4096) throw new InvalidOperationException("PNG 单边不得超过 4096 像素。");
                    frame.Freeze(); png = frame;
                }
                catch (Exception ex) { ImageError = "PNG 无法加载：" + ex.Message; }
            }
        }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));
        void Draw(Anchor a, Action<Brush, Pen, double, double> action)
        {
            if (!a.Enabled || a.Opacity <= 0) return;
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.Color)); brush.Freeze();
            var pen = new Pen(brush, a.Thickness); pen.Freeze();
            dc.PushOpacity(a.Opacity);
            action(brush, pen, w / 2 + a.X, h / 2 + a.Y);
            dc.Pop();
        }
        Draw(preset.Vignette, (b, _, _, _) =>
        {
            var a = preset.Vignette;
            var color = ((SolidColorBrush)b).Color;
            double depth = Math.Min(a.Depth, Math.Min(w, h) / 2);
            if (depth <= 0) return;
            void Edge(Rect rect, Point start, Point end)
            {
                var gradient = new LinearGradientBrush(color, Colors.Transparent, start, end); gradient.Freeze();
                dc.DrawRectangle(gradient, null, rect);
            }
            Edge(new Rect(0, 0, w, depth), new Point(0, 0), new Point(0, 1));
            Edge(new Rect(0, h - depth, w, depth), new Point(0, 1), new Point(0, 0));
            Edge(new Rect(0, 0, depth, h), new Point(0, 0), new Point(1, 0));
            Edge(new Rect(w - depth, 0, depth, h), new Point(1, 0), new Point(0, 0));
        });
        Draw(preset.Grid, (_, pen, x, y) =>
        {
            double step = preset.Grid.Spacing;
            for (double xx = ((x % step) + step) % step; xx < w; xx += step) dc.DrawLine(pen, new Point(xx, 0), new Point(xx, h));
            for (double yy = ((y % step) + step) % step; yy < h; yy += step) dc.DrawLine(pen, new Point(0, yy), new Point(w, yy));
        });
        Draw(preset.Border, (_, pen, _, _) =>
        {
            var a = preset.Border;
            double ww = w - 2 * a.Inset, hh = h - 2 * a.Inset;
            if (ww > 0 && hh > 0) dc.DrawRoundedRectangle(null, pen, new Rect(a.Inset + a.X, a.Inset + a.Y, ww, hh), a.CornerRadius, a.CornerRadius);
        });
        Draw(preset.Corners, (_, pen, _, _) =>
        {
            var a = preset.Corners;
            foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 })
            {
                double x = (sx < 0 ? a.Inset : w - a.Inset) + a.X, y = (sy < 0 ? a.Inset : h - a.Inset) + a.Y;
                dc.DrawLine(pen, new Point(x, y), new Point(x - sx * a.Length, y));
                dc.DrawLine(pen, new Point(x, y), new Point(x, y - sy * a.Length));
            }
        });
        Draw(preset.Crosshair, (brush, pen, x, y) =>
        {
            var a = preset.Crosshair;
            foreach (int s in new[] { -1, 1 })
            {
                dc.DrawLine(pen, new Point(x + s * a.Gap, y), new Point(x + s * (a.Gap + a.Length), y));
                dc.DrawLine(pen, new Point(x, y + s * a.Gap), new Point(x, y + s * (a.Gap + a.Length)));
            }
            if (a.DotSize > 0) dc.DrawEllipse(brush, null, new Point(x, y), a.DotSize / 2, a.DotSize / 2);
        });
        Draw(preset.CenterDot, (brush, _, x, y) => dc.DrawEllipse(brush, null, new Point(x, y), preset.CenterDot.Size / 2, preset.CenterDot.Size / 2));
        Draw(preset.Horizontal, (_, pen, x, y) => dc.DrawLine(pen, new Point(x - preset.Horizontal.Length / 2, y), new Point(x + preset.Horizontal.Length / 2, y)));
        Draw(preset.Vertical, (_, pen, x, y) => dc.DrawLine(pen, new Point(x, y - preset.Vertical.Length / 2), new Point(x, y + preset.Vertical.Length / 2)));
        if (png is not null) Draw(preset.Png, (_, _, x, y) =>
        {
            var a = preset.Png;
            if (a.Width > 0 && a.Height > 0) dc.DrawImage(png, new Rect(x - a.Width / 2, y - a.Height / 2, a.Width, a.Height));
        });
        dc.Pop();
    }
}
```
### StaticAnchorOverlay/app.manifest
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="StaticAnchorOverlay" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3"><security><requestedPrivileges><requestedExecutionLevel level="asInvoker" uiAccess="false" /></requestedPrivileges></security></trustInfo>
  <application xmlns="urn:schemas-microsoft-com:asm.v3"><windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings></application>
</assembly>
```
### StaticAnchorOverlay/App.xaml
```xml
<Application x:Class="StaticAnchorOverlay.App" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" ShutdownMode="OnExplicitShutdown">
  <Application.Resources />
</Application>
```
### StaticAnchorOverlay/App.xaml.cs
```csharp
using System;
using System.Linq;
using System.Windows.Interop;
using System.Threading;
using System.Windows;

namespace StaticAnchorOverlay;

public partial class App : Application
{
    public Configuration Config { get; private set; } = new();
    public ConfigStore Store { get; } = new();
    private OverlayWindow overlay = null!;
    private SettingsWindow? settings;
    private TrayService? tray;
    private HotkeyService? hotkeys;
    private Mutex? instance;
    private string? startupWarning;
    public string? Warning { get; private set; }
    public bool IsExiting { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance = new Mutex(true, @"Local\StaticAnchorOverlay", out bool created);
        if (!created)
        {
            if (!NativeMethods.ActivateExistingSettings()) MessageBox.Show("应用正在启动，请稍后使用任务栏或托盘打开设置。", "StaticAnchorOverlay");
            ExitApplication(); return;
        }
        try
        {
            Config = Store.Load(); startupWarning = Store.LoadWarning;
            if (string.IsNullOrEmpty(Config.MonitorDevice)) Config.MonitorDevice = NativeMethods.GetMonitors()[0].Device;
            overlay = new OverlayWindow();
            overlay.Update(Config); overlay.Show();
            overlay.DisplaysChanged += () => settings?.RefreshMonitors();
            tray = new TrayService(Toggle, OpenSettings, ExitApplication);
            hotkeys = new HotkeyService(Toggle, OpenSettings, ExitApplication);
            try { hotkeys.Apply(Config.Hotkeys); } catch (Exception ex) { startupWarning = (startupWarning ?? "") + "\n" + ex.Message; }
            Apply();
            ShowSettings(Warning is null && e.Args.Contains("--autostart", StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "启动失败"); ExitApplication(); }
    }
    public void Toggle() { if (overlay.IsVisible) overlay.Hide(); else overlay.Show(); }
    public void OpenSettings() => ShowSettings(false);
    private void ShowSettings(bool minimized)
    {
        if (settings is null)
        {
            settings = new SettingsWindow(this);
            MainWindow = settings;
            settings.Closed += (_, _) => settings = null;
        }
        settings.WindowState = minimized ? WindowState.Minimized : WindowState.Normal;
        settings.Show();
        if (!minimized)
        {
            settings.Activate();
            NativeMethods.ActivateWindow(new WindowInteropHelper(settings).Handle);
        }
    }
    public void ExitApplication() { IsExiting = true; Shutdown(); }
    public void Apply()
    {
        Config.Validate();
        overlay.Update(Config);
        tray?.Refresh(Config, id => { Config.ActivePresetId = id; Apply(); settings?.Reload(); });
        try { Store.Save(Config); Warning = string.Join("\n", new[] { startupWarning, overlay.ImageError }).Trim(); if (Warning.Length == 0) Warning = null; }
        catch (Exception ex) { Warning = "保存失败：" + ex.Message; }
    }
    public void SetHotkeys(HotkeySettings value) { hotkeys!.Apply(value); Config.Hotkeys = value; Apply(); }
    public void Import(string path)
    {
        var value = ConfigStore.Read(path);
        hotkeys!.Apply(value.Hotkeys);
        Config = value; Apply();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        hotkeys?.Dispose(); tray?.Dispose(); instance?.Dispose(); base.OnExit(e);
    }
}
```
### StaticAnchorOverlay/ConfigStore.cs
```csharp
using System;
using System.IO;
using System.Text.Json;

namespace StaticAnchorOverlay;

public sealed class ConfigStore
{
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string FilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StaticAnchorOverlay", "config.json");
    public string? LoadWarning { get; private set; }

    public Configuration Load()
    {
        LocalFilePolicy.Check(FilePath);
        if (File.Exists(FilePath))
        {
            try { return Read(FilePath); }
            catch (Exception ex)
            {
                // Preserve the original before autosaving defaults.
                var backup = FilePath + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff");
                File.Copy(FilePath, backup);
                LoadWarning = "配置无法读取，原文件已备份到：" + backup + "\n" + ex.Message;
            }
        }
        var config = new Configuration();
        config.Validate();
        return config;
    }

    public static Configuration Read(string path)
    {
        LocalFilePolicy.Check(path);
        if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidOperationException("配置文件不得超过 2 MB。");
        var config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("配置为空。");
        config.Validate();
        return config;
    }

    public void Save(Configuration config) => Write(FilePath, config);
    public static void Write(string path, Configuration config)
    {
        LocalFilePolicy.Check(path);
        config.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(temporary, path, true);
    }
}
```
### StaticAnchorOverlay/HotkeyService.cs
```csharp
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
```
### StaticAnchorOverlay/LocalFilePolicy.cs
```csharp
using System;
using System.IO;

namespace StaticAnchorOverlay;

public static class LocalFilePolicy
{
    public static void Check(string path)
    {
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\") || new Uri(path).IsUnc ||
            new DriveInfo(Path.GetPathRoot(path)!).DriveType == DriveType.Network)
            throw new InvalidOperationException("仅支持本地绝对路径，不支持 UNC 或映射网络盘。");
    }
}
```
### StaticAnchorOverlay/Models.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace StaticAnchorOverlay;

// Positions and lengths are device-independent pixels (DIP). X/Y are offsets from the screen centre.
public sealed class Anchor
{
    public bool Enabled { get; set; } = true;
    public string Color { get; set; } = "#FFFFFF";
    public double Opacity { get; set; } = 0.35;
    public double X { get; set; }
    public double Y { get; set; }
    public double Length { get; set; } = 16;
    public double Thickness { get; set; } = 2;
    public double Gap { get; set; } = 5;
    public double DotSize { get; set; } = 3;
    public double Size { get; set; } = 4;
    public double Inset { get; set; } = 24;
    public double CornerRadius { get; set; } = 8;
    public double Spacing { get; set; } = 120;
    public double Depth { get; set; } = 100;
    public double Width { get; set; } = 128;
    public double Height { get; set; } = 128;
    public string ImagePath { get; set; } = "";
}

public sealed class Preset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "默认";
    public Anchor Crosshair { get; set; } = new();
    public Anchor Border { get; set; } = new() { Opacity = 0.18 };
    public Anchor CenterDot { get; set; } = new() { Enabled = false };
    public Anchor Horizontal { get; set; } = new() { Enabled = false, Length = 200 };
    public Anchor Vertical { get; set; } = new() { Enabled = false, Length = 200 };
    public Anchor Corners { get; set; } = new() { Length = 24, Opacity = 0.25 };
    public Anchor Grid { get; set; } = new() { Enabled = false, Opacity = 0.08, Thickness = 1 };
    public Anchor Vignette { get; set; } = new() { Enabled = false, Color = "#000000", Opacity = 0.25 };
    public Anchor Png { get; set; } = new() { Enabled = false, Opacity = 0.35 };
}

public sealed class HotkeySettings
{
    public string Toggle { get; set; } = "Alt+Shift+A";
    public string Settings { get; set; } = "Alt+Shift+S";
    public string Quit { get; set; } = "Alt+Shift+Q";
}

public sealed class Configuration
{
    public int Version { get; set; } = 1;
    public string MonitorDevice { get; set; } = "";
    public string ActivePresetId { get; set; } = "";
    public HotkeySettings Hotkeys { get; set; } = new();
    public List<Preset> Presets { get; set; } = new() { new Preset() };
    [System.Text.Json.Serialization.JsonIgnore]
    public Preset Active => Presets.First(p => p.Id == ActivePresetId);

    public void Validate()
    {
        if (Version != 1) throw new InvalidOperationException("不支持的配置版本。");
        if (Hotkeys is null || Presets is null || Presets.Count == 0 || Presets.Count > 100)
            throw new InvalidOperationException("配置必须含有 1–100 个预设和热键。");
        var ids = new HashSet<string>();
        foreach (var p in Presets)
        {
            if (p is null || string.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id) || string.IsNullOrWhiteSpace(p.Name))
                throw new InvalidOperationException("预设名称或 ID 无效。");
            foreach (var prop in typeof(Preset).GetProperties().Where(x => x.PropertyType == typeof(Anchor)))
            {
                if (prop.GetValue(p) is not Anchor a) throw new InvalidOperationException("锚点配置缺失。");
                if (!IsColor(a.Color)) throw new InvalidOperationException("颜色须为 #RRGGBB 或 #AARRGGBB。");
                foreach (var n in typeof(Anchor).GetProperties().Where(x => x.PropertyType == typeof(double)))
                {
                    var value = (double)n.GetValue(a)!;
                    if (!double.IsFinite(value) || Math.Abs(value) > 20000 || (n.Name != "X" && n.Name != "Y" && value < 0))
                        throw new InvalidOperationException("尺寸必须有限且在 0–20000 DIP 内（偏移允许负数）。");
                }
                if (a.Opacity > 1 || a.Spacing < 8 || a.Thickness > 100)
                    throw new InvalidOperationException("透明度范围 0–1，网格间距至少 8，线宽最多 100。");
                if (a.ImagePath is null) throw new InvalidOperationException("图片路径不能为 null。");
                if (a.ImagePath.Length > 0) LocalFilePolicy.Check(a.ImagePath);
            }
        }
        if (!Presets.Any(p => p.Id == ActivePresetId)) ActivePresetId = Presets[0].Id;
    }

    public static bool IsColor(string? value) => value is not null &&
        (value.Length == 7 || value.Length == 9) && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}

public static class PresetManager
{
    public static Preset Add(Configuration config, string name, bool copy)
    {
        if (config.Presets.Count >= 100) throw new InvalidOperationException("最多 100 个预设。");
        var preset = copy ? JsonSerializer.Deserialize<Preset>(JsonSerializer.Serialize(config.Active))! : new Preset();
        preset.Id = Guid.NewGuid().ToString("N");
        preset.Name = name;
        config.Presets.Add(preset);
        config.ActivePresetId = preset.Id;
        return preset;
    }
    public static void Delete(Configuration config)
    {
        if (config.Presets.Count == 1) throw new InvalidOperationException("至少保留一个预设。");
        config.Presets.Remove(config.Active);
        config.ActivePresetId = config.Presets[0].Id;
    }
}
```
### StaticAnchorOverlay/NativeMethods.cs
```csharp
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
```
### StaticAnchorOverlay/OverlayWindow.xaml
```xml
<Window x:Class="StaticAnchorOverlay.OverlayWindow" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Title="StaticAnchorOverlay" WindowStyle="None" ResizeMode="NoResize" AllowsTransparency="True" Background="Transparent" Topmost="True" ShowInTaskbar="False" ShowActivated="False" Focusable="False" IsHitTestVisible="False">
  <Grid x:Name="SurfaceHost" IsHitTestVisible="False" />
</Window>
```
### StaticAnchorOverlay/OverlayWindow.xaml.cs
```csharp
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
```
### StaticAnchorOverlay/SettingsWindow.xaml
```xml
<Window x:Class="StaticAnchorOverlay.SettingsWindow" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Title="StaticAnchorOverlay 设置" Icon="Assets/App.ico" Width="660" Height="740" MinWidth="560" MinHeight="480" Background="#F5F7FA" WindowStartupLocation="CenterScreen" ShowInTaskbar="True" ShowActivated="True" Closing="WindowClosing">
  <Window.Resources>
    <Style TargetType="Button"><Setter Property="Padding" Value="12,6"/><Setter Property="Margin" Value="0,0,8,0"/></Style>
    <Style TargetType="ComboBox"><Setter Property="Padding" Value="6"/></Style>
    <Style TargetType="Expander"><Setter Property="Margin" Value="0,0,0,10"/><Setter Property="Background" Value="White"/><Setter Property="Padding" Value="10"/></Style>
  </Window.Resources>
  <DockPanel Margin="20">
    <StackPanel DockPanel.Dock="Top">
      <TextBlock Text="静态视觉锚点" FontSize="24" FontWeight="SemiBold"/>
      <TextBlock Text="修改即生效 · 自动保存" Foreground="#64748B" Margin="0,4,0,16"/>
      <Grid Margin="0,0,0,12">
        <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
        <StackPanel><TextBlock Text="当前预设" Margin="0,0,0,5"/><ComboBox x:Name="PresetBox" DisplayMemberPath="Name" SelectionChanged="PresetChanged"/></StackPanel>
        <Button Grid.Column="1" Content="预设管理 ▾" Click="OpenPresetMenu" VerticalAlignment="Bottom" Margin="8,0,0,0">
          <Button.ContextMenu><ContextMenu>
            <MenuItem Header="新建预设" Click="NewPreset"/><MenuItem Header="复制当前预设" Click="CopyPreset"/><MenuItem Header="重命名" Click="RenamePreset"/><MenuItem Header="删除当前预设" Click="DeletePreset"/><Separator/><MenuItem Header="导入配置…" Click="ImportConfig"/><MenuItem Header="导出配置…" Click="ExportConfig"/>
          </ContextMenu></Button.ContextMenu>
        </Button>
      </Grid>
      <Button Content="显示 / 隐藏叠加" Click="ToggleOverlay" HorizontalAlignment="Left" Margin="0,0,0,12"/>
    </StackPanel>
    <Border DockPanel.Dock="Bottom" Padding="0,10,0,0"><TextBlock x:Name="StatusText" TextWrapping="Wrap" Foreground="#64748B"/></Border>
    <TabControl Background="Transparent" BorderThickness="0">
      <TabItem Header="锚点"><ScrollViewer VerticalScrollBarVisibility="Auto" Margin="0,12,0,0"><StackPanel x:Name="EditorPanel"/></ScrollViewer></TabItem>
      <TabItem Header="应用"><ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel Margin="0,16,0,0">
        <TextBlock Text="显示屏" FontWeight="SemiBold" Margin="0,0,0,8"/>
        <ComboBox x:Name="MonitorBox" DisplayMemberPath="Label" SelectionChanged="MonitorChanged" Margin="0,0,0,18"/>
        <CheckBox x:Name="StartupCheckBox" Content="开机自动启动" Click="StartupChanged" Margin="0,0,0,8"/>
        <TextBlock x:Name="StartupStatusText" TextWrapping="Wrap" Foreground="#64748B" Margin="0,0,0,16"/>
        <Expander Header="快捷键"><StackPanel x:Name="HotkeyPanel"/></Expander>
        <Expander Header="使用说明与程序位置"><StackPanel Margin="8">
          <TextBlock Text="游戏请使用无边框或窗口化全屏。关闭设置窗口后，叠加继续运行，可从任务栏或托盘重新打开。" TextWrapping="Wrap" Margin="0,0,0,12"/>
          <TextBlock Text="将程序放在固定文件夹后再开启自启；移动后需重新开启。" TextWrapping="Wrap" Margin="0,0,0,12"/>
          <TextBox x:Name="ExecutablePathBox" IsReadOnly="True" TextWrapping="Wrap"/>
        </StackPanel></Expander>
        <Button Content="退出应用" Click="ExitApplication" HorizontalAlignment="Left" Margin="0,12,0,0"/>
      </StackPanel></ScrollViewer></TabItem>
    </TabControl>
  </DockPanel>
</Window>
```
### StaticAnchorOverlay/SettingsWindow.xaml.cs
```csharp
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace StaticAnchorOverlay;

public partial class SettingsWindow : Window
{
    private readonly App app;
    private bool refreshing;
    public SettingsWindow(App app) { this.app = app; InitializeComponent(); Reload(); }
    public void RefreshMonitors()
    {
        refreshing = true;
        var monitors = NativeMethods.GetMonitors(); MonitorBox.ItemsSource = monitors;
        MonitorBox.SelectedItem = monitors.FirstOrDefault(m => m.Device == app.Config.MonitorDevice) ?? monitors[0];
        refreshing = false;
    }
    public void Reload()
    {
        RefreshMonitors(); refreshing = true;
        PresetBox.ItemsSource = null; PresetBox.ItemsSource = app.Config.Presets; PresetBox.SelectedItem = app.Config.Active;
        EditorPanel.Children.Clear(); HotkeyPanel.Children.Clear();
        var p = app.Config.Active;
        Group("中心十字准星", p.Crosshair, "Length:线长", "Thickness:线宽", "Gap:间隙", "DotSize:中心圆点直径");
        Group("屏幕边框", p.Border, "Inset:内缩距离", "Thickness:厚度", "CornerRadius:圆角半径");
        Group("中心点", p.CenterDot, "Size:直径");
        Group("水平参考线", p.Horizontal, "Length:长度", "Thickness:粗细");
        Group("垂直参考线", p.Vertical, "Length:长度", "Thickness:粗细");
        Group("四角标记", p.Corners, "Length:线段长度", "Thickness:粗细", "Inset:距离角落");
        Group("低透明度网格", p.Grid, "Spacing:间距（至少 8）", "Thickness:线宽");
        Group("暗角 / 边缘渐变", p.Vignette, "Depth:渐变深度");
        Group("自定义 PNG（颜色不影响图片）", p.Png, "Width:宽度", "Height:高度", "ImagePath:本地 PNG 路径");
        AddHotkeyEditor();
        RefreshStartup();
        StatusText.Text = app.Warning ?? "展开锚点可调整外观；更多细项见高级参数。";
        refreshing = false;
    }
    private void Group(string title, Anchor anchor, params string[] fields)
    {
        var panel = new StackPanel { Margin = new Thickness(10) };
        var enabled = new CheckBox { Content = title, IsChecked = anchor.Enabled, Margin = new Thickness(0, 0, 0, 8) };
        enabled.Click += (_, _) => { anchor.Enabled = enabled.IsChecked == true; Commit(); };
        var advanced = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (string field in new[] { "Color:颜色", "Opacity:透明度" }.Concat(fields).Concat(new[] { "X:水平偏移", "Y:垂直偏移" }))
        {
            var parts = field.Split(':'); var property = typeof(Anchor).GetProperty(parts[0])!;
            bool isAdvanced = parts[0] is "X" or "Y" or "CornerRadius" or "DotSize" or "ImagePath";
            var box = Row(isAdvanced ? advanced : panel, parts[1], Convert.ToString(property.GetValue(anchor), CultureInfo.InvariantCulture) ?? "");
            box.TextChanged += (_, _) =>
            {
                if (refreshing) return;
                object? previous = property.GetValue(anchor);
                try
                {
                    object value = property.PropertyType == typeof(double) ? double.Parse(box.Text, CultureInfo.InvariantCulture) : box.Text;
                    property.SetValue(anchor, value); app.Config.Validate();
                    box.ClearValue(Border.BorderBrushProperty); Commit();
                }
                catch (Exception ex)
                {
                    property.SetValue(anchor, previous); box.BorderBrush = Brushes.Red;
                    StatusText.Text = "输入尚未生效：" + ex.Message;
                }
            };
        }
        if (anchor == app.Config.Active.Png)
        {
            var choose = new Button { Content = "选择 PNG…", HorizontalAlignment = HorizontalAlignment.Left };
            choose.Click += ChoosePng; panel.Children.Insert(0, choose);
        }
        panel.Children.Add(new Expander { Header = "高级参数", Content = advanced });
        EditorPanel.Children.Add(new Expander { Header = enabled, Content = panel, IsExpanded = false });
    }
    private static TextBox Row(StackPanel panel, string label, string value)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) }); row.ColumnDefinitions.Add(new ColumnDefinition());
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        var box = new TextBox { Text = value, Padding = new Thickness(4) }; Grid.SetColumn(box, 1); row.Children.Add(box); panel.Children.Add(row); return box;
    }
    private void AddHotkeyEditor()
    {
        var panel = new StackPanel { Margin = new Thickness(10) };
        var toggle = Row(panel, "显示/隐藏", app.Config.Hotkeys.Toggle);
        var settings = Row(panel, "打开设置", app.Config.Hotkeys.Settings);
        var quit = Row(panel, "退出", app.Config.Hotkeys.Quit);
        var button = new Button { Content = "应用热键", Padding = new Thickness(8) };
        button.Click += (_, _) => Run(() => { app.SetHotkeys(new() { Toggle = toggle.Text, Settings = settings.Text, Quit = quit.Text }); StatusText.Text = app.Warning ?? "热键已注册并保存。"; });
        panel.Children.Add(button);
        panel.Children.Add(new TextBlock { Text = "修饰键：Alt / Ctrl / Shift / Win；主键：A–Z、D0–D9、F1–F24。热键修改需点应用。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        HotkeyPanel.Children.Add(panel);
    }
    private void Commit() { app.Apply(); StatusText.Text = app.Warning ?? "已实时应用并自动保存。"; }
    private void Run(Action action) { try { action(); } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private void MonitorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshing && MonitorBox.SelectedItem is MonitorInfo m) { app.Config.MonitorDevice = m.Device; Run(Commit); }
    }
    private void PresetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshing && PresetBox.SelectedItem is Preset p) { app.Config.ActivePresetId = p.Id; Run(() => { Commit(); Reload(); }); }
    }
    private string? AskName(string initial)
    {
        var dialog = new Window { Title = "预设名称", Owner = this, Width = 360, Height = 160, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var panel = new StackPanel { Margin = new Thickness(16) }; var input = new TextBox { Text = initial, Margin = new Thickness(0, 0, 0, 12) };
        var ok = new Button { Content = "确定", IsDefault = true, Padding = new Thickness(8) };
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(input.Text) && input.Text.Trim().Length <= 80) dialog.DialogResult = true; };
        panel.Children.Add(input); panel.Children.Add(ok); dialog.Content = panel;
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }
    private void NewPreset(object sender, RoutedEventArgs e) => Create(false);
    private void CopyPreset(object sender, RoutedEventArgs e) => Create(true);
    private void Create(bool copy)
    {
        string? name = AskName(copy ? app.Config.Active.Name + " 副本" : "新预设");
        if (name is not null) Run(() => { PresetManager.Add(app.Config, name, copy); Commit(); Reload(); });
    }
    private void RenamePreset(object sender, RoutedEventArgs e)
    {
        string? name = AskName(app.Config.Active.Name);
        if (name is not null) Run(() => { app.Config.Active.Name = name; Commit(); Reload(); });
    }
    private void DeletePreset(object sender, RoutedEventArgs e) => Run(() => { PresetManager.Delete(app.Config); Commit(); Reload(); });
    private void ImportConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON 配置|*.json" };
        if (dialog.ShowDialog(this) == true) Run(() => { app.Import(dialog.FileName); Reload(); });
    }
    private void ExportConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "JSON 配置|*.json", FileName = "StaticAnchorOverlay-config.json" };
        if (dialog.ShowDialog(this) == true) Run(() => { ConfigStore.Write(dialog.FileName, app.Config); StatusText.Text = "配置已导出。"; });
    }
    private void ChoosePng(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PNG 图片|*.png" };
        if (dialog.ShowDialog(this) == true) Run(() => { app.Config.Active.Png.ImagePath = dialog.FileName; app.Config.Validate(); Commit(); Reload(); });
    }
    private void ToggleOverlay(object sender, RoutedEventArgs e) => app.Toggle();
    private void OpenPresetMenu(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender; button.ContextMenu.PlacementTarget = button; button.ContextMenu.IsOpen = true;
    }
    private void RefreshStartup()
    {
        try
        {
            ExecutablePathBox.Text = StartupService.ExecutablePath;
            StartupCheckBox.IsChecked = StartupService.IsEnabled;
            StartupStatusText.Text = StartupService.IsEnabled
                ? "已开启，登录 Windows 后自动显示叠加。"
                : "尚未开启开机自动启动。";
        }
        catch (Exception ex) { StartupCheckBox.IsEnabled = false; StartupStatusText.Text = ex.Message; }
    }
    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupService.SetEnabled(StartupCheckBox.IsChecked == true);
            RefreshStartup();
        }
        catch (Exception ex)
        {
            RefreshStartup(); StartupStatusText.Text = "自启设置失败：" + ex.Message;
        }
    }
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (app.IsExiting) return;
        e.Cancel = true;
        WindowState = WindowState.Minimized;
    }
    private void ExitApplication(object sender, RoutedEventArgs e) => app.ExitApplication();
}
```
### StaticAnchorOverlay/StartupService.cs
```csharp
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
```
### StaticAnchorOverlay/StaticAnchorOverlay.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon>Assets/App.ico</ApplicationIcon>
    <!-- WPF owns DPI initialization; WinForms is used only for NotifyIcon. -->
    <NoWarn>$(NoWarn);WFAC010</NoWarn>
  </PropertyGroup>
  <ItemGroup><Resource Include="Assets/App.ico" /></ItemGroup>
</Project>
```
### StaticAnchorOverlay/TrayService.cs
```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StaticAnchorOverlay;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly Icon appIcon;
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem presets = new("切换预设");
    public TrayService(Action toggle, Action settings, Action quit)
    {
        menu.Items.Add("显示/隐藏叠加", null, (_, _) => toggle());
        menu.Items.Add("打开设置", null, (_, _) => settings());
        menu.Items.Add(presets);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => quit());
        using (var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/App.ico"))!.Stream)
            appIcon = new Icon(stream, 32, 32);
        icon = new NotifyIcon { Icon = appIcon, Text = "StaticAnchorOverlay", ContextMenuStrip = menu, Visible = true };
        icon.DoubleClick += (_, _) => settings();
    }
    public void Refresh(Configuration config, Action<string> select)
    {
        foreach (ToolStripItem item in presets.DropDownItems.Cast<ToolStripItem>().ToArray()) item.Dispose();
        presets.DropDownItems.Clear();
        foreach (var p in config.Presets)
        {
            string id = p.Id;
            var item = new ToolStripMenuItem(p.Name) { Checked = id == config.ActivePresetId };
            item.Click += (_, _) => select(id);
            presets.DropDownItems.Add(item);
        }
    }
    public void Dispose() { icon.Visible = false; icon.Dispose(); appIcon.Dispose(); menu.Dispose(); }
}
```
### StaticAnchorOverlay/Properties/PublishProfiles/WindowsPortable.pubxml
```xml
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <PublishTrimmed>false</PublishTrimmed>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <PublishDir>../../dist/win-x64/</PublishDir>
  </PropertyGroup>
</Project>
```
### Verification/Program.cs
```csharp
using StaticAnchorOverlay;

var sample = Path.GetFullPath(args.Length > 0 ? args[0] : "StaticAnchorOverlay/config.example.json");
var config = new Configuration(); config.Validate();
config.Presets[0].Id = "default"; config.ActivePresetId = "default";
ConfigStore.Write(sample, config);
var copy = PresetManager.Add(config, "副本", true);
copy.Crosshair.Color = "#00FF00";
if (config.Presets[0].Crosshair.Color != "#FFFFFF") throw new Exception("复制未隔离。");
var testPath = sample + ".test.json";
try
{
    ConfigStore.Write(testPath, config);
    var restored = ConfigStore.Read(testPath);
    if (restored.Active.Name != "副本" || restored.Active.Crosshair.Color != "#00FF00") throw new Exception("配置往返失败。");
    PresetManager.Delete(restored);
    try { PresetManager.Delete(restored); throw new Exception("不应允许删除最后一套预设。"); }
    catch (InvalidOperationException) { }
    restored.Active.Crosshair.Opacity = 2;
    try { restored.Validate(); throw new Exception("未拒绝非法透明度。"); }
    catch (InvalidOperationException) { }
    try { LocalFilePolicy.Check(@"\\server\share\image.png"); throw new Exception("未拒绝网络路径。"); }
    catch (InvalidOperationException) { }
    Console.WriteLine("PASS: preset isolation, JSON round-trip, last-preset protection, value validation, UNC rejection.");
}
finally { if (File.Exists(testPath)) File.Delete(testPath); }
```
### Verification/Verification.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup>
    <Compile Include="../StaticAnchorOverlay/Models.cs" Link="Models.cs" />
    <Compile Include="../StaticAnchorOverlay/ConfigStore.cs" Link="ConfigStore.cs" />
    <Compile Include="../StaticAnchorOverlay/LocalFilePolicy.cs" Link="LocalFilePolicy.cs" />
  </ItemGroup>
</Project>
```
## 3. 构建命令和运行步骤 / 4. 使用说明
# StaticAnchorOverlay

Windows 10/11，C# .NET 8 + WPF 外部静态视觉锚点工具。源码无第三方 NuGet 依赖。

## 直接使用 EXE 与开机自启

双击 `dist\win-x64\StaticAnchorOverlay.exe` 即可启动，适用于 Windows 10/11 x64，无需另外安装 .NET。
这是包含 .NET 运行时的单文件便携版本，约 154 MiB；首次启动会在系统临时目录解压所需的原生库。
手动启动时自动打开设置主窗口并显示在任务栏。关闭设置窗口会最小化，继续保留任务栏入口和托盘图标；使用“退出应用”、托盘退出或 Alt+Shift+Q 可完全退出。

在设置窗口选择“应用与启动”，勾选“开机自动启动”。应用会在当前用户登录 Windows 后显示叠加并驻留托盘，设置主窗口最小化到任务栏（发生错误时会展开）。再次启动 EXE 会恢复已有设置窗口，避免重复运行。
取消勾选即关闭自启。此设置读取 Windows 的实际注册项，不随 JSON 预设导入/导出改变。
无需管理员权限，仅写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下名为 `StaticAnchorOverlay` 的值。
命令包含加引号的 EXE 完整路径与 `--autostart` 参数。
请将 EXE 放在固定的本地文件夹再启用自启；移动文件后需从新位置取消并重新勾选。
Windows 任务管理器“启动应用”中的禁用状态也会影响实际自启。
删除软件前取消自启以移除注册项。

重新打包单文件 EXE：

```powershell
dotnet publish .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -p:PublishProfile=WindowsPortable -o .\dist\win-x64 --ignore-failed-sources
```

本工具不注入、不读写游戏内存、不 Hook、不截屏、不联网、不修改游戏文件。
使用的 HwndSource.AddHook 仅是本工具自己窗口的 Win32 消息处理回调，不是系统/游戏 Hook；没有使用 SetWindowsHookEx。
视觉锚点可能帮助部分用户缓解晕 3D，效果因人而异，不保证治疗效果。
外部叠加通常较安全，但某些反作弊可能误判，建议先用小号或测试环境验证，并遵守游戏规则。

## 构建与运行

安装 .NET 8 SDK。在仓库根目录 PowerShell 执行：

```powershell
dotnet build .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -c Release --ignore-failed-sources
dotnet run --project .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -c Release --no-build
```

直接运行：

```powershell
.\StaticAnchorOverlay\bin\Release\net8.0-windows\StaticAnchorOverlay.exe
```

可选发布（需要对应运行时包，构建环境可能需下载；工具运行时没有网络功能）：

```powershell
dotnet publish .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -c Release -r win-x64 --self-contained false -o .\publish
```

目标机器安装 .NET 8 Desktop Runtime。ARM64 机器可使用相应运行时或改为 win-arm64 发布。

## 使用

启动后叠加默认显示，通知区域出现图标（可能位于隐藏图标菜单）。右键打开设置，双击也可打开设置。
Alt+Shift+A 显示/隐藏；Alt+Shift+S 打开设置；Alt+Shift+Q 退出。关闭设置窗口会最小化到任务栏。
托盘菜单还可切换预设或退出。

设置窗口顶部选择目标显示器与预设。展开元素分组，启用/关闭、输入参数；合法参数立即应用并保存。
非法或尚未输入完整的值标红，保留上次合法值。小数使用英文句点。
热键支持 Alt/Ctrl/Shift/Win 加 A–Z、D0–D9、F1–F24；修改后点击“应用热键”。
如果热键被占用，会显示错误，并尝试恢复上一组热键；启动时注册失败仍可使用托盘。
新建生成默认参数；复制复制当前预设；至少保留一套预设。导入会替换整个配置，导出保存完整配置。

参数使用 DIP（96 DIP = 100% 缩放时的 96 像素）。X/Y 为相对中心偏移，边框/四角标记为整体平移；网格偏移改变网格相位；暗角固定在四边，X/Y 不改变边缘位置。
PNG 使用 Width/Height 控制显示尺寸，X/Y 控制中心位置，颜色不作用于 PNG。
颜色支持 #RRGGBB / #AARRGGBB，透明度 0–1，与颜色 alpha 相乘。
选择 PNG 后需启用 PNG 元素；只读取本地图片，文件不超过 16 MB，宽高各不超过 4096 像素。
载入后不锁定图片；相同路径的图片内容替换后，需要清空路径再重新选择以刷新缓存。

配置自动保存至 `%AppData%\StaticAnchorOverlay\config.json`。保存使用临时文件后替换，读取失败时备份为 `.invalid-时间戳` 后使用默认配置。
记录显示器设备名、当前预设、热键。显示器暂时缺失时回退主屏，保留原显示器选择；重新连接后恢复。
预设与配置导入不复制 PNG 文件，迁移机器后需重新选择本地 PNG。

## 显示模式与性能

请使用游戏无边框窗口或窗口化全屏。独占全屏可能绕过桌面合成，叠加无法显示时需切换游戏模式。
工具不检查游戏进程，因此无法自动检测独占全屏。其他始终置顶窗口也可能遮住叠加。
使用 WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE，叠加不进入任务栏、不抢焦点并点击穿透。
显示器边界使用物理像素定位，manifest 为 PerMonitorV2，响应显示器与 DPI 变化。

绘制通过 FrameworkElement.OnRender + DrawingContext 留存矢量指令，除了可选 PNG 不创建全屏位图。
无每帧定时器、无动画、无 CompositionTarget.Rendering；仅配置、布局、DPI/显示器变化触发重绘。
WPF/DWM 仍需合成透明窗口，游戏中的实际 CPU/GPU 成本取决于分辨率、GPU、驱动与开启的元素。
关闭网格/渐变有助于降低合成成本。接近 0 的 CPU 和无掉帧应按下列步骤实测，不能只由编译证明。

## 验收

1. 启动：确认托盘与默认十字、边框、角标出现。
2. 热键：A 切换显示，S 打开设置，Q 退出并移除托盘图标。
3. 无边框游戏：鼠标经过锚点点击、键盘移动，确认游戏正常接收输入，锚点不跟随视角或鼠标。
4. 设置：改变颜色、透明度、线宽、位置，确认即时变化；无效输入不破坏当前配置。
5. 配置：复制/重命名预设，重启后确认保留；导出、修改、导入确认恢复。
6. 多屏：分别选择 100%/150%/200% 缩放显示器，确认覆盖整个物理屏幕且尺寸按 DIP 缩放；拔插副屏测试回退。
7. 性能：静置 60 秒观察任务管理器 CPU，在相同游戏场景比较开启/隐藏叠加的帧率与帧时间。
8. 冲突：用占用的热键尝试应用，确认错误可见、原热键恢复；确认最后一套预设无法删除。

源码中使用 WinForms 仅为 NotifyIcon。WFAC010 是 WinForms 分析器对 manifest DPI 的建议；因 WPF 和 PerMonitorV2 要求，在项目中仅抑制该条分析器警告。

## 已执行的自动检查

Release 构建通过，0 警告、0 错误。配置与预设检查通过：

```powershell
dotnet run --project .\Verification\Verification.csproj -- .\StaticAnchorOverlay\config.example.json
```

验证程序检查深复制隔离、JSON 往返、最后预设保护、非法透明度、UNC 拒绝，并重新生成默认配置示例。
游戏中实际显示、点击穿透、混合 DPI 和帧率尚未做交互实测，请使用上面的验收清单。

## 5. 默认配置示例
```json
{
  "Version": 1,
  "MonitorDevice": "",
  "ActivePresetId": "default",
  "Hotkeys": {
    "Toggle": "Alt\u002BShift\u002BA",
    "Settings": "Alt\u002BShift\u002BS",
    "Quit": "Alt\u002BShift\u002BQ"
  },
  "Presets": [
    {
      "Id": "default",
      "Name": "\u9ED8\u8BA4",
      "Crosshair": {
        "Enabled": true,
        "Color": "#FFFFFF",
        "Opacity": 0.35,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Border": {
        "Enabled": true,
        "Color": "#FFFFFF",
        "Opacity": 0.18,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "CenterDot": {
        "Enabled": false,
        "Color": "#FFFFFF",
        "Opacity": 0.35,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Horizontal": {
        "Enabled": false,
        "Color": "#FFFFFF",
        "Opacity": 0.35,
        "X": 0,
        "Y": 0,
        "Length": 200,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Vertical": {
        "Enabled": false,
        "Color": "#FFFFFF",
        "Opacity": 0.35,
        "X": 0,
        "Y": 0,
        "Length": 200,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Corners": {
        "Enabled": true,
        "Color": "#FFFFFF",
        "Opacity": 0.25,
        "X": 0,
        "Y": 0,
        "Length": 24,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Grid": {
        "Enabled": false,
        "Color": "#FFFFFF",
        "Opacity": 0.08,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 1,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Vignette": {
        "Enabled": false,
        "Color": "#000000",
        "Opacity": 0.25,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      },
      "Png": {
        "Enabled": false,
        "Color": "#FFFFFF",
        "Opacity": 0.35,
        "X": 0,
        "Y": 0,
        "Length": 16,
        "Thickness": 2,
        "Gap": 5,
        "DotSize": 3,
        "Size": 4,
        "Inset": 24,
        "CornerRadius": 8,
        "Spacing": 120,
        "Depth": 100,
        "Width": 128,
        "Height": 128,
        "ImagePath": ""
      }
    }
  ]
}
```
