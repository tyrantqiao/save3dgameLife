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
