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
& (Join-Path $projectRoot 'scripts\Test-VersionConsistency.ps1') -LauncherPath $launcher
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

# 새 기능에서 Windows 기본 알림창이나 네모난 표준 버튼이 다시 들어오지 않도록 소스를 검사합니다.
$uiSources = @(
    Get-ChildItem -LiteralPath $projectRoot -File -Filter '*.cs'
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'decompiled') -File -Filter '*.cs'
)
$uiText = [Text.StringBuilder]::new()
foreach ($source in $uiSources) {
    $text = Get-Content -LiteralPath $source.FullName -Raw
    [void]$uiText.AppendLine($text)
    if ($text -match 'MessageBox\s*\.\s*Show\s*\(') { throw "Windows 기본 알림창 호출이 남아 있습니다: $($source.Name)" }
    if ($text -match 'new\s+(?:System\.Windows\.Forms\.)?Button\s*\(') { throw "네모난 표준 버튼 생성이 남아 있습니다: $($source.Name)" }
    if ($text -match 'new\s+(?:System\.Windows\.Forms\.)?(?:ComboBox|CheckBox|TrackBar|HScrollBar|VScrollBar)\s*\(') {
        throw "테마를 우회하는 기본 선택·드래그 컨트롤이 남아 있습니다: $($source.Name)"
    }
}
$formCount = [regex]::Matches($uiText.ToString(), '\bclass\s+\w+\s*:\s*Form\b').Count
$dpiFormCount = [regex]::Matches($uiText.ToString(), 'AutoScaleMode\s*=\s*AutoScaleMode\.Dpi').Count
if ($formCount -gt 0 -and $dpiFormCount -lt $formCount) {
    throw "DPI 배율이 적용되지 않은 창이 있을 수 있습니다: forms=$formCount, dpi=$dpiFormCount"
}
Write-Host 'MODERN_DIALOG_SCAN_OK'

# 감사에서 확인한 공급망·서명 회귀가 다시 들어오지 않도록 정적 안전 조건을 검사합니다.
$signScript = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts\sign-build.ps1') -Raw
if ($signScript -match 'New-SelfSignedCertificate|mineharbor123') { throw '고정 비밀번호 또는 자동 개발 인증서 생성 코드가 다시 들어왔습니다.' }
$selfSignScript = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts\New-SelfSignedReleaseCertificate.ps1') -Raw
$selfSignCleanupScript = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts\Remove-SelfSignedReleaseCertificate.ps1') -Raw
if ($selfSignScript -notmatch 'RandomNumberGenerator' -or $selfSignScript -match 'mineharbor123') { throw '자체서명 릴리스 인증서의 임시 비밀번호가 안전하게 생성되지 않습니다.' }
if ($selfSignCleanupScript -notmatch 'CurrentUser\\My' -or $selfSignCleanupScript -notmatch 'Remove-Item') { throw '임시 자체서명 인증서 정리 경로가 누락되었습니다.' }
$launcherSource = Get-Content -LiteralPath (Join-Path $projectRoot 'decompiled\Launcher.decompiled.cs') -Raw
if ($launcherSource -notmatch 'Forge Installer의 SHA-256 검증에 실패했습니다' -or $launcherSource -notmatch 'AllowAutoRedirect\s*=\s*false') {
    throw 'Forge 다운로드 해시 또는 리디렉션 검증이 누락되었습니다.'
}
$releaseWorkflow = Get-Content -LiteralPath (Join-Path $projectRoot '.github\workflows\build-release.yml') -Raw
if ($releaseWorkflow -match 'uses:\s*[^\r\n]+@v\d') { throw 'GitHub Actions가 변경 가능한 버전 태그를 사용합니다.' }
if ($releaseWorkflow -notmatch 'persist-credentials:\s*false') { throw '체크아웃 자격 증명 유지 차단이 누락되었습니다.' }
if ($releaseWorkflow -match '(?m)^\s{2}SIGNING_CERTIFICATE:\s*\$\{\{\s*secrets\.') {
    throw '서명 인증서가 작업 전체 환경 변수로 노출됩니다.'
}
if ($releaseWorkflow -match 'secrets\.WINDOWS_CERTIFICATE' -or $releaseWorkflow -notmatch 'New-SelfSignedReleaseCertificate\.ps1' -or $releaseWorkflow -notmatch 'Remove-SelfSignedReleaseCertificate\.ps1') {
    throw '릴리스가 임시 자체서명 전용 경로와 정리 단계를 사용하지 않습니다.'
}
$releaseArtifactScript = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts\New-ReleaseArtifacts.ps1') -Raw
if ($releaseArtifactScript -match '코드 서명 인증서가 없어' -or $releaseArtifactScript -notmatch '자체서명 무결성 서명') { throw '릴리스 안내의 자체서명 설명이 실제 배포 정책과 다릅니다.' }
$bumpScript = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts\bump-version.ps1') -Raw
if ($bumpScript -notmatch 'WriteAllText' -or $bumpScript -notmatch '하나만 지정') {
    throw '버전 갱신의 UTF-8 저장 또는 모드 상호 배타 검증이 누락되었습니다.'
}
$discordSource = Get-Content -LiteralPath (Join-Path $projectRoot 'DiscordRemoteManagement.cs') -Raw
if ($discordSource -notmatch 'ProtectedData\.Protect' -or $discordSource -notmatch 'DataProtectionScope\.CurrentUser') {
    throw 'Discord 봇 토큰의 현재 사용자 범위 DPAPI 보호가 누락되었습니다.'
}
if ($discordSource -match '\b(?:HttpListener|TcpListener)\b') {
    throw 'Discord 원격 제어에 공개 수신 리스너가 추가되었습니다.'
}
if ($discordSource -notmatch '\{\s*"intents",\s*0\s*\}' -or $discordSource -notmatch '"allowed_mentions"') {
    throw 'Discord 특권 Intent 차단 또는 응답 멘션 차단이 누락되었습니다.'
}
if (($discordSource -match 'Process\.Start|cmd\.exe|powershell(?:\.exe)?') -or
    ($discordSource -notmatch 'Unsupported remote command') -or
    ($discordSource -match 'CreateDiscordSubcommand\("command"')) {
    throw 'Discord 원격 제어의 명령 화이트리스트 경계가 누락되었거나 외부 실행 경로가 추가되었습니다.'
}
Write-Host 'SECURITY_REGRESSION_SCAN_OK'
