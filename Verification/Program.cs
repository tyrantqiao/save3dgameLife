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
