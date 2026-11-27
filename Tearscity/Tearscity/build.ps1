# 一键构建：导出 pck -> 编译 dll -> 收集到 dist\Tearscity
# 用法：powershell -ExecutionPolicy Bypass -File .\build.ps1
# 可选：-Godot "C:\path\to\Godot_v4.5.1-stable_mono_win64_console.exe"
param(
    [string]$Godot = "C:\Users\ASUS\Documents\sts2 tools\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe"
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $projectDir
try {
    if (-not (Test-Path $Godot)) {
        throw "找不到 Godot：$Godot（用 -Godot 参数指定）"
    }

    Write-Host "== 1/3 导入 Godot 资源 ==" -ForegroundColor Cyan
    & $Godot --headless --path $projectDir --import

    Write-Host "== 2/3 导出 pck ==" -ForegroundColor Cyan
    & $Godot --headless --path $projectDir --export-pack "PCK" "$projectDir\Tearscity.pck"
    if (-not (Test-Path "$projectDir\Tearscity.pck")) {
        throw "pck 导出失败"
    }

    Write-Host "== 3/3 编译 dll 并收集到 dist ==" -ForegroundColor Cyan
    dotnet build -p:DeployMod=false

    Write-Host "`n构建完成，产物：" -ForegroundColor Green
    Get-ChildItem "$projectDir\dist\Tearscity" | Select-Object Name, Length | Format-Table -AutoSize
    Write-Host "部署到游戏：powershell -ExecutionPolicy Bypass -File .\deploy.ps1"
}
finally {
    Pop-Location
}
