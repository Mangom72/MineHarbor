# AI Agent Synchronization State

## Codex Opt-in Windows Notifications - 2026-07-26

- **Current Version**: 1.13.0 (build 26.2.45.77)
- **Branch**: `codex/v1.13.0-release-state` (기능 브랜치: `codex/windows-notifications-v1.13.0`)
- **Status**: 선택형 Windows 작업 표시줄 알림, 중요도·종류·조용한 시간, 이전 기록 재생 방지와 폭주 요약을 구현해 [PR #34](https://github.com/Mangom72/MineHarbor/pull/34)를 병합하고 [v1.13.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.13.0) 공개 검증 완료
- 알림은 기본 비활성화이며 백그라운드 에이전트가 실행 중일 때만 기존 트레이 `NotifyIcon`으로 새 운영 기록을 표시합니다. 운영 기록, 백그라운드 설정과 트레이에서 알림 설정 화면을 열 수 있습니다.
- `windows-notifications.json`은 사용자 데이터에 최대 64KiB 스키마 검증, 경로별 프로세스 간 뮤텍스와 원자적 교체로 저장합니다. 손상·미래 스키마 원본을 덮어쓰지 않습니다.
- 최소 중요도는 정보·경고·오류, 종류는 서버·예약·백업·콘텐츠·네트워크·업데이트/보안으로 나누며 자정을 지나는 조용한 시간을 지원합니다.
- 에이전트 시작 시 서버별 최신 기록만 기준점으로 저장해 과거 기록을 다시 알리지 않습니다. 대기 항목은 최대 50개이고 8초 안에 여러 건이 생기면 가장 중요한 최신 사건과 추가 개수로 요약합니다.
- 알림 문구는 운영 기록에서 가린 절대 서버 경로·IPv4·토큰·비밀번호·웹훅 형태를 다시 정리하고 명령 원문을 표시하지 않습니다. 알림 실패는 서버 제어·백업·예약 실행에서 격리합니다.
- `Prepare-BuildResources.ps1`, `build.ps1 -SkipDependencyDownload`, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`가 통과했습니다. 현재 결과는 `VERSION_CONSISTENCY_OK`, `PASSED=30`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`입니다. [PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30187511660)와 [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30187553178)는 .NET 10 SDK 경고=오류 `net48` 빌드, Portable·브리지 빌드, 전체 테스트와 검증 자산 업로드까지 통과했습니다.
- [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30187596732)는 임시 RSA-3072/SHA-256 자체서명 인증서 생성·폐기, 설치 파일·Portable·브리지와 SHA-256 공개 자산 7종을 검증했습니다. 공개 자산을 별도 임시 폴더에 다시 내려받아 `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, `PUBLIC_AUTO_UPDATE_OK=1.12.0->1.13.0`을 확인하고 업데이트된 공개 EXE로 30개 테스트 그룹과 브리지 10개를 다시 통과했습니다.
- Windows 알림 내구 저장소·동작 버튼, 플레이어 접속·퇴장 전용 알림, 웹/Discord 원격 관리와 GUI 소유 실행 서버의 무손실 이전은 구현된 것으로 표시하지 않습니다.

## Codex Background Operations Beta - 2026-07-26

- **Current Version**: 1.12.0 (build 26.2.45.76)
- **Branch**: `codex/v1.12.0-release-state` (기능 브랜치: `codex/background-agent-v1.12.0`)
- **Status**: 사용자 계정용 백그라운드 에이전트·트레이·로컬 IPC·예약 프로세스 간 중복 방지를 구현해 [PR #32](https://github.com/Mangom72/MineHarbor/pull/32)를 병합하고 [v1.12.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.12.0) 공개 검증 완료
- 백그라운드 운영은 기본 비활성화이며 `서버 관리 → 백그라운드`의 명시적 동의 후에만 `MineHarbor.exe --background-agent`를 실행합니다. Windows 로그인 자동 시작과 충돌 재시작은 별도 선택 사항입니다.
- 트레이에서 서버별 시작·안전 종료·재시작·즉시 백업·콘솔, 전체 안전 종료, 예약 일시 중지와 완전 종료를 지원합니다. 에이전트 콘솔은 기존 자동완성과 위험 명령 확인을 재사용합니다.
- IPC는 현재 Windows SID 파생 이름과 해당 SID 전용 ACL을 가진 로컬 이름 있는 파이프를 사용합니다. 요청 16KiB, 응답 256KiB, 수신 3초 상한을 적용하고 외부 네트워크나 임의 셸 실행을 제공하지 않습니다.
- 에이전트가 직접 시작한 관리 자식만 소유하며 다른 프로세스가 같은 포트를 사용하면 명령·종료·실행 중 백업을 거부합니다. 관리 자식은 부모가 사라졌을 때 서버에 `stop`을 먼저 보냅니다.
- 자동화 파일은 서버 경로별 Windows 뮤텍스와 PID·시작 시각 임대를 함께 사용해 GUI/에이전트 중복 청구를 차단합니다. 절전 복귀 시 기존 놓친 작업 정책을 다시 평가합니다.
- 에이전트와 런처 업데이트는 소유 서버가 제한 시간 안에 안전 종료된 경우에만 계속하며, 실패하면 강제 종료 대신 취소합니다.
- `Prepare-BuildResources.ps1`, Portable `build.ps1`과 전체 `test.ps1`이 통과했습니다. 현재 결과는 `VERSION_CONSISTENCY_OK`, `PASSED=29`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`입니다. [PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30186372130)와 [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30186411385)는 .NET 10 SDK 경고=오류 `net48` 빌드, Portable·브리지 빌드, 전체 테스트와 검증 자산 업로드까지 통과했습니다.
- [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30186453428)는 임시 RSA-3072/SHA-256 자체서명 인증서 생성·폐기, 설치 파일·Portable·브리지와 SHA-256 공개 자산 7종을 검증했습니다. 공개 자산을 별도 임시 폴더에 다시 내려받아 `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, `PUBLIC_AUTO_UPDATE_OK=1.11.0->1.12.0`을 확인하고 업데이트된 공개 EXE로 29개 테스트 그룹과 브리지 10개를 다시 통과했습니다.
- Windows 알림, 웹/Discord 원격 관리, 관리자 권한 서비스, 로그인 전 실행, GUI가 직접 시작한 실행 중 서버의 무손실 소유권 이전은 구현된 것으로 표시하지 않습니다.

## Codex Operations Foundation - 2026-07-26

- **Current Version**: 1.11.0 (build 26.2.45.75)
- **Branch**: `codex/v1.11.0-release-state` (기능 브랜치: `codex/operations-foundation`)
- **Status**: 고급 예약 신뢰성·놓친 작업 정책·변조 감지 운영 기록 구현, [PR #30](https://github.com/Mangom72/MineHarbor/pull/30) 병합 및 [v1.11.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.11.0) 공개 검증 완료
- 자동화 스키마를 2로 올려 반복 간격·매일에 선택 요일과 특정 날짜 한 번을 추가했습니다. 스키마 1은 읽을 때만 메모리에서 호환하고, 미래/손상 스키마는 원본을 덮어쓰지 않습니다.
- 놓친 작업은 최대 지연 시간을 기준으로 다음 실행 때 한 번 실행, 건너뛰기, 알림만 기록을 선택할 수 있습니다. 일회성 작업은 임대 시 비활성화하고, 놓친 횟수만큼 무제한 실행하지 않습니다.
- 예약 편집기는 다음 예상 실행, 위험도, 서버가 꺼졌을 때 처리와 5분 이내 충돌을 보여 줍니다. 메인 관리 도구는 양언어 3×3 배치로 정리했습니다.
- 서버 시작·종료·충돌·자동 재시작과 예약 결과를 서버별 `.mineharbor/operations-history.json`에 최대 500개 보관합니다. 크기/스키마/중복/시각 검증, 원자적 교체, 경로별 프로세스 간 뮤텍스와 SHA-256 연속 해시를 사용하고 경로·IPv4·비밀값을 가립니다.
- 메인 `운영 기록` 화면은 서버·중요도·읽음 상태 필터, 개별/전체 읽음, 새로고침과 CSV 내보내기를 지원합니다.
- `Prepare-BuildResources.ps1`, `build.ps1 -SkipDependencyDownload`, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`가 통과했습니다. 결과는 `VERSION_CONSISTENCY_OK`, `PASSED=28`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`입니다. [PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30183624045)와 [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30183685789)는 .NET 10 SDK 경고=오류 `net48` 빌드, Portable·브리지 빌드, 전체 테스트와 검증 자산 업로드까지 통과했습니다.
- [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30183726971)는 임시 RSA-3072/SHA-256 자체서명 인증서 생성·폐기, 설치 파일·Portable·브리지와 SHA-256 공개 자산 7종을 검증했습니다. 별도 로컬 임시 폴더에 공개 자산을 다시 내려받아 `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, `PUBLIC_AUTO_UPDATE_OK=1.10.0->1.11.0`을 확인하고, 업데이트된 공개 EXE로 28개 테스트 그룹과 브리지 10개를 다시 통과했습니다.
- 백그라운드 에이전트·시스템 트레이·Windows 알림·웹/Discord 원격 관리·클라우드 백업·Fabric/Forge/NeoForge 브리지는 이번 범위에 포함하지 않았습니다. 일정은 기존처럼 MineHarbor 메인 또는 여러 서버 관리 창이 열려 있을 때만 평가됩니다.

## Codex Step-by-step Command Builder - 2026-07-26

- **Current Version**: 1.10.0 (build 26.2.45.74)
- **Branch**: `codex/v1.10.0-release-state` (기능 브랜치: `codex/command-builder-ux-v1.10.0`)
- **Status**: 단계형 빠른 명령 빌더를 구현해 [PR #28](https://github.com/Mangom72/MineHarbor/pull/28)을 병합하고 [v1.10.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.10.0) 게시 및 공개 자동 업데이트 재검증 완료
- 명령 템플릿을 고정 문구와 `{필수}`, `[선택]`, `[선택=기본값]` 인수로 분리합니다. 미완성·현재·완료·잘못된 값을 시각적으로 구분하고 필수 인수의 실제 유효성으로 전송 가능 여부를 판단합니다.
- 후보를 확정하면 다음 인수로 자동 이동하고 `Shift+Tab`으로 돌아가도 다른 값을 유지합니다. 명령별 초안과 목록을 닫았다 다시 여는 상태를 보존하며, `Esc`로 명시적으로 취소하거나 실제 전송에 성공했을 때만 초기화합니다.
- 온라인 플레이어·선택자, 게임 모드, 난이도, 불리언, 숫자·시간·좌표, 부분 검색 가능한 아이템·효과 후보를 제공합니다. 빠른 명령 목록은 최대 40개·430px, 공통 자동완성은 최대 20개·380px까지 가용 공간에 맞춰 확장하고 입력 영역과 겹치지 않습니다.
- 후보 적용 `Enter`가 뒤의 콘솔 전송으로 이어지지 않게 이벤트를 소비하고 관리 콘솔도 처리된 입력을 다시 보내지 않습니다. 위험 명령은 모든 필수 인수가 유효한 최종 전송 시점에만 완성 명령을 보여 주고 확인합니다.
- `Prepare-BuildResources.ps1`, `build.ps1 -SkipDependencyDownload`, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`가 통과했습니다. 결과는 `VERSION_CONSISTENCY_OK`, `PASSED=27`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`입니다. [최종 PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30169678662)와 [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30169724770)는 경고=오류 `net48` 빌드, Portable·브리지 빌드, 전체 테스트와 자산 업로드까지 통과했습니다.
- [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30169793820)는 임시 RSA-3072/SHA-256 자체서명 인증서 생성·폐기, 설치 파일·Portable·브리지와 SHA-256 자산 7종 검증 및 이전 공개 런처 업데이트를 통과했습니다. 공개 자산을 새 임시 폴더에 다시 내려받아 `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, `PUBLIC_AUTO_UPDATE_OK=1.9.0->1.10.0`을 확인하고 업데이트된 공개 EXE로 전체 27개 테스트 그룹과 브리지 10개를 다시 통과했습니다.

## Codex README Feature Audit - 2026-07-26

- **Current Version**: 1.9.0 (build 26.2.45.73, 문서 전용 변경으로 버전 유지)
- **Branch**: `codex/readme-feature-audit-v1.9.0`
- **Status**: `README.md`의 한국어·영어 기능 목록을 현재 코드, v1.5.20~v1.9.0 변경 기록, 자동 테스트와 공개 v1.9.0 Release 자산에 대조해 수정하고 [PR #27](https://github.com/Mangom72/MineHarbor/pull/27) 게시 및 CI 검증 완료
- 더 이상 호출되지 않는 이전 콘텐츠 화면 기준의 `첫 화면 인기 콘텐츠`와 `선택 항목 아이콘` 설명을 제거했습니다. 현재 통합 화면의 빈 검색어 인기순 결과, 제작자·다운로드·설명, 로컬 JAR/ZIP 설치와 정확한 `<월드>/datapacks` 대상 및 데이터팩 검증 범위를 반영했습니다.
- 반복 간격·매일 시각·즉시 실행 일정, 콘텐츠 진행률·취소·종료 후 콜백 차단, 반응형 UI·접근성, 보조 창 클릭 관통/반복 클릭 차단과 상태별 런처 종료 확인을 한·영 기능 목록에 추가했습니다.
- 직접 SSDP/SOAP, Windows COM 백업, 최대 8개 대체 외부 포트와 보수적 외부 접속 판정을 명시하고 운영체제·런타임, Modrinth, 일정 실행, 브리지 지표, 외부 접속과 자체서명의 현재 지원 범위를 표로 분리했습니다.
- SDK 스타일 `net48`, 일반 CI와 릴리스 워크플로의 버전·자산·SHA-256·자체서명·이전 공개 런처 업데이트 검증을 설명하고 관련 콘텐츠/복사/빠른 명령/.NET/감사 문서 링크를 추가했습니다.
- README 로컬 링크 27개와 외부 링크 8종, 한국어/영어 앵커, UTF-8 BOM, 깨진 문자, `v1.9.0`/`26.2.45.73` 및 공개 최신 Release 7개 자산을 확인했습니다. Markdown 규칙 검사는 기존 중앙 정렬 HTML·긴 표 문장 스타일만 보고했으며 새 링크나 구조 오류는 없었습니다.
- `Prepare-BuildResources.ps1`, `build.ps1`, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`가 통과했습니다. 결과는 `VERSION_CONSISTENCY_OK`, `PASSED=27`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`입니다. 로컬에 없는 .NET SDK 검증은 [PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30163548740)에서 .NET 10 SDK의 경고=오류 `net48` 빌드, Portable·브리지 빌드, 전체 테스트와 자산 업로드까지 통과했습니다.

## Codex Version-aware Quick Commands and Risk Model - 2026-07-25

- **Current Version**: 1.9.0 (build 26.2.45.73)
- **Branch**: `codex/v1.9.0-release-state` (기능 브랜치: `codex/quick-command-safety-v1.9.0`)
- **Status**: 빠른 명령 확장, 선택 인수, Minecraft 버전 호환성 및 3단계 위험도 구현 후 [v1.9.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.9.0) 게시 및 공개 자동 업데이트 재검증 완료
- 플레이어·IP 차단 조회/변경, 시드·시간, 유휴 시간, 게임 규칙, 데이터팩, `reload`와 안전한 경험치·월드 경계·강제 로딩 조회를 추가해 기본 명령을 70개 이상으로 확장했습니다.
- `{player}` 필수 인수와 `[reason]` 선택 인수를 구분하며 마지막 사유·메시지·명령은 여러 단어를 받을 수 있습니다. 주소·시간 단위·분·비율·날씨·게임 규칙·데이터팩·함수·차원·거리 후보를 로컬 자동완성에 추가했습니다.
- 명령 정의에 최소·최대 Minecraft 버전을 추가했습니다. 데이터팩/강제 로딩은 1.13 이상, 수면 비율은 1.17 이상에서만 표시하고, 26.x에서 변경된 기존 `time query daytime`은 1.21.11까지만 표시합니다.
- 기존 `QuickCommandRisk`의 `Normal`·`Confirm`·`Dangerous`를 실제 목록 색상과 빠른 명령·직접 콘솔 확인에 연결했습니다. `reload`, 광범위 변경, `keepInventory false`, `doMobSpawning false`는 강한 위험 경고를 사용하고 읽기 전용 `worldborder get`·`forceload query`는 일반으로 유지합니다.
- 사용자 명령 편집기에 현대형 위험도 선택, 최소·최대 버전, 선택 인수 안내와 접근성 정보를 추가하고 기존 `Confirm` JSON을 자동 호환합니다.
- 최종 로컬 빌드와 전체 27개 테스트 그룹, Portable 실행·버전, 브리지 10개, 모던 대화상자 및 보안 회귀 검사가 통과했습니다. 임시 RSA-3072/SHA-256 자체서명으로 설치 파일·Portable·브리지·SHA-256 등 릴리스 산출물 7종을 검증한 뒤 인증서와 PFX를 제거했습니다.
- 기능 [PR #25](https://github.com/Mangom72/MineHarbor/pull/25), [PR CI](https://github.com/Mangom72/MineHarbor/actions/runs/30162595403)와 [v1.9.0 main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30162646922)가 통과했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30162698640)는 .NET SDK 빌드, 전체 테스트, 자체서명 생성·폐기, 설치 파일·Portable·브리지·SHA-256 검증과 이전 런처 업데이트 검증까지 완료했습니다.
- 공개 자산 7종을 새 임시 폴더에 다시 내려받아 자체서명과 SHA-256 목록을 독립 검증했습니다. 공개 v1.8.1 런처로 `PUBLIC_AUTO_UPDATE_OK=1.8.1->1.9.0`을 확인하고, 업데이트된 공개 실행 파일로 `PASSED=27`, `BRIDGE_PROTOCOL_PASSED=10`, Portable/UI/보안 검사를 다시 통과했습니다. 실제 사용자 서버·UPnP·공유기 설정과 현재 사용 중인 화면은 건드리지 않았습니다.

## Codex Tool-Window Close Release Guard Hotfix - 2026-07-25

- **Current Version**: 1.8.1 (build 26.2.45.72)
- **Branch**: `codex/v1.8.1-release-state` (핫픽스 브랜치: `codex/input-guard-v1.8.1`, 기능 브랜치: `codex/duplication-settings-v1.8.0`)
- **Status**: Paper/Purpur 복사 설정과 전체 UI 회귀 검사를 v1.8.0으로 배포한 뒤, 공개 자동 업데이트 검증 중 확인한 보조 창 닫기 해제 입력의 포인터 이동 의존성을 수정하여 [v1.8.1 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.8.1) 게시 및 공개 자동 업데이트 재검증 완료
- 닫기 누름과 짝인 마우스 해제는 포인터가 보호 반경 밖으로 이동해도 메인 창에 전달하지 않고, 이후 추가 누름만 기존 좌표 범위로 제한합니다. 컴퓨터를 사용하는 동안에도 검사가 실제 포인터 이동에 흔들리지 않도록 반경 밖 이동을 회귀 테스트에 명시했습니다.
- 서버별 설정 탭에 피스톤 기반(TNT·레일·양탄자), 엔드 포털 중력 블록, 철사 덫 갈고리 복사 동작을 각각 제어하는 현대식 체크박스와 접근성 설명을 추가했습니다.
- Paper/Purpur 1.19 이상은 `config/paper-global.yml`, 이전 버전은 `paper.yml`의 중첩된 `settings.unsupported-settings`를 사용합니다. 신형 키는 도입 버전 또는 실제 생성 여부를 확인하고, 직접 JAR·Spigot/Vanilla 및 비 Paper 계열에 호환되지 않는 키를 기록하지 않습니다.
- 기존 프로필은 새 설정을 한 번 저장하기 전까지 Paper YAML을 자동 변경하지 않습니다. 실제 변경 전 서버별 `.mineharbor/configuration-backups`에 최대 5개의 백업을 남기며, 손상·중복 키, 탭 들여쓰기, 심볼릭 링크 및 서버 폴더 밖 경로는 거부합니다.
- 설정 창의 146px 불필요한 공백을 제거하고 일반 창 크기에서는 스크롤이 나타나지 않도록 재배치했습니다. 전체 22개 Form의 DPI 설정과 기본 Button·ComboBox·CheckBox·TrackBar·스크롤바 사용 금지를 자동 검사합니다.
- 로컬 빌드와 27개 테스트 그룹, 같은 런처 테스트 3회 반복, Portable 실행·버전, 명령 브리지 10개, 현대식 대화상자 및 보안 회귀 검사가 통과했습니다. 자체서명 릴리스 산출물 7종을 검증하고 임시 인증서와 PFX가 제거되었음을 확인했습니다.
- 기능 [PR #22](https://github.com/Mangom72/MineHarbor/pull/22)와 핫픽스 [PR #23](https://github.com/Mangom72/MineHarbor/pull/23), 각각의 PR CI 및 [v1.8.1 main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30159893607)가 통과했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30159940514)는 .NET SDK 빌드, 전체 테스트, 자체서명 생성·폐기, 설치 파일·Portable·브리지·SHA-256 검증과 공개 자산 검증까지 완료했습니다.
- 공개 v1.8.0 런처로 `PUBLIC_AUTO_UPDATE_OK=1.8.0->1.8.1`을 확인하고, 업데이트된 공개 실행 파일로 `PASSED=27`, `BRIDGE_PROTOCOL_PASSED=10`, Portable/UI/보안 검사를 다시 통과했습니다. 실제 사용자 서버·UPnP·공유기 설정과 현재 사용 중인 화면은 건드리지 않았습니다.

## Codex Fixed Quick-Command and Console Workspace - 2026-07-25

- **Current Version**: 1.7.5 (build 26.2.45.70)
- **Branch**: `codex/v1.7.5-release-state` (기능 브랜치: `codex/fix-quick-command-layout-v1.7.5`)
- **Status**: 빠른 명령 위치 복원과 콘솔 겹침 수정을 구현하고 [v1.7.5 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.5) 게시 및 공개 자동 업데이트 검증 완료
- v1.7.3에서 도입한 콘솔 닫힘 전체 폭·열림 오른쪽 보조 패널 전환을 제거하고, 빠른 명령을 구버전과 같은 오른쪽 410px 고정 열에 유지합니다.
- 작업 영역을 명시적인 2열 `TableLayoutPanel`로 바꿔 콘솔은 왼쪽 열, 빠른 명령은 오른쪽 열만 사용합니다. 기존에는 실제 창에서 콘솔과 빠른 명령 경계가 겹쳐 콘솔 오른쪽이 가려졌으나 수정 후 경계가 교차하지 않습니다.
- 고정 열 위치·폭, 콘솔 전환 후 위치 유지, 별도 열과 실제 경계 비중첩을 26번째 UX 테스트 그룹에 추가했습니다. 로컬 빌드와 `PASSED=26`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`, 자체서명 릴리스 산출물 7종 검증을 통과했습니다.
- 실제 Windows 다크 화면에서 콘솔 닫힘 → 열림 → 닫힘을 확인했습니다. 빠른 명령은 오른쪽 고정 위치를 유지하고 콘솔 출력·도구막대·명령 입력은 왼쪽에서 가려지지 않았습니다. 서버 시작·명령 전송·UPnP는 실행하지 않았고 기존 설치본 창을 검증 후 복원했습니다.
- [PR #20](https://github.com/Mangom72/MineHarbor/pull/20)은 `c6bbd29`로 병합됐고 PR CI와 [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30119364755)가 모두 통과했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30119473148)는 자체서명 인증서 생성·폐기, 산출물 7종과 SHA-256, 설치 파일·Portable·브리지 검증을 완료했습니다.
- 공개 v1.7.4 런처로 v1.7.5 자동 업데이트를 실행해 `PUBLIC_AUTO_UPDATE_OK=1.7.4->1.7.5`를 확인한 뒤, 업데이트된 실행 파일로 전체 `PASSED=26`, `BRIDGE_PROTOCOL_PASSED=10`, Portable/UI/보안 검사를 다시 통과했습니다.

## Codex Repeated Close-Click Guard and Exit Confirmation - 2026-07-25

- **Current Version**: 1.7.4 (build 26.2.45.69)
- **Branch**: `codex/v1.7.4-release-state` (기능 브랜치: `codex/fix-close-confirmation-v1.7.4`)
- **Status**: 보조 창 추가 클릭 방어와 상태별 런처 종료 확인을 구현하고 [v1.7.4 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.4) 게시 및 공개 자동 업데이트 검증 완료
- 보조 창 X의 화면 좌표를 보존하고 64px 주변에서 Windows 더블클릭 시간에 150ms 여유를 더한 구간 동안 추가 마우스 입력을 소비합니다. 보호 시간은 500~1200ms로 제한하며, 포인터가 X 주변을 벗어나면 다른 메인 창 동작을 즉시 사용할 수 있습니다.
- 기존 `FormClosed`·마우스 해제 직후 `BeginInvoke`로 보호를 해제하던 경쟁 조건을 제거했습니다. 단일 클릭뿐 아니라 마우스 스위치 채터링과 지연된 두 번째 클릭도 같은 닫기 입력으로 처리합니다.
- 런처 X와 Alt+F4는 서버가 꺼져 있어도 종료 여부를 확인합니다. 유휴 상태의 확인은 서버 종료 프로세스를 실행하지 않으며, 작업 중에는 완료 후 종료, 서버 실행 중에는 월드 저장과 안전 종료 후 런처 종료를 안내합니다.
- 클릭 보호 시간·좌표 경계·반복 입력·만료와 유휴/작업/서버 종료 질문 분기를 회귀 검사에 추가했습니다. 로컬 Portable 빌드와 `PASSED=26`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`를 통과했고, 실제 Windows에서 한국어·다크 일반 종료 확인창을 확인했습니다.
- [PR #18](https://github.com/Mangom72/MineHarbor/pull/18)은 `d01c183`으로 병합됐고 PR CI와 `main` CI가 모두 통과했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30112841129)는 자체서명 인증서 생성·폐기, 산출물 7종과 SHA-256, 설치 파일·Portable·브리지 검증을 완료했습니다.
- 공개 v1.7.3 런처로 v1.7.4 자동 업데이트를 실행해 `PUBLIC_AUTO_UPDATE_OK=1.7.3->1.7.4`를 확인한 뒤, 업데이트된 실행 파일로 전체 `PASSED=26`, `BRIDGE_PROTOCOL_PASSED=10`, Portable/UI/보안 검사를 다시 통과했습니다.

## Codex Responsive Workspace and Command Completion - 2026-07-24

- **Current Version**: 1.7.3 (build 26.2.45.68)
- **Branch**: `codex/v1.7.3-release-state` (기능 브랜치: `codex/ux-audit-v1.7.3`)
- **Status**: 반응형 빠른 명령·콘솔 탐색·자동완성 UX 수정, PR #16 병합 및 `v1.7.3` 정식 릴리스 게시 완료
- 콘솔 닫힘 시 빠른 명령이 전체 작업 영역을 사용하고, 콘솔 열림 시 360~460 논리 픽셀의 읽을 수 있는 보조 패널로 전환됩니다. 영어 상태·안내·관리 버튼과 런처 업데이트 버튼의 말줄임표를 제거했습니다.
- CI의 글꼴 측정 차이에서 좁은 동반 패널의 영어 버튼 잘림이 발견되어, 공간이 부족하면 두 동작을 전체 폭의 세로 버튼으로 전환하고 겹침 회귀 검사를 추가했습니다.
- 콘솔 도구막대를 검색 → 로그 분류 → 줄 바꿈 순서로 재배치하고 양언어 폭을 보장했습니다. 시작 준비가 끝나면 기본 서버 시작 동작으로 키보드 포커스를 이동합니다.
- 메인 콘솔에 기본 명령·연결 플레이어 인수, 예약 명령 편집에 실행 시점과 무관한 기본 명령 자동완성을 추가하고 기존 방향키·Tab·Enter·Esc 및 스크린 리더 흐름을 재사용했습니다.
- Windows 11 실제 125% DPI에서 한국어·영어, 다크·라이트, 콘솔 닫힘·열림을 확인하고 한국어·다크 설정으로 복원했습니다. 제한 없는 문구 폭, Dock 전환, 콘솔 순서와 자동완성 연결을 검사하는 26번째 테스트 그룹을 추가했습니다.
- 로컬 검증은 고정 리소스 4종, Portable 빌드, `VERSION_CONSISTENCY_OK`, `PASSED=26`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`와 임시 자체서명 릴리스 자산 7종 검증을 통과했습니다. UPnP는 루프백 가짜 장치와 가짜 COM만 사용했습니다.
- GitHub PR [#16](https://github.com/Mangom72/MineHarbor/pull/16)을 병합 커밋 `4ac8500`으로 `main`에 병합했습니다. [main CI](https://github.com/Mangom72/MineHarbor/actions/runs/30038195121)와 [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/30038232632)는 .NET 10 SDK의 `net48` 빌드, 기존 Portable 빌드, 26개 테스트 그룹, 자체서명 Portable·설치 파일, 인증서 정리, SHA-256과 공개 자산 검증을 모두 통과하고 [v1.7.3 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.3)를 게시했습니다.
- 공개 자산 7종을 별도로 다시 내려받아 자체서명 주체·해시·크기·버전·업데이트 URL을 확인했습니다. 공개 v1.7.2 런처의 실제 업데이트 파서와 다운로드 루틴이 `PUBLIC_AUTO_UPDATE_OK=1.7.2->1.7.3`을 통과했고, 업데이트본의 전체 26개 런처 테스트, Portable smoke/version, 브리지 10개, UI·보안 검사도 다시 통과했습니다.

## Codex Tool Window Close Isolation and Self-Signed Release - 2026-07-23

- **Current Version**: 1.7.2 (build 26.2.45.67)
- **Branch**: `codex/v1.7.2-release-state` (기능 브랜치: `codex/close-click-guard-v1.7.2`)
- **Status**: 제목 표시줄 X 클릭 관통 방지와 자체서명 전용 릴리스 구현, PR #14 병합 및 `v1.7.2` 정식 릴리스 게시 완료
- 모델리스 도구 창의 `WM_NCLBUTTONDOWN/HTCLOSE`를 `FormClosing`보다 먼저 감지하고, 닫기 입력이 끝날 때까지 주 창의 `WM_MOUSEACTIVATE`를 `MA_NOACTIVATEANDEAT`로 거부합니다. 클라이언트·비클라이언트·X 버튼·포인터 누름/해제를 함께 소비하고, 창 종료와 입력 해제가 모두 확인되면 다음 독립 클릭을 즉시 허용하며 750ms 제한 시간으로 누락 메시지를 복구합니다.
- 릴리스 워크플로와 로컬 릴리스 도구는 매 실행마다 난수 비밀번호의 임시 RSA-3072/SHA-256 자체서명 인증서를 생성해 Portable EXE와 설치 프로그램을 서명합니다. 외부 인증서 비밀은 사용하지 않으며 PFX와 CurrentUser 인증서 저장소 항목을 `finally`/`always()` 경로에서 삭제합니다.
- `Test-ReleaseArtifacts.ps1 -RequireSelfSignedSignature`는 Portable/호환 별칭/설치 프로그램의 동일 자체서명 주체와 Authenticode 무결성을 검사합니다. 별도 서명 테스트는 변조본을 `NotSigned`/`HashMismatch`로 거부하고 인증서·PFX 잔여물이 없는지 확인합니다. 자체서명은 공개 배포자 신뢰나 SmartScreen 평판을 제공하지 않는다는 안내를 README, SECURITY, CONTRIBUTING과 릴리스 노트에 동기화했습니다.
- 현재 로컬 검증: 고정 리소스 4종 해시 확인, 기존 Portable 빌드, `VERSION_CONSISTENCY_OK`, `PASSED=25`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`, `SELF_SIGNED_RELEASE_SIGNING_OK=UnknownError`, 변조 거부, 인증서 정리 및 자체서명 설치 산출물 7종 검증을 통과했습니다. 로컬에는 .NET SDK가 없어 SDK 병행 빌드는 GitHub PR/릴리스 CI에서 확인합니다. UPnP 검증은 루프백 가짜 장치와 가짜 COM만 사용했습니다.
- GitHub PR [#14](https://github.com/Mangom72/MineHarbor/pull/14)를 병합 커밋 `823e2ca`로 `main`에 병합했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/29945953541)는 .NET 10 SDK `net48` 빌드, Portable/브리지와 25개 테스트 그룹, 자체서명 Portable·설치 파일, 인증서 정리, SHA-256과 공개 자산 검증을 모두 통과하고 [v1.7.2 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.2)를 게시했습니다.
- 공개 자산 7종을 별도로 다시 내려받아 자체서명 주체·해시·크기·버전·업데이트 URL을 확인했습니다. 공개 v1.7.1 런처의 실제 업데이트 파서와 다운로드 루틴이 `PUBLIC_AUTO_UPDATE_OK=1.7.1->1.7.2`를 통과했고, 업데이트본의 서명 주체와 전체 25개 런처 테스트, Portable smoke/version, 브리지 10개, UI·보안 검사도 다시 통과했습니다.

## Codex Secondary Window Dark Theme - 2026-07-22

- **Current Version**: 1.7.1 (build 26.2.45.66)
- **Branch**: `codex/v1.7.1-release-state` (기능 브랜치: `codex/dark-window-theme-v1.7.1`)
- **Status**: 보조 창·콘솔·네이티브 컨트롤의 다크 테마 수정, PR #12 병합 및 `v1.7.1` 정식 릴리스 게시 완료
- 보조 창의 공통 테마 경로가 클라이언트 배경만 바꾸고 Windows 제목 표시줄·테두리를 갱신하지 않던 문제를 수정했습니다. Windows 11 DWM 다크 모드와 색상 특성을 현재 팔레트에 연결하고 핸들 재생성 시 다시 적용합니다.
- RichTextBox, ListBox, CheckedListBox, ListView, TreeView, NumericUpDown, ComboBox와 DataGridView의 네이티브 테마·표면·헤더·선택 색을 통합했습니다. 핸들이 늦게 생성되는 컨트롤도 HandleCreated에서 원하는 테마를 적용합니다.
- 메인 콘솔 검색·명령 입력은 둥근 입력 표면으로 바꾸고 관리 콘솔의 기본 외곽선과 도구막대·명령 영역 뒤로 겹치던 출력 Dock 순서를 수정했습니다. 빠른 명령 관리의 기본 GroupBox는 테마 팔레트로 직접 그리는 ModernGroupBox로 교체했습니다.
- 다크 보조 창의 배경, 리치 텍스트·목록·체크 목록, 입력 표면·테두리, 현대형 그룹 표면·테두리, DataGridView 헤더와 외곽선을 실제 팔레트 값으로 검증하는 회귀 테스트를 추가했습니다.
- 현재 로컬 검증: .NET SDK `net48` Release 빌드 경고 0/오류 0, `VERSION_CONSISTENCY_OK`, `PASSED=25`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`. UPnP 검증은 루프백 가짜 장치와 가짜 COM만 사용했습니다.
- GitHub PR [#12](https://github.com/Mangom72/MineHarbor/pull/12)를 병합 커밋 `2f68137`로 `main`에 병합했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/29862987941)는 SDK/Portable 빌드, 25개 테스트 그룹, 설치 파일, SHA-256 및 자산 검증을 통과하고 [v1.7.1 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.1)를 게시했습니다. 코드 서명 비밀이 없어 서명 단계만 건너뛰었습니다.
- 공개 자산 7종을 별도로 다시 내려받아 `RELEASE_ARTIFACTS_PASSED=7`과 공개 자산 모드를 확인했습니다. 공개 v1.7.0 런처의 실제 업데이트 파서와 다운로드 루틴이 v1.7.1을 탐지·다운로드해 `PUBLIC_AUTO_UPDATE_OK=1.7.0->1.7.1`을 통과했으며, 그 다운로드본도 25개 런처 테스트, Portable smoke/version, 브리지 10개, UI·보안 검사를 모두 통과했습니다.

## Codex Modern UI, Autocomplete, and Port Status - 2026-07-20

- **Current Version**: 1.7.0 (build 26.2.45.65)
- **Branch**: `codex/v1.7.0-release-verification` (기능 브랜치: `codex/modern-ui-autocomplete-port-status-v1.7.0`)
- **Status**: 현대형 공통 컨트롤·자동완성·보수적 외부 포트 상태·휴지통/창 닫기 UX 구현, PR #10 병합 및 `v1.7.0` 정식 릴리스 게시 완료
- 다크·라이트 팔레트를 공유하는 체크박스, 닫힌 면을 직접 그리는 둥근 드롭다운, pill 탭, 목록 헤더·행, 입력 표면과 상태 표 구분선을 추가했습니다. 대시보드의 고정 격자와 불필요한 항상 스크롤을 제거했고 작은 설정 창의 세로 스크롤은 콘텐츠가 넘칠 때만 유지합니다.
- 플레이어 관리에는 연결된 플레이어 이름 자동완성, 멀티 서버 콘솔에는 기본 명령과 플레이어 인수 자동완성을 추가했습니다. 방향키, Tab, Enter, Esc 및 스크린 리더 설명을 제공합니다.
- 일반 외부 TCP 검사는 Minecraft 서버의 정체를 확인할 수 없으므로 `서버 일치 미확인`으로 분리합니다. MineHarbor가 만든 UPnP 매핑의 사후 검사만 `확인됨`으로 표시하며 검사 요청의 캐시를 막습니다. 기존 직접 SSDP/SOAP, COM 백업, 8개 대체 포트와 정확한 소유권 정리는 유지합니다.
- 서버 이름 입력은 휴지통으로 보내는 단계에만 유지하고 연한 서버명 예시를 표시합니다. 휴지통 영구 삭제는 3초 잠금 확인으로 바꿨으며, 모델리스 도구 창의 X 닫기 클릭이 뒤쪽 메인 창 버튼으로 전달되지 않도록 메시지 필터 보호를 추가했습니다.
- 격리 렌더링에서 한국어 플레이어 관리, 영구 삭제 확인, 현대형 드롭다운·체크박스·탭을 확인했습니다. 로컬 검증은 `Prepare-BuildResources.ps1`, 기존 Portable 빌드, .NET SDK 10.0.302의 경고=오류 `net48` 빌드, 25개 런처 테스트 그룹, Portable smoke/version, 10개 브리지 프로토콜, UI·보안 정적 검사를 통과했습니다. 모든 UPnP 시험은 루프백 가짜 장치와 가짜 COM만 사용했습니다.
- GitHub PR [#10](https://github.com/Mangom72/MineHarbor/pull/10)을 병합 커밋 `0018251`로 `main`에 병합했습니다. [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/29756668524)는 SDK/Portable 빌드, 25개 테스트 그룹, 설치 파일, SHA-256 및 자산 검증을 통과하고 [v1.7.0 정식 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.7.0)를 게시했습니다. 코드 서명 비밀이 없어 서명 단계만 건너뛰었습니다.
- 공개 자산 7종을 다시 내려받아 해시·크기·버전·자동 업데이트 URL을 검증했고, 공개 v1.6.0 런처의 실제 메타데이터 파서와 다운로드 루틴이 v1.7.0을 감지·다운로드하여 크기·SHA-256·Windows 버전 리소스를 확인했습니다. 공개 EXE와 자동 업데이트로 받은 EXE 모두 25개 테스트 그룹, Portable smoke/version, 브리지 10개, UI·보안 검사를 통과했습니다.
- 공개 검증 스크립트가 배포하지 않는 내부 `release-notes.md`를 요구하던 문제를 수정하고, 앞으로 릴리스 워크플로가 게시 직후 공개 자산을 다시 내려받아 직전 정식 버전의 실제 자동 업데이트 경로와 전체 회귀 테스트를 자동 실행하도록 후속 검증 단계를 추가했습니다.

## Codex Content, Automation, and Dashboard - 2026-07-20

- **Current Version**: 1.6.0 (build 26.2.45.64)
- **Branch**: `codex/v1.6.0-release-state`
- **Status**: 콘텐츠 관리·서버 자동화·상태 대시보드 구현, PR #8 병합 및 `v1.6.0` 정식 릴리스 게시 완료
- 서버별 `.mineharbor/content-manifest.json`을 도입해 MineHarbor 관리 파일과 수동 설치 파일을 구분합니다. 플러그인·모드·데이터팩 검색, 호환성 검사, 필수 의존성 처리, 해시 검증, 설치·업데이트·일괄 업데이트·활성화 전환·복구 가능한 제거를 지원합니다.
- 데이터팩은 Vanilla, Paper, Purpur와 직접 JAR 프로필에서 월드별 `datapacks` 폴더를 대상으로 관리합니다. ZIP 루트의 `pack.mcmeta`, `pack_format`, 경로 이탈·중복, 항목 수와 해제 크기를 설치 전에 검사합니다.
- 서버별 `.mineharbor/automation.json`에 시작 전·종료 후 백업, 정기 백업, 예약 시작·종료·재시작·명령, 플레이어 사전 공지, 최근 결과와 다음 실행, 개수·기간·용량 보존 정책을 저장합니다. 프로세스 ID와 시작 시각을 포함한 디스크 실행 임대로 중복 실행과 비정상 종료 후 잠금 잔존을 처리합니다.
- 통합 서버 대시보드는 상태·가동 시간·CPU·메모리·Java·플레이어·서버/월드/백업 크기·최근 경고/오류·외부 접속·다음 작업을 보여 줍니다. Paper/Purpur 브리지는 TPS/MSPT를 주기적으로 전송하고, 얻을 수 없는 값은 추정하지 않고 지원되지 않음으로 표시합니다.
- 콘텐츠·일정·백업·대시보드 화면을 서버 관리에서 분리하고 다크/라이트 테마, DPI 재배치, 키보드 접근, 접근성 이름, 진행률과 취소를 적용했습니다. 폼 종료 후 비동기 UI 콜백은 취소 토큰과 폐기 상태 검사로 차단합니다.
- SDK 스타일 `net48` 프로젝트를 추가해 기존 Portable 빌드와 병행 검증하도록 했습니다. 기본 런타임의 .NET 10 전환은 프레임워크 전용 JSON API, WinForms/COM, 단일 EXE 업데이트 및 설치 호환성 검증이 끝날 때까지 보류합니다.
- PR 및 `main` push용 일반 CI를 릴리스 워크플로와 분리했습니다. 버전·문서·소스 목록 동기화, SDK/기존 빌드, 실행·실패 경로·UI·브리지 테스트, Portable 메타데이터를 검사하며 모든 외부 Action을 전체 커밋 SHA로 고정했습니다.
- 최종 로컬 검증: 릴리스 산출물 7종 구조·SHA-256 통과, `VERSION_CONSISTENCY_OK`, `PASSED=24`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`. UPnP 검증은 루프백 가짜 장치와 가짜 COM만 사용했고 실제 공유기 설정은 변경하지 않았습니다.
- GitHub PR [#8](https://github.com/Mangom72/MineHarbor/pull/8)을 병합 커밋 `7e342b0`으로 `main`에 병합했습니다. 태그 `v1.6.0`의 [릴리스 워크플로](https://github.com/Mangom72/MineHarbor/actions/runs/29733089559)에서 빈 캐시 의존성 검증, .NET 10 SDK의 `net48` 빌드, 기존 Portable 빌드·테스트, 설치 파일 생성, 산출물 검증과 정식 릴리스 게시가 모두 통과했습니다. Paper Maven의 UI 리디렉션 문제는 공식 Artifactory 원본 URL과 기존 SHA-256을 함께 고정해 수정했습니다.
- 공개 [v1.6.0 릴리스](https://github.com/Mangom72/MineHarbor/releases/tag/v1.6.0)의 7개 자산을 다시 내려받아 `SHA256SUMS.txt`와 `update.json`의 크기·SHA-256·URL을 대조했습니다. 공개 EXE로 전체 24개 테스트 그룹과 Portable·브리지·UI·보안 검증을 다시 통과했으며, 공개 `v1.5.23` 런처의 실제 자동 업데이트 루틴이 `v1.6.0`을 탐지하고 EXE를 내려받아 버전·크기·SHA-256을 검증했습니다.
- 남은 검증은 실제 DPI/다중 모니터·제조사별 서버 설치 환경, Fabric/Forge용 TPS 브리지, Windows 코드 서명입니다.

## Codex Full Product Audit and Release - 2026-07-20

- **Current Version**: 1.5.23 (build 26.2.45.63)
- **Branch**: `codex/v1.5.23-release-state`
- **Status**: UPnP·자동 업데이트·UI/UX·접근성·네트워크·공급망 전제품 감사 수정 및 `v1.5.23` 정식 릴리스 게시 완료
- 이전 릴리스의 직접 SSDP/SOAP 기본 경로, NATUPnP COM 백업, 8개 대체 외부 포트, 세대·취소·정확한 소유권 정리를 재검증했습니다. 테스트는 루프백 가짜 공유기와 가짜 COM만 사용하며 이번 작업에서 실제 공유기 매핑을 만들지 않았습니다.
- GitHub 릴리스 별칭에서 정식 저장소와 CDN으로 이어지는 자동 업데이트·명령 브리지 리디렉션을 검증된 수동 경로로 수정했습니다. 메타데이터·파일 크기, 저장소·버전·파일명, 최종 SHA-256을 확인하며 공개 `v1.5.23` 브리지 자산 다운로드도 통과했습니다.
- Modrinth API/CDN과 아이콘 변환 응답의 리디렉션·크기·디코딩 픽셀 수를 제한하고, 아이콘 프록시 개인정보 안내를 한국어·영어로 갱신했습니다.
- 영어 업데이트 노트, 보조 창 DPI/버튼 배치, 모델리스 타이머 해제, 스크린 리더 이름과 빠른 명령 영문 문구를 수정했습니다. 격리 프로필로 주요 Windows 화면, 한국어·영어와 다크·라이트 테마를 실제 확인했습니다.
- GitHub Actions를 전체 SHA로 고정하고 체크아웃 자격 증명 저장을 끄며 서명 비밀의 범위와 임시 PFX 정리를 강화했습니다. 버전 도구의 경로·모드·인코딩 검증과 CI 정적 회귀 조건을 추가했습니다.
- 깨진 과거 `CHANGELOG.md` 인코딩을 읽을 수 있는 이중 언어 요약으로 복구하고 오래된 일회성 계획·우회 스크립트를 제거했습니다.
- 최종 검증: `PASSED=22`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=8`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK`, 릴리스 자산 7종 구조·해시 검증을 통과했습니다. GitHub Actions 실행 `29700540478`의 빌드·테스트·설치 파일·게시 단계가 모두 성공했으며, 코드 서명 비밀이 없어 서명 단계만 건너뛰었습니다.
- 공개 `v1.5.23` 자산을 다시 내려받아 `SHA256SUMS.txt`와 `update.json`의 실행 파일·브리지 크기 및 SHA-256을 대조했습니다. 공개 `v1.5.22`와 `v1.5.20` 런처의 실제 자동 업데이트 루틴이 `v1.5.23`을 탐지하고 새 실행 파일을 다운로드·검증하는 호환성 시험도 통과했습니다.

## Codex UPnP Lifecycle/Fallback Remediation - 2026-07-20

- **Current Version**: 1.5.22 (build 26.2.45.62)
- **Branch**: `codex/fix-auto-update-v1.5.22`
- **Status**: UPnP 수명 주기·대체 경로 보강과 정식 GitHub 자동 업데이트 릴리스 게시
- 비활성 상태였던 직접 SSDP/SOAP 매핑을 기본 경로로 구현하고, 실패할 때 Windows NATUPnP COM을 보조 경로로 사용합니다. 서비스 검색은 인터페이스별 내부 IP를 보존하며 응답 수·XML 크기·HTTP 시간·안전한 라우터 주소를 제한합니다.
- 기본 외부 포트가 다른 매핑과 충돌하면 최대 8개의 대체 외부 포트를 순서대로 시도하고, 실제 선택된 외부 포트를 상태·접속 주소·재검사·정리에 일관되게 사용합니다. 사용자가 만든 기존 매핑은 소유하지 않으며 삭제하지 않습니다.
- 서버 중단 토큰과 직접 SOAP 호출을 연결하고, 이전 실행의 지연 정리가 새 실행의 매핑을 삭제하지 않도록 실행 세대 검사를 추가했습니다. 종료되지 않은 COM 정리 작업이 있으면 새 매핑 시작을 차단하여 반복 온·오프 경합을 방지합니다.
- 직접 경로는 정확한 라우터 상태 조회 후에만 소유 매핑을 삭제하며, 추가 응답이 유실된 경우에도 라우터의 실제 상태를 재조회해 성공을 복구합니다. 취소 중 남은 소유 기록은 다음 실행의 안전한 복구 대상으로 표시합니다.
- 회귀 검증에는 직접 SOAP TCP/UDP 8회 반복 생성·삭제, COM 12회 반복 생성·삭제, 기본 포트 충돌과 대체 포트, 응답 유실, 요청 중 취소, 사용자 매핑 보존, 이전 실행 지연 정리 경합이 포함됩니다. 테스트는 루프백 가짜 공유기만 사용하도록 고정했습니다.
- 검증 결과: `Prepare-BuildResources.ps1` 성공, `build.ps1` 성공, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`에서 `PASSED=21`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=8`, `MODERN_DIALOG_SCAN_OK`, `SECURITY_REGRESSION_SCAN_OK` 통과.
- 런처가 조회하는 최신 릴리스 API는 정식 `Mangom72/MineHarbor` 저장소를 사용합니다. `update.json`의 실행 파일·브리지 URL은 `v1.5.20` 이하의 자동 업데이트를 위해 GitHub가 유지하는 이전 저장소 별칭을 사용하며, 새 런처는 정식·별칭 경로를 모두 검증합니다.
- 테스트 개발 중 실제 공유기에 정확히 한 개의 격리된 `25602/TCP -> 127.0.0.1:25565` 시험 매핑이 생성되었습니다. 즉시 설명·프로토콜·외부/내부 포트를 재검증한 뒤 해당 항목만 삭제했으며 부재를 다시 확인했습니다. 이후 시험은 실제 SSDP 탐색을 호출하지 않습니다.
- 남은 검증은 제조사·펌웨어별 공유기, 이중 NAT/CGNAT, NAT-PMP/PCP 전용 장치에 대한 격리 장치 행렬입니다. NAT-PMP/PCP는 현재 구현 범위에 포함하지 않았습니다.

## Codex Security Remediation - 2026-07-18

- **Current Version**: 1.5.20 (build 26.2.45.60)
- **Branch**: `codex/release-v1.5.20`
- **Status**: 감사 수정본을 정식 패치 릴리스로 게시하기 위한 버전·변경 기록 및 태그 준비
- UPnP 매핑을 실행별로 기록하고 현재 내부 IP·포트·프로토콜·설명이 정확히 일치할 때만 삭제합니다. 살아 있는 다른 MineHarbor 프로세스의 기록은 보존합니다.
- TCP/UDP 부분 성공, 외부 검사 실패, 중복 재검사, CGNAT dual-stack 비교, 가상 어댑터와 방화벽 규칙 판정을 보수적으로 처리합니다.
- Forge/Inno Setup 다운로드, 백업 복원, 직접 콘솔 위험 명령, 로컬 코드 서명 경로를 강화했습니다.
- 기준 검증: `build.ps1 -SkipDependencyDownload` 성공, `test.ps1 -LauncherPath artifacts\MineHarbor.exe`에서 `PASSED=21`, `BRIDGE_PROTOCOL_PASSED=8`, 버전·smoke·modern-dialog scan 통과.
- 실제 공유기 COM 무응답을 프로세스 내부에서 강제로 중단하는 것은 안전하지 않으므로 호출자는 제한 시간 후 복구 기록을 남기며, 제조사별 장치 시험은 별도 격리 환경에서 수행해야 합니다.

## Current Session Status
- **Agent Name**: Antigravity
- **Last Updated**: 2026-07-16T07:18:00Z
- **Current Version**: 1.5.17 (build 26.2.45.57)
- **Status**: Stable / Released

## Recent Accomplishments
1. **DWM Title Bar Cohesion (Windows 11)**:
   - Integrated TitleBarDwm using dwmapi.dll (DWMWA_CAPTION_COLOR, DWMWA_TEXT_COLOR, DWMWA_BORDER_COLOR) to seamlessly blend the native OS title bar with the application's internal dark/light theme on Windows 11 (Build >= 22000).
   - Preserved FormBorderStyle.Sizable, maintaining 100% of native Windows features like Snap Layouts, multi-monitor DPI scaling, and drop shadows.
   - Handled graceful fallback to default OS title bar for Windows 10 without throwing exceptions.
2. **Handle Recreation Hotfix (v1.5.17)**:
   - Fixed an issue where the C# Form.OnHandleCreated method could not be overridden properly due to internal visibility modifier constraints.
   - Switched to subscribing to the HandleCreated event inside the form constructors (LauncherForm and ServerSetupForm).
   - Ensures DWM color states are preserved even when the window handle is forcefully recreated by the WinForms engine (e.g. ShowInTaskbar updates).
3. **Release v1.5.17**:
   - Bumped version to 1.5.17.
   - Updated CHANGELOG.md with multi-language hotfix notes.
   - Successfully ran Publish-LocalRelease.ps1 and pushed the new artifacts to GitHub Releases.

## Active Issues / Next Steps
- **Monitoring**: Everything is currently stable. Monitor for any reports of unhandled exceptions from dwmapi.dll calls on unusual Windows versions.

## Workspace Context
- **Encoding Rule**: All PowerShell .ps1, C# .cs, and .md files must be saved with **UTF-8 with BOM** to avoid build failures with the local csc.exe and signtool.
- **Certificates**: Local signing throws warnings in PS but still signs successfully.
