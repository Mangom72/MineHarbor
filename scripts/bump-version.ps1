param (
    [switch]$Major,
    [switch]$Minor,
    [switch]$Patch,
    [switch]$BuildOnly
)

$versionFile = "version.json"
if (-Not (Test-Path $versionFile)) {
    Write-Error "version.json not found."
    exit 1
}

$json = Get-Content $versionFile | ConvertFrom-Json

# 1. Update Product Version
$prodParts = $json.productVersion.Split('.')
if ($prodParts.Length -ne 3) { $prodParts = @("1", "0", "0") }

if ($Major) {
    $prodParts[0] = [int]$prodParts[0] + 1
    $prodParts[1] = 0
    $prodParts[2] = 0
} elseif ($Minor) {
    $prodParts[1] = [int]$prodParts[1] + 1
    $prodParts[2] = 0
} elseif ($Patch) {
    $prodParts[2] = [int]$prodParts[2] + 1
}

$json.productVersion = "{0}.{1}.{2}" -f $prodParts[0], $prodParts[1], $prodParts[2]

# 2. Update Build Number (increment the last part)
$buildParts = $json.buildNumber.Split('.')
if ($buildParts.Length -gt 0) {
    $lastIdx = $buildParts.Length - 1
    $buildParts[$lastIdx] = [int]$buildParts[$lastIdx] + 1
    $json.buildNumber = $buildParts -join '.'
}

$json | ConvertTo-Json -Depth 5 | Set-Content $versionFile

Write-Host "Updated version.json:"
Write-Host "Product Version : $($json.productVersion)"
Write-Host "Build Number    : $($json.buildNumber)"
