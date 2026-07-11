[CmdletBinding()]
param(
    [string]$DestinationDirectory = (Join-Path $PSScriptRoot '..\.build\dependencies')
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$destination = [IO.Path]::GetFullPath($DestinationDirectory)
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'build-resources.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $destination | Out-Null

foreach ($name in @('paper', 'java')) {
    $item = $manifest.$name
    $target = Join-Path $destination $item.fileName
    $valid = Test-Path -LiteralPath $target
    if ($valid) {
        $file = Get-Item -LiteralPath $target
        $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $valid = $file.Length -eq [long]$item.size -and $hash.Equals([string]$item.sha256, [StringComparison]::OrdinalIgnoreCase)
    }
    if ($valid) {
        Write-Host "Verified $($item.fileName)"
        continue
    }

    $temporary = $target + '.download'
    Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    Write-Host "Downloading $($item.fileName)"
    Invoke-WebRequest -Uri $item.url -OutFile $temporary -Headers @{ 'User-Agent' = 'Minecraft-Server-Launcher-Build/0.3' }
    $download = Get-Item -LiteralPath $temporary
    $downloadHash = (Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash
    if ($download.Length -ne [long]$item.size -or !$downloadHash.Equals([string]$item.sha256, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
        throw "Build resource verification failed: $($item.fileName)"
    }
    Move-Item -LiteralPath $temporary -Destination $target -Force
}

[pscustomobject]@{
    PaperJar = Join-Path $destination $manifest.paper.fileName
    JavaZip = Join-Path $destination $manifest.java.fileName
}
