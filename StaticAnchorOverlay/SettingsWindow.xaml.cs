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

