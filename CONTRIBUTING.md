# Contributing

## 개발 환경

- Windows 10 또는 Windows 11 x64
- Windows에 포함된 .NET Framework 4.x C# 컴파일러
- PowerShell 5.1 이상
- 설치 프로그램 빌드 시 Inno Setup 6.7 이상

## 빌드

```powershell
.\scripts\Prepare-BuildResources.ps1
.\build.ps1
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

## 변경 원칙

- `version.json`을 제품/빌드 버전의 단일 기준으로 사용합니다.
- 사용자 데이터 이동·삭제·덮어쓰기를 자동화하지 않습니다.
- 네트워크 다운로드에는 HTTPS, 허용된 호스트, 크기와 해시 검증을 적용합니다.
- UI 문구는 한국어와 영어를 함께 갱신합니다.
- 관련 없는 대규모 포맷팅을 피하고 한 변경에는 한 목적만 담습니다.

## English

Build on Windows with PowerShell and the .NET Framework compiler. Run `scripts\Prepare-BuildResources.ps1`, `build.ps1`, and `test.ps1`. Inno Setup 6.7 or newer is required for installer builds. Keep `version.json` as the single version source, never mutate real server/router data in tests, preserve user data, verify downloads, and update Korean and English UI/documentation together.
