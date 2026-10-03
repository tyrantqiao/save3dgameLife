# 项目协作指南

本文件适用于整个仓库。开始修改前阅读相关源码和已有文档；若子目录新增更具体的 AGENTS.md，遵循其作用域内的要求。

## 产品与技术

StaticAnchorOverlay 是 Windows 10/11 上的外部静态视觉锚点工具，使用 C#、.NET 8 和 WPF。WinForms 仅用于通知区域图标。当前没有第三方 NuGet 依赖。

用户界面和主要文档使用简体中文，代码标识符保持英文。根 README 面向用户和推广，`StaticAnchorOverlay/README.md` 保存详细操作、实现说明与验收清单。

## 源码导航

| 文件 | 职责 |
| --- | --- |
| `StaticAnchorOverlay/App.xaml.cs` | 启动、单实例、设置窗口、配置应用与退出清理 |
| `StaticAnchorOverlay/AnchorSurface.cs` | 静态锚点绘制与图片加载 |
| `StaticAnchorOverlay/OverlayWindow.xaml.cs` | 叠加窗口、显示器定位与显示环境变化 |
| `StaticAnchorOverlay/SettingsWindow.xaml` / `.xaml.cs` | 中文设置界面、输入校验和预设操作 |
| `StaticAnchorOverlay/Models.cs` | 配置模型、值校验与预设管理 |
| `StaticAnchorOverlay/ConfigStore.cs` | JSON 读写、自动保存与损坏配置备份 |
| `StaticAnchorOverlay/LocalFilePolicy.cs` | 本地绝对路径限制 |
| `StaticAnchorOverlay/NativeMethods.cs` | Win32 互操作与显示器信息 |
| `StaticAnchorOverlay/HotkeyService.cs` | 全局快捷键注册、冲突处理与释放 |
| `StaticAnchorOverlay/TrayService.cs` | 托盘菜单与预设切换 |
| `StaticAnchorOverlay/StartupService.cs` | 当前用户开机自启 |
| `Verification/Program.cs` | 配置与预设逻辑验证 |
| `Package.ps1` | 发布及生成分发包、源码包和完整源码文档 |

## 修改原则

- 保持外部叠加架构：不注入、不读写游戏内存、不修改游戏文件、不增加截屏或联网行为。不将本窗口的消息回调误称为游戏 Hook。
- 保持叠加点击穿透、不抢焦点、不出现在任务栏；设置窗口仍需支持任务栏入口。修改 Win32 样式时检查两类窗口的行为。
- 保持静态、事件触发重绘。不要为静态元素添加每帧定时器、动画或 `CompositionTarget.Rendering`；除可选 PNG 外避免创建全屏位图。
- 参数尺寸采用 DIP；显示器边界使用物理像素。保留 PerMonitorV2 和显示器 / DPI 变化处理，避免混用坐标单位。
- 配置变化必须经过校验。非法或未输入完整的值应保留上一合法配置，不破坏正在显示的效果。
- 保持预设深复制、唯一 ID、至少一套预设和最多 100 套预设的规则。变更配置格式时考虑既有 JSON 的兼容性和版本处理。
- 保存配置采用临时文件替换；读取损坏配置时先备份再恢复默认值。保留文件大小限制与本地路径检查，拒绝 UNC 和映射网络盘。
- PNG 保持本地读取、大小 / 像素限制和载入后不锁文件的行为。导入导出配置不隐式复制图片。
- 开机自启仅修改当前用户的 Run 注册项，无需管理员权限；不要让 JSON 预设导入导出改变自启设置。
- 保持热键冲突可见并尝试恢复原热键；退出时释放托盘、热键和单实例资源。
- 按现有 C# / XAML 风格做小范围修改，保持 nullable 检查；不要随意新增依赖或扩大警告抑制范围。

## 构建与验证

在 Windows 的仓库根目录执行，需要 .NET 8 SDK：

```powershell
dotnet build .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -c Release --ignore-failed-sources
dotnet run --project .\Verification\Verification.csproj -- "$env:TEMP\StaticAnchorOverlay-verification.json"
```

验证程序会写入参数指定的 JSON 文件，并检查预设复制隔离、JSON 往返、最后预设保护、透明度校验和 UNC 拒绝。未传参数时会重新生成仓库的 `config.example.json`；常规验证请使用临时输出路径。

逻辑改动运行相关验证，应用代码改动执行 Release 构建。仅文档改动检查命令、相对链接和描述与源码一致即可，不必重新发布应用。

涉及 UI、叠加、热键、启动或多屏时，按详细 README 的验收步骤测试。构建通过不能证明游戏内显示、点击穿透、混合 DPI 或帧率表现；交付时说明实际完成的验证及尚未验证的项目。

## 发布与文档

```powershell
dotnet publish .\StaticAnchorOverlay\StaticAnchorOverlay.csproj -p:PublishProfile=WindowsPortable -o .\dist\win-x64 --ignore-failed-sources
.\Package.ps1
```

`Package.ps1 -SkipPublish` 仅在已有匹配当前源码的发布产物时使用。`dist/`、`bin/`、`obj/` 为构建产物；`COMPLETE_SOURCE.md` 和源码 ZIP 由脚本生成，不手工编辑其中的源码副本。需要变更打包内容时修改脚本的文件清单。

新增或变更用户功能时同步对应使用文档。推广内容以已实现功能为依据，不承诺治疗效果、零资源占用、零掉帧或所有反作弊兼容；不要编造下载地址、截图、用户数据或已完成的测试。
