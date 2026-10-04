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
选择 PNG 后自动启用 PNG 元素；只读取本地图片，文件不超过 16 MB，宽高各不超过 4096 像素。
载入后不锁定图片；相同路径的图片内容替换后，需要清空路径再重新选择以刷新缓存。

配置自动保存至 `%AppData%\StaticAnchorOverlay\config.json`。保存使用临时文件后替换，读取失败时备份为 `.invalid-时间戳` 后使用默认配置。
记录显示器设备名、当前预设、热键。显示器暂时缺失时回退主屏，保留原显示器选择；重新连接后恢复。
预设与配置导入不复制 PNG 文件，迁移机器后需重新选择本地 PNG。

## 方案示意与自定义准心

![内置方案布局](docs/preset-preview.png)

“方案示意”页展示五种布局并支持添加独立预设，不覆盖已有预设；已有 JSON 缺少新增元素时，新增元素默认关闭。
四向瞄准线从四边沿水平/垂直中轴向中心伸入。LengthRatio 为 0–1，默认 0.25，即每条长度为对应屏幕尺寸的 25%；左右条按宽度、上下条按高度计算，实际长度受距中心 Gap 限制，默认中心间隙 24 DIP、条宽 8 DIP、Inset 为 0。
三等分竖条位于宽度的 1/3、2/3，贯穿全高，默认条宽 12 DIP。两侧大圆点每侧两列交错排列，内列圆点位于相邻外列圆点的中间（偏移 Spacing / 2），内列比外列少一个点；按 Spacing 纵向等间距排列并垂直居中，数量随屏幕高度变化；ColumnSpacing 控制列间距，默认 40 DIP。
新增元素的 X/Y 为整体 DIP 平移；瞄准线的 Inset 为线段起点距边缘距离；圆点的 Inset 为外列圆心距边缘距离，并至少为半直径以避免默认贴边裁切。旧预设中保存的线宽、长度比例和内缩保持原值；重新添加方案可获取新默认参数。
中心十字、中心圆点和 PNG 可自由组合。透明 PNG 可用作用户自己的中心准心，选择后自动启用，宽高用 DIP，默认 128 × 128；X/Y 为 0 时居中。导入导出只记录路径，不复制图片。
示意页使用与叠加相同的矢量绘制代码，以 960 × 540 DIP 缩放显示；不读取或截取游戏画面。

设计仅提供屏幕固定参照。视觉与前庭运动信号差异是晕动症的相关解释，不能据此断言游戏眩晕只由鼠标绑定造成，或认定玩多了 3D 必然导致晕 2D。参考 [NHS 晕动症说明](https://www.nhs.uk/conditions/motion-sickness/)，本工具的缓解效果未验证。

新增验收：逐一添加五种方案，确认示意与实际布局一致；在不同分辨率与 DPI 下确认瞄准线朝向与中心间隙、三等分位置和圆点居中；选择透明 PNG，确认居中、透明通道、宽高/偏移及加载失败提示；旧 JSON 导入后新增元素关闭。上述交互项目尚未实测。

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
