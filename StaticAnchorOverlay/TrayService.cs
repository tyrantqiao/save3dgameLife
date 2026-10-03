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

