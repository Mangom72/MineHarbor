[CmdletBinding()]
param(
    [string]$LauncherPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($LauncherPath)) {
    $build = & (Join-Path $projectRoot 'build.ps1') -OutputDirectory (Join-Path $projectRoot 'artifacts\test') -SkipDependencyDownload
    $LauncherPath = $build.PortableExe
}
$launcher = [IO.Path]::GetFullPath($LauncherPath)
if (!(Test-Path -LiteralPath $launcher)) { throw "Launcher not found: $launcher" }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testOutput = Join-Path $projectRoot 'obj\Launcher.Tests.exe'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $testOutput) | Out-Null
& $csc /nologo /target:exe /platform:anycpu /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/out:$testOutput" (Join-Path $projectRoot 'tests\Launcher.Tests.cs')
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed with exit code $LASTEXITCODE." }
& $testOutput $launcher
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE." }
$version = Get-Content -LiteralPath (Join-Path $projectRoot 'version.json') -Raw | ConvertFrom-Json
$versionInfo = (Get-Item -LiteralPath $launcher).VersionInfo
if ($versionInfo.ProductVersion.Trim() -ne [string]$version.productVersion) {
    throw "Portable EXE 제품 버전이 일치하지 않습니다: $($versionInfo.ProductVersion)"
}
if ($versionInfo.FileVersion.Trim() -ne [string]$version.buildNumber) {
    throw "Portable EXE 내부 빌드 번호가 일치하지 않습니다: $($versionInfo.FileVersion)"
}
Write-Host 'PORTABLE_VERSION_OK'
$smoke = Start-Process -FilePath $launcher -ArgumentList '--version' -Wait -PassThru -WindowStyle Hidden
if ($smoke.ExitCode -ne 0) { throw "Portable EXE smoke test failed with exit code $($smoke.ExitCode)." }
Write-Host 'PORTABLE_SMOKE_OK'
& (Join-Path $projectRoot 'scripts\Test-CommandBridge.ps1')
