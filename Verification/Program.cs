using StaticAnchorOverlay;

var sample = Path.GetFullPath(args.Length > 0 ? args[0] : "StaticAnchorOverlay/config.example.json");
var config = new Configuration(); config.Validate();
config.Presets[0].Id = "default"; config.ActivePresetId = "default";
ConfigStore.Write(sample, config);
var copy = PresetManager.Add(config, "副本", true);
copy.Crosshair.Color = "#00FF00";
if (config.Presets[0].Crosshair.Color != "#FFFFFF") throw new Exception("复制未隔离。");
var legacy = System.Text.Json.JsonSerializer.Deserialize<Preset>("{}")!;
if (legacy.EdgeBars.Enabled || legacy.Thirds.Enabled || legacy.EdgeDots.Enabled)
    throw new Exception("旧配置默认启用了新增元素。");
var schemes = BuiltInPresets.Create();
var schemeConfig = new Configuration(); schemeConfig.Validate();
foreach (var scheme in schemes) BuiltInPresets.Add(schemeConfig, scheme);
schemeConfig.Active.CenterDot.Size = 99;
if (schemes[4].CenterDot.Size != 6) throw new Exception("内置方案复制未隔离。");
var schemeJson = System.Text.Json.JsonSerializer.Serialize(schemeConfig);
var schemeRestored = System.Text.Json.JsonSerializer.Deserialize<Configuration>(schemeJson)!;
schemeRestored.Validate();
if (!schemeRestored.Presets[1].EdgeBars.Enabled || schemeRestored.Presets[1].EdgeBars.LengthRatio != 0.25 ||
    !schemeRestored.Presets[2].Thirds.Enabled || !schemeRestored.Presets[3].EdgeDots.Enabled)
    throw new Exception("新增方案往返失败。");
schemeRestored.Active.EdgeBars.LengthRatio = 1.1;
try { schemeRestored.Validate(); throw new Exception("未拒绝非法比例。"); }
catch (InvalidOperationException) { }
while (schemeConfig.Presets.Count < 100) BuiltInPresets.Add(schemeConfig, schemes[0]);
try { BuiltInPresets.Add(schemeConfig, schemes[0]); throw new Exception("未限制方案数量。"); }
catch (InvalidOperationException) { }
Console.WriteLine("PASS: built-in schemes, legacy defaults, template isolation, ratio validation, preset limit.");
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
