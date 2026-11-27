# 部署：把 dist\Tearscity\ 下的 dll/json/pck 复制到游戏 mods\Tearscity\
# 用法：powershell -ExecutionPolicy Bypass -File .\deploy.ps1
param(
    [string]$Sts2Dir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $projectDir "dist\Tearscity"
$dst = Join-Path $Sts2Dir "mods\Tearscity"

if (-not (Test-Path $src)) {
    Write-Host "未找到构建产物：$src" -ForegroundColor Red
    Write-Host "请先运行 build.ps1 或 dotnet build" -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Path (Join-Path $src "*") -Destination $dst -Force

Write-Host "已部署到：$dst" -ForegroundColor Green
Get-ChildItem $dst | Select-Object Name, Length | Format-Table -AutoSize
