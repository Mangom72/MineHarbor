# Contributing

## 개발 환경

- Windows 10 또는 Windows 11 x64
- Windows에 포함된 .NET Framework 4.x C# 컴파일러
- SDK 스타일 호환 빌드 확인 시 .NET 10 SDK
- PowerShell 5.1 이상
- 설치 프로그램 빌드 시 Inno Setup 6.7 이상

## 빌드

```powershell
.\scripts\Prepare-BuildResources.ps1
.\build.ps1
dotnet build .\MineHarbor.csproj -c Release
```

설치 프로그램까지 만들려면 다음을 실행합니다.

```powershell
.\build.ps1 -BuildInstaller -InnoCompiler 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

외부 빌드 리소스는 `build-resources.json`에 URL, 크기와 SHA-256을 고정합니다. 해시를 확인하지 않은 파일로 값을 갱신하지 마세요.

## 테스트

```powershell
.\test.ps1
```

테스트는 임시 폴더를 사용해야 하며 실제 서버 데이터, 공유기 UPnP 매핑 또는 외부 포트 설정을 변경해서는 안 됩니다.

`test.ps1`은 버전·문서 일치, Portable EXE 버전, 콘텐츠 manifest와 데이터팩 실패 경로, 자동화 스키마 마이그레이션·요일/일회성 계산·놓친 작업·프로세스 간 잠금과 실행 임대, 백그라운드 설정의 기본 비활성화·손상/미래 스키마 보존·자동 시작 위임·접근성, 관리 자식 인계의 파이프·토큰·PID/시작 시각 검증·원자적 소유권 전환·응답 유실 복구·명령 단일 전달·로그 유지, 운영 기록 가림·연속 해시·읽음 상태, Windows 알림의 기본 비활성화·필터·조용한 시간·이전 기록 재생 방지·요약·민감값 가림, Discord 원격 설정의 DPAPI 암호화·허용 목록·역할 권한·확인 소유권/만료/재사용·속도 제한·임의 명령 차단·손상 원본 보존, 백업 보존, Paper/Purpur 복사 설정 YAML의 보존·백업·실패 경로, 빠른 명령의 필수·선택·기본값 인수와 단계 이동·값 유지·동적 후보 높이, 버전 호환성·3단계 위험도, 비동기 UI 종료 및 Paper/Purpur 브리지 프로토콜을 함께 검사합니다. PR과 `main` push는 `.github/workflows/ci.yml`, 태그와 수동 릴리스는 별도 `build-release.yml`에서 검증합니다.

릴리스 워크플로는 실행마다 임시 RSA-3072/SHA-256 자체서명 인증서와 난수 PFX 비밀번호를 만들고 Portable EXE와 설치 프로그램을 서명한 뒤 인증서와 PFX를 항상 삭제합니다. 자체서명은 파일 무결성 표시는 제공하지만 공개 신뢰나 SmartScreen 평판은 제공하지 않습니다. 외부 코드 서명 비밀을 이 경로에 추가하지 마세요.

정식 릴리스가 게시되면 `build-release.yml`은 공개 자산 7종을 다시 내려받고 자체서명 주체와 무결성을 검사한 다음, 직전 정식 버전의 실제 `ParseLauncherUpdateMetadata` 및 `DownloadLauncherUpdate` 루틴으로 새 EXE를 받습니다. 그 EXE의 크기·SHA-256·버전 리소스와 전체 회귀 테스트가 모두 통과해야 릴리스 작업이 성공합니다. 수동 재검증은 다음 명령을 사용합니다.

```powershell
.\scripts\Publish-LocalRelease.ps1 -NoPublish
.\scripts\Test-ReleaseArtifacts.ps1 -ArtifactsDirectory <공개-자산-폴더> -PublishedAssets -RequireSelfSignedSignature
.\scripts\Test-PublicAutoUpdate.ps1 -SourceLauncherPath <이전-MineHarbor.exe> -UpdateMetadataPath <공개-update.json> -DestinationPath <새-다운로드-경로>
```

## 변경 원칙

- `version.json`을 제품/빌드 버전의 단일 기준으로 사용합니다.
- 사용자 데이터 이동·삭제·덮어쓰기를 자동화하지 않습니다.
- 네트워크 다운로드에는 HTTPS, 허용된 호스트, 크기와 해시 검증을 적용합니다.
- UI 문구는 한국어와 영어를 함께 갱신합니다.
- 관련 없는 대규모 포맷팅을 피하고 한 변경에는 한 목적만 담습니다.
- 콘텐츠·자동화·운영 기록은 `.mineharbor` 아래에 제한된 크기와 스키마로 원자적으로 저장하고, 손상되거나 미래 버전인 원본을 자동으로 덮어쓰지 않습니다.
- 백그라운드 기능은 기본 비활성화, 현재 사용자 범위와 로컬 IPC를 유지하고, 소유하지 않은 서버 프로세스·포트·실행 중 백업을 건드리지 않습니다.
- 실행 중 관리 서버 인계는 등록 프로필, 자식과 현재·새 소유자의 PID/시작 시각, 실행별 토큰을 모두 확인하고 부분 실패 시 기존 소유권을 유지해야 합니다. 테스트에서는 실제 Java 서버 대신 현재 프로세스와 임시 파이프만 사용합니다.
- Windows 알림은 별도 동의, 민감 정보 가림, 조용한 시간과 폭주 제한을 유지하며 알림 실패가 서버 제어를 중단시키지 않게 합니다.
- Discord 원격 기능은 별도 동의, 현재 사용자 DPAPI 자격 증명, 길드·채널·사용자/역할·프로필 허용 목록과 속도 제한을 유지합니다. 임의 콘솔·셸·파일 실행 또는 외부 소유 서버 제어를 추가하지 않으며 테스트에서 실제 Discord 자격 증명이나 서버를 사용하지 않습니다.
- Paper/Purpur 설정은 지원되는 서버 종류와 버전 경로에서만 수정하고, 기존 YAML 주석·관련 없는 항목·수동 설정 마이그레이션 보호와 변경 전 백업을 유지합니다.
- 장시간 작업은 `Task`/`async`와 `CancellationToken`을 사용하고, 닫힌 폼에 완료 콜백을 보내지 않는 테스트를 추가합니다.
- `build.ps1`과 `MineHarbor.csproj`의 명시적 소스 목록을 함께 갱신합니다.

## English

Build on Windows with PowerShell and the .NET Framework compiler. Run `scripts\Prepare-BuildResources.ps1`, `build.ps1`, and `test.ps1`; with the .NET 10 SDK installed, also run `dotnet build MineHarbor.csproj -c Release` for the SDK-style `net48` compatibility path. Inno Setup 6.7 or newer is required for installer builds. Each release uses an ephemeral RSA-3072/SHA-256 self-signed certificate and random PFX password, signs the Portable EXE and installer, and always removes the certificate material. This provides an integrity signature, not public publisher trust or SmartScreen reputation. After publishing, the release workflow downloads all public assets, verifies their self-signed subject and integrity, uses the immediately preceding stable launcher's real update parser and download routine, and runs the full regression suite against that downloaded EXE. Keep `version.json` as the single version source, keep `build.ps1` and project source lists synchronized, never mutate real server/router/Discord data in tests, preserve corrupt or future-schema manifests, automation, operations history, Discord settings, and Paper/Purpur YAML instead of overwriting them, keep pre-change configuration backups, cancel long-running UI work safely, verify downloads, and update Korean and English UI/documentation together. Background features must remain opt-in and current-user scoped and must never take ownership of an unrelated server process or port. Discord remote control must retain current-user DPAPI token protection, guild/channel/user-or-role/profile allowlists, rate limits, single-use confirmations, and the prohibition on arbitrary console, shell, and file execution. Live managed-child handoff must verify the registered profile, exact child/current-owner/new-owner PID and start time, and a per-run token; tests must use a temporary pipe instead of a real Java server.
