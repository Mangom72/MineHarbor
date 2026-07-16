[CmdletBinding()]
param(
    [string]$DependencyDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($DependencyDirectory)) { $DependencyDirectory = Join-Path $projectRoot '.build\dependencies' }
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'build-resources.json') -Raw | ConvertFrom-Json
$javaZip = Join-Path $DependencyDirectory $manifest.java.fileName
$compilerRoot = Join-Path $projectRoot '.build\bridge-jdk'
$javac = Get-ChildItem -LiteralPath $compilerRoot -Recurse -Filter javac.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (!$javac) {
    if (!(Test-Path -LiteralPath $javaZip)) { throw "Missing Java build resource: $javaZip" }
    New-Item -ItemType Directory -Force -Path $compilerRoot | Out-Null
    Expand-Archive -LiteralPath $javaZip -DestinationPath $compilerRoot -Force
    $javac = Get-ChildItem -LiteralPath $compilerRoot -Recurse -Filter javac.exe | Select-Object -First 1 -ExpandProperty FullName
}
$java = Join-Path (Split-Path -Parent $javac) 'java.exe'
$classes = Join-Path $projectRoot 'obj\bridge-protocol-tests'
Remove-Item -LiteralPath $classes -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $classes | Out-Null
$protocol = Join-Path $projectRoot 'bridge\paper\src\main\java\io\github\mcserverlauncher\bridge\BridgeProtocol.java'
$test = Join-Path $projectRoot 'bridge\paper\src\test\java\io\github\mcserverlauncher\bridge\BridgeProtocolTest.java'
$batPath = Join-Path $projectRoot 'obj\bridge-protocol-tests\run.bat'
Set-Content -Path $batPath -Value "`"$javac`" --release 8 -Xlint:-options -encoding UTF-8 -d `"$classes`" `"$protocol`" `"$test`"`r`nif %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%`r`n`"$java`" -cp `"$classes`" io.github.mcserverlauncher.bridge.BridgeProtocolTest`r`nif %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%" -Encoding ASCII
& cmd.exe /c $batPath
if ($LASTEXITCODE -ne 0) { throw 'Bridge protocol tests failed.' }
