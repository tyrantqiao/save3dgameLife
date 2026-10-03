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
