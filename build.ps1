[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts'),
    [switch]$BuildInstaller,
    [switch]$SkipDependencyDownload,
    [switch]$SkipCompile,
    [string]$InnoCompiler
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)
$version = Get-Content -LiteralPath (Join-Path $projectRoot 'version.json') -Raw | ConvertFrom-Json
$generated = & (Join-Path $projectRoot 'scripts\Generate-VersionInfo.ps1')
$dependencyDirectory = Join-Path $projectRoot '.build\dependencies'
if (!$SkipCompile -and !$SkipDependencyDownload) {
    & (Join-Path $projectRoot 'scripts\Prepare-BuildResources.ps1') -DestinationDirectory $dependencyDirectory | Out-Null
}
$javaZip = Join-Path $dependencyDirectory 'Paper26_2.java25.zip'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$portableExe = Join-Path $output 'Minecraft-Server-Launcher.exe'
$sources = @(
    'decompiled\Launcher.decompiled.cs',
    'AssemblyInfo.cs',
    'StorageConfiguration.cs',
    'ModernLauncherGui.cs',
    'RuntimeCompatibility.cs',
    'UpnpExternalAccess.cs',
    'BackupAndProfileTools.cs',
    'ContentAndDiagnostics.cs',
    'ManagedServerDashboard.cs',
    'NetworkAndPlayerTools.cs',
    'obj\GeneratedVersionInfo.cs'
) | ForEach-Object { Join-Path $projectRoot $_ }

$arguments = @(
    '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/debug-',
    "/win32icon:$projectRoot\launcher-icon.ico",
    "/win32manifest:$projectRoot\app.manifest",
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll',
    '/reference:System.IO.Compression.dll', '/reference:System.IO.Compression.FileSystem.dll',
    "/resource:$javaZip,Paper26_2.java25.zip",
    "/out:$portableExe"
) + $sources
if (!$SkipCompile) {
    foreach ($dependency in @($javaZip)) {
        if (!(Test-Path -LiteralPath $dependency)) { throw "Missing build dependency: $dependency" }
    }
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (!(Test-Path -LiteralPath $csc)) { throw 'The .NET Framework 4.x C# compiler was not found.' }
    & $csc @arguments
    if ($LASTEXITCODE -ne 0) { throw "C# build failed with exit code $LASTEXITCODE." }
}
elseif (!(Test-Path -LiteralPath $portableExe)) {
    throw "Portable EXE does not exist for -SkipCompile: $portableExe"
}

$packageRoot = Join-Path $projectRoot 'obj\portable'
Remove-Item -LiteralPath $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Copy-Item -LiteralPath $portableExe -Destination $packageRoot
foreach ($document in @('README.md', 'LICENSE', 'PRIVACY.md')) {
    $source = Join-Path $projectRoot $document
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $packageRoot }
}
$portableZip = Join-Path $output ("Minecraft-Server-Launcher-Portable-v{0}.zip" -f $version.productVersion)
Remove-Item -LiteralPath $portableZip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $portableZip -CompressionLevel Optimal

$setupPath = $null
if ($BuildInstaller) {
    if ([string]::IsNullOrWhiteSpace($InnoCompiler)) {
        $candidate = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($candidate) { $InnoCompiler = $candidate.Source }
    }
    if ([string]::IsNullOrWhiteSpace($InnoCompiler) -or !(Test-Path -LiteralPath $InnoCompiler)) {
        throw 'Inno Setup compiler was not found. Pass -InnoCompiler or install JRSoftware.InnoSetup.'
    }
    $marker = Join-Path $projectRoot 'obj\installed.mode'
    [IO.File]::WriteAllText($marker, "installed`r`n", [Text.UTF8Encoding]::new($false))
    & $InnoCompiler "/DMyAppVersion=$($version.productVersion)" "/DMyBuildNumber=$($version.buildNumber)" "/DSourceExe=$portableExe" "/DProjectRoot=$projectRoot" "/DOutputDir=$output" (Join-Path $projectRoot 'installer\MinecraftServerLauncher.iss')
    if ($LASTEXITCODE -ne 0) { throw "Installer build failed with exit code $LASTEXITCODE." }
    $setupPath = Join-Path $output ("Minecraft-Server-Launcher-Setup-v{0}.exe" -f $version.productVersion)
}

[pscustomobject]@{
    ProductVersion = $version.productVersion
    BuildNumber = $version.buildNumber
    PortableExe = $portableExe
    PortableZip = $portableZip
    SetupExe = $setupPath
}
