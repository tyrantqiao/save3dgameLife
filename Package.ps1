param([switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if (-not $SkipPublish) {
    dotnet publish ./StaticAnchorOverlay/StaticAnchorOverlay.csproj -p:PublishProfile=WindowsPortable -o ./dist/win-x64 --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw '发布失败。' }
}
$taskFiles = @('.gitignore', 'Package.ps1', 'GenerateIcon.ps1') + @(Get-ChildItem ./StaticAnchorOverlay,./Verification -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    ForEach-Object { [System.IO.Path]::GetRelativePath($PSScriptRoot, $_.FullName).Replace('\','/') })
$taskDoc = [System.Text.StringBuilder]::new()
[void]$taskDoc.AppendLine('# StaticAnchorOverlay 完整项目交付')
[void]$taskDoc.AppendLine('## 1. 项目结构和文件清单')
[void]$taskDoc.AppendLine('```text')
foreach ($taskFile in $taskFiles) { [void]$taskDoc.AppendLine($taskFile) }
[void]$taskDoc.AppendLine('```')
[void]$taskDoc.AppendLine('## 2. 每个文件的完整代码')
foreach ($taskFile in $taskFiles | Where-Object { $_ -notlike '*README.md' -and $_ -notlike '*.json' -and $_ -notlike '*.ico' }) {
    [void]$taskDoc.AppendLine('### ' + $taskFile)
    $taskLanguage = switch ([System.IO.Path]::GetExtension($taskFile)) { '.cs' { 'csharp' } '.ps1' { 'powershell' } '.xaml' { 'xml' } '.csproj' { 'xml' } '.pubxml' { 'xml' } '.manifest' { 'xml' } default { 'text' } }
    [void]$taskDoc.AppendLine('```' + $taskLanguage)
    [void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $taskFile)).TrimEnd())
    [void]$taskDoc.AppendLine('```')
}
[void]$taskDoc.AppendLine('## 3. 构建命令和运行步骤 / 4. 使用说明')
[void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StaticAnchorOverlay/README.md')))
[void]$taskDoc.AppendLine('## 5. 默认配置示例')
[void]$taskDoc.AppendLine('```json')
[void]$taskDoc.AppendLine([System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StaticAnchorOverlay/config.example.json')))
[void]$taskDoc.AppendLine('```')
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'COMPLETE_SOURCE.md'), $taskDoc.ToString(), [System.Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression
$taskZipStream = [System.IO.File]::Create((Join-Path $PSScriptRoot 'StaticAnchorOverlay-source.zip'))
$taskArchive = [System.IO.Compression.ZipArchive]::new($taskZipStream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in $taskFiles + @('COMPLETE_SOURCE.md')) {
        $taskEntry = $taskArchive.CreateEntry($taskFile)
        $taskOutput = $taskEntry.Open()
        $taskInput = [System.IO.File]::OpenRead((Join-Path $PSScriptRoot $taskFile))
        try { $taskInput.CopyTo($taskOutput) } finally { $taskInput.Dispose(); $taskOutput.Dispose() }
    }
} finally { $taskArchive.Dispose(); $taskZipStream.Dispose() }
Copy-Item -LiteralPath ./StaticAnchorOverlay/README.md -Destination ./dist/win-x64/README.md
Compress-Archive -Path ./dist/win-x64/* -DestinationPath ./dist/StaticAnchorOverlay-windows-x64.zip -Force
Get-Item ./dist/win-x64/StaticAnchorOverlay.exe,./dist/StaticAnchorOverlay-windows-x64.zip | Select-Object Name,Length


