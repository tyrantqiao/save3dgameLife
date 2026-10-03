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
