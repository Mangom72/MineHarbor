[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\obj\GeneratedVersionInfo.cs')
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = Get-Content -LiteralPath (Join-Path $projectRoot 'version.json') -Raw | ConvertFrom-Json
if ($version.productVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'productVersion must use MAJOR.MINOR.PATCH.' }
if ($version.buildNumber -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'buildNumber must use four numeric components.' }
if ($version.minimumSupportedVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'minimumSupportedVersion must use MAJOR.MINOR.PATCH.' }

$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$source = @"
using System.Reflection;

[assembly: AssemblyVersion("$($version.buildNumber)")]
[assembly: AssemblyFileVersion("$($version.buildNumber)")]
[assembly: AssemblyInformationalVersion("$($version.productVersion)")]

internal static class BuildVersionInfo
{
    public const string ProductVersion = "$($version.productVersion)";
    public const string BuildNumber = "$($version.buildNumber)";
    public const string MinimumSupportedVersion = "$($version.minimumSupportedVersion)";
    public const string DisplayVersion = "v$($version.productVersion) (build $($version.buildNumber))";
}
"@
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $source, [Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $OutputPath
