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
