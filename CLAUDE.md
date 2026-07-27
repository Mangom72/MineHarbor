# MineHarbor — 작업 지침

Windows용 Minecraft 서버 런처입니다. C# / WinForms / .NET Framework 4.8, 빌드는 .NET SDK 10.

**이 파일이 이 저장소의 유일한 작업 지침입니다.** 빌드·테스트·릴리스의 세부 절차는 `CONTRIBUTING.md`,
직전 세션의 작업 내역은 `docs/WORK_LOG.md`에 있습니다.

## 작업 시작 전

1. `docs/WORK_LOG.md` 맨 위 항목 — 직전 세션이 무엇을 했고 무엇이 남았는지
2. `git status`와 현재 브랜치 — 커밋되지 않은 변경이 있는지

## 개발 명령 (Windows PowerShell)

```powershell
.\scripts\Prepare-BuildResources.ps1
.\build.ps1
.\test.ps1 -LauncherPath artifacts\MineHarbor.exe
```

코드를 고쳤으면 **반드시 위 셋을 돌려 통과를 확인**한 뒤 커밋합니다.
빌드 산출물 `artifacts\MineHarbor.exe`를 직접 실행해 화면을 확인하는 것이 가장 확실합니다.
설치 프로그램 빌드, `dotnet build`로 하는 SDK 스타일 확인, 로컬 자체서명 릴리스 검증은 `CONTRIBUTING.md`를 보세요.

새 소스 파일을 추가하면 `build.ps1`과 `MineHarbor.csproj`의 **명시적 소스 목록을 함께** 갱신해야 합니다.

## 이 저장소에서 반드시 지킬 것

### 줄바꿈과 인코딩

`decompiled/Launcher.decompiled.cs`는 CRLF·CR·LF가 섞여 있습니다(CR CR LF 3550, bare LF 417).
일반 편집 도구로 저장하면 줄바꿈이 전체 정규화되어 **실제 변경이 몇 줄이어도 4000줄짜리 가짜 diff**가 납니다.
이 파일은 바이트 단위로 편집하고, 편집 후 CRLF·CR·LF 개수가 그대로인지 확인하세요.
`.gitattributes`가 `* -text`로 Git 쪽 정규화는 막고 있지만, 편집 도구는 막지 못합니다.

`.ps1`과 `CHANGELOG.md`는 **UTF-8 with BOM**으로 저장해야 합니다. Windows PowerShell 5.1이 BOM 없는 유니코드를 깨뜨립니다.
`.cs` 파일과 나머지 문서는 기존 인코딩(BOM 유무)을 그대로 유지하세요.

### 예외 메시지는 한국어·영어를 함께

예외 타입은 바꾸지 않습니다. `catch (InvalidDataException)`처럼 타입에 의존하는 곳이 20군데 있습니다.

```csharp
throw Localized(new InvalidDataException("한국어 문구"), "English wording");
```

사용자에게 보여 줄 때는 `exception.Message`가 아니라 `DescribeException(exception)`을 씁니다.
영어 문구가 없는 예외는 원래 메시지를 그대로 쓰므로 안전합니다.

### UI 문구와 디자인

- UI 문구를 바꾸면 **한국어와 영어를 함께** 수정합니다.
- Windows 기본 알림창을 쓰지 않고 `ModernDialogs.cs`의 커스텀 대화상자를 씁니다.
- 둥근 모서리의 현대적 디자인(Toss 앱 스타일)을 유지합니다. 버튼 이름을 길게 늘이는 대신 툴팁을 씁니다.
- 새 창에는 `AutoScaleMode.Dpi`와 접근성 정보(`AccessibleName`, `AccessibleDescription`)를 지정합니다.
- 기존 모듈 구조(UI, Network, Bridge, Storage)를 깨지 않습니다.
- 장시간 작업은 `Task`/`async`와 `CancellationToken`을 쓰고, 닫힌 폼에 완료 콜백을 보내지 않습니다.

`test.ps1`이 소스를 스캔해 `MessageBox.Show`, 기본 `new Button()`, 기본 `new CheckBox()` 사용을 차단합니다.

### 보안 경계 (건드리지 말 것)

- 다운로드는 HTTPS, 호스트 허용 목록, 크기·해시 검증을 유지합니다.
- UPnP는 런처가 직접 만든 매핑만 삭제합니다. 사용자가 연 포트는 절대 건드리지 않습니다.
- Discord 원격 제어는 임의 콘솔·셸·파일 명령을 노출하지 않습니다. 허용 사용자·역할·채널·프로필을 모두 검사합니다.
- 백그라운드 기능은 기본 비활성화·현재 사용자 범위·로컬 IPC를 유지하고, 소유하지 않은 프로세스나 포트를 건드리지 않습니다.
- 로그를 외부로 내보내는 경로(`SanitizeOperationMessage`, `RedactDiagnosticText`)는 IPv4·IPv6 주소와 경로를 가립니다.
- `.mineharbor` 아래 설정·기록은 크기·스키마 검증과 원자적 교체를 쓰고, 손상되거나 미래 스키마인 원본을 덮어쓰지 않습니다.
- 비밀키·토큰·인증서를 저장소에 넣지 않습니다.

### 버전과 릴리스

- 버전의 단일 기준은 `version.json`입니다. `.\scripts\bump-version.ps1 -Patch|-Minor|-Major`로 올립니다.
- `CHANGELOG.md`에 해당 버전 항목을 **`### Korean`과 `### English` 둘 다** 작성합니다.
- `README.md`의 버전 표기도 함께 갱신합니다. `Test-VersionConsistency.ps1`이 검사합니다.
- 제품 코드가 바뀌지 않는 변경(문서, `.gitattributes` 등)은 버전을 올리지 않고 릴리스도 하지 않습니다.

릴리스는 GitHub Actions가 수행합니다. `main`에 병합한 뒤 `v<버전>` 태그를 푸시하거나,
`build-release.yml`을 `publish_release: true`로 실행합니다. 워크플로가 빌드·테스트·서명·패키징과
이전 버전 런처를 통한 자동 업데이트 검증까지 수행합니다.

### Git

- `main`에 직접 커밋하거나 푸시하지 않습니다. 브랜치에서 작업하고 PR로 병합합니다.
- `git reset --hard`, `git clean -fd`, 강제 푸시는 사용자 승인 없이 실행하지 않습니다.
- 브랜치를 만들 때 **먼저 `git fetch origin main`** 하세요. 오래된 `origin/main`에서 브랜치를 만들면
  최신 변경이 빠진 채로 작업하게 됩니다.
- 저장소 밖의 파일을 사용자 승인 없이 수정하거나 삭제하지 않습니다.
- 관련 없는 대규모 포맷팅을 하지 않습니다. 한 변경에는 한 목적만 담습니다.

## 작업 종료 시

`docs/WORK_LOG.md` 맨 위에 이번 작업 내역을 추가합니다.
무엇을 고쳤는지, 왜 그렇게 했는지, 검증은 어떻게 했는지, 무엇이 남았는지를 다음 세션이 알 수 있게 씁니다.

## 실제 사용자 자원

테스트는 임시 폴더만 사용합니다. 실제 사용자 서버 데이터, 공유기 설정, UPnP 매핑, 외부 포트,
Discord 자격 증명을 사용하거나 변경하지 않습니다.
