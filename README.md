<div align="center">
  <img src="launcher-icon.ico" width="96" height="96" alt="잔디 블록 모양의 MineHarbor 아이콘">
  <h1>MineHarbor — Minecraft Server Launcher</h1>
  <p><strong>복잡한 Java와 네트워크 설정은 줄이고, Windows에서 마인크래프트 서버를 바로 시작하세요.</strong></p>
  <p>서버 생성부터 업데이트, 백업, 콘텐츠, 플레이어, 외부 접속까지 한곳에서 관리하는 데스크톱 런처입니다.</p>

  <p>
    <a href="https://github.com/Mangom72/MineHarbor/releases/latest"><img src="https://img.shields.io/github/v/release/Mangom72/MineHarbor?display_name=tag&amp;sort=semver&amp;style=flat-square&amp;color=3182F6" alt="최신 GitHub Release 버전"></a>
    <a href="https://github.com/Mangom72/MineHarbor/actions/workflows/build-release.yml"><img src="https://github.com/Mangom72/MineHarbor/actions/workflows/build-release.yml/badge.svg" alt="빌드 및 릴리스 워크플로 상태"></a>
    <a href="LICENSE"><img src="https://img.shields.io/github/license/Mangom72/MineHarbor?style=flat-square&amp;color=20C997" alt="MIT 라이선스"></a>
    <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=flat-square&amp;logo=windows11&amp;logoColor=white" alt="Windows 10 및 Windows 11 지원">
  </p>

  <p><a href="#한국어">한국어</a> · <a href="#english">English</a></p>
</div>

---

## 한국어

### 다운로드

| 방식 | 이런 경우에 추천합니다 | 받기 |
| --- | --- | --- |
| **Portable EXE** | 설치 없이 파일 하나로 바로 실행 | **[고정 링크로 다운로드](https://github.com/Mangom72/MineHarbor/releases/latest/download/MineHarbor.exe)** |
| **Windows 설치 프로그램** | 시작 메뉴, 선택적 바탕화면 바로가기, 제거 기능 사용 | **[최신 Release 열기](https://github.com/Mangom72/MineHarbor/releases/latest)** |
| **Portable ZIP** | README와 라이선스를 포함한 묶음 보관 | **[최신 Release 열기](https://github.com/Mangom72/MineHarbor/releases/latest)** |

현재 소스 버전은 `v1.12.0`, 내부 빌드는 `26.2.45.76`입니다. MineHarbor 이름으로 배포된 Portable EXE는 같은 링크에서 계속 최신 파일을 받을 수 있습니다. 기존 설치의 `%LOCALAPPDATA%\MinecraftServerLauncher` 데이터는 자동으로 찾아 그대로 사용하며, 새 사용자 데이터 경로는 `%LOCALAPPDATA%\MineHarbor`입니다.

이 README는 로드맵이 아니라 현재 `v1.12.0` 소스와 자동 테스트, 공개 Release 자산에서 확인한 기능만 설명합니다. 서버 종류나 브리지 연결처럼 조건에 따라 달라지는 기능과 지원되지 않는 상태는 아래에 따로 표시합니다.

> [!WARNING]
> 현재 릴리스 실행 파일은 요청된 자체서명 인증서로 무결성을 표시하지만 공개 인증 기관이 신뢰한 배포자 서명은 아닙니다. 따라서 Windows SmartScreen 경고가 나타날 수 있습니다. Release의 `SHA256SUMS.txt`와 GitHub 출처를 함께 확인해 주세요.

> [!TIP]
> Paper/Purpur 실시간 명령 브리지는 앱의 `명령·브리지 관리`에서 서버별로 설치할 수 있습니다. 수동 설치 파일과 `SHA256SUMS.txt`는 최신 Release에 함께 제공됩니다.

### 왜 이 런처인가요?

| | |
| --- | --- |
| **빠른 시작**<br>Java, 서버 JAR, 기본 설정을 순서대로 준비합니다. | **버전 호환**<br>서버 종류와 Minecraft 버전에 맞는 Java를 선택합니다. |
| **한곳에서 운영**<br>여러 서버, 백업, 콘텐츠, 플레이어와 콘솔을 함께 관리합니다. | **외부 접속 지원**<br>외부 TCP 응답을 확인하되 서버 일치 여부를 구분하고 필요한 경우에만 UPnP를 시도합니다. |

Paper, Vanilla, Purpur부터 모드 서버까지 지원하면서도 자주 쓰는 기능만 먼저 보여 줍니다. 상세 설정과 진단 도구는 필요할 때 열 수 있고, 한국어·영어 및 다크·라이트 모드를 제공합니다.

### 3단계로 시작하기

1. **[Portable EXE를 내려받아](https://github.com/Mangom72/MineHarbor/releases/latest/download/MineHarbor.exe)** 실행합니다.
2. 데이터 위치와 서버 종류, Minecraft 버전, 프리셋, 메모리를 선택하고 Minecraft EULA에 동의합니다.
3. `서버 시작`을 누른 뒤 화면에 표시된 주소를 친구에게 전달합니다.

메인 창은 먼저 표시되고 업데이트, 프로필, 최신 버전 목록은 백그라운드에서 불러옵니다. Java 준비, 파일 다운로드, 서버 시작, 포트 확인처럼 시간이 걸리는 작업은 현재 단계와 진행률을 화면에 표시하며, 콘텐츠 작업은 취소할 수 있습니다.

## 지원 범위

### 서버 종류

| 종류 | 파일 준비 | 버전 선택 | 실시간 명령 브리지 |
| --- | --- | --- | --- |
| Paper | 자동 | 지원 | Paper 1.13 이상에서 선택 사용 |
| Vanilla | 자동 | 지원 | 로컬 자동완성 |
| Purpur | 자동 | 지원 | Purpur 1.13 이상에서 선택 사용 |
| Fabric | 자동 | 지원 | 로컬 자동완성 |
| Forge | 자동 | 지원 | 로컬 자동완성 |
| NeoForge | 자동 | 지원 | 로컬 자동완성 |
| 직접 JAR | 사용자가 지정 | 요구 Java 직접 선택 | 로컬 자동완성 |

스냅샷과 프리릴리즈는 기본적으로 숨기고 사용자가 `스냅샷 포함`을 켰을 때만 표시합니다. 서버 JAR은 EXE에 넣지 않으며, 새 서버를 만들 때 선택한 프로젝트의 공개 API에서 최신 호환 파일을 내려받고 제공된 해시를 검증합니다.

### Java 호환성

Minecraft 및 서버 종류에 맞춰 Java 8·11·16·17·21·25 중 필요한 런타임을 선택합니다. Java는 런처 EXE에 포함하지 않으며, 호환 버전이 없을 때 Eclipse Adoptium에서 한 번만 내려받아 SHA-256을 확인하고 캐시합니다. 직접 JAR은 제작자가 요구하는 Java 주 버전을 사용자가 지정할 수 있습니다.

### 시스템 요구 사항

| 항목 | 요구 사항 |
| --- | --- |
| 운영체제 | Windows 10 또는 Windows 11 x64 |
| 런타임 | .NET Framework 4.8 |
| 메모리 | 서버 규모에 따라 선택, 기본 추천 4GB |
| 인터넷 | 최초 Java·서버 파일 준비, 업데이트, 콘텐츠 검색, 외부 접속 검사에 필요 |
| 저장 공간 | 서버 파일, 월드, 플러그인·모드와 백업 크기에 따라 달라짐 |

## 핵심 기능

### 서버 생성과 여러 서버 관리

- 평화로움·쉬움·보통·어려움·하드코어 야생 프리셋
- 일반 지형·평지 크리에이티브 월드 프리셋
- 게임 모드, 난이도, PvP, 화이트리스트, 명령 블록, 정품 인증, 거리 설정 직접 편집
- Paper/Purpur에서 TNT·양탄자·레일, 중력 블록, 철사덫 갈고리 복사를 서버별로 선택 허용
- 프로필 생성, 복제, 폴더 가져오기, 이름 변경, 안전 보관과 기본 서버 선택
- 삭제한 서버를 30일간 보관하는 휴지통과 복구·영구 삭제 관리. 휴지통으로 보낼 때만 서버 이름을 입력하고 영구 삭제는 3초 안전 확인 사용
- 프로필별 월드·설정·플러그인·모드 분리
- 같은 포트의 서버를 동시에 실행할 때 확인 후 사용 가능한 포트로 자동 변경
- 서버별 접속 주소와 바로 옆 복사 기호, 외부 접속 실패 시 `접속 불가` 상태 표시
- 관리 도구 창을 열어 둔 상태에서도 메인 창의 상태·콘솔·주소 확인 가능
- 사용자가 명시적으로 켠 경우에만 실행되는 사용자 계정용 백그라운드 트레이 에이전트(베타)
- 비정상 종료 자동 재시작 및 10분 안에 3번 연속 실패하면 중단

Paper/Purpur의 복사 옵션은 서버가 공식적으로 지원하지 않는 호환 설정입니다. 1.19 이상은 `config/paper-global.yml`, 이전 버전은 `paper.yml`의 `settings.unsupported-settings`에 저장되며 서버를 다시 시작한 뒤 적용됩니다. 피스톤 복사는 지원되는 모든 Paper 세대에 제공하고, 중력 블록은 1.20.4 이상, 철사덫 갈고리는 1.21.4 이상 또는 실제 생성된 키가 확인된 서버에서만 활성화합니다. Spigot·Vanilla·모드 서버에는 Paper 전용 키를 쓰지 않고 각 서버의 기본 동작을 유지합니다. 직접 JAR은 기존 Paper 설정 파일과 해당 키를 감지한 범위에서만 옵션을 제공합니다. MineHarbor는 YAML의 다른 항목과 주석을 보존하고 변경 전 파일을 `.mineharbor/configuration-backups`에 보관하지만, 중요한 월드는 별도로 백업해 주세요.

### 업데이트와 백업

- 서버 파일 자동 업데이트 옵션과 수동 `서버 업글`
- 서버 종류·Minecraft 버전 변경 전 호환성 경고와 백업
- 전체 프로필 수동 백업, 보존 개수 설정, 내보내기와 외부 백업 가져오기
- 서버별 정기 백업, 시작 전·종료 후 백업, 반복 간격·매일·선택 요일·특정 날짜 한 번을 지정한 예약 시작·종료·재시작·명령 실행과 즉시 실행
- 컴퓨터 종료·절전 등으로 놓친 작업을 다음 실행 때 한 번 실행하거나 건너뛰거나 알림만 남기는 정책과 최대 지연 시간
- 예약 편집기의 다음 실행·위험도·서버가 꺼졌을 때 처리·5분 이내 충돌 미리보기, 재시작 전 플레이어 공지, 최근 결과와 개수·기간·총용량 백업 보존 정책
- 메인 `운영 기록`에서 서버 시작·종료·충돌·자동 재시작과 예약 결과를 서버/중요도/읽음 상태로 필터링하고 CSV로 내보내기
- 서버별 `.mineharbor/operations-history.json`의 최대 500개 로컬 기록, 원자적 저장, 프로세스 간 잠금과 SHA-256 연속 해시 변조 감지
- 백그라운드 운영(베타)을 켜면 창을 닫은 뒤에도 사용자 계정용 트레이 에이전트가 예약을 평가하며, Windows 로그인 자동 시작을 별도로 선택 가능
- SHA-256 무결성 확인과 임시 폴더 검증 후 안전 복원
- 설정 변경 전 `server.properties` 백업
- 사용자 승인 후에만 실행되는 런처 자동 업데이트와 실패 시 이전 EXE 복원
- 언어 변경 옆 `런처 업데이트` 버튼을 통한 즉시 재검사와 선택한 버전의 알림 숨기기

### 백그라운드 운영(베타)

- `서버 관리 → 백그라운드`에서 사용자가 동의한 경우에만 `MineHarbor.exe --background-agent`를 실행합니다.
- 시스템 트레이에서 서버별 시작·안전 종료·재시작·즉시 백업·콘솔 열기, 전체 안전 종료, 예약 일시 중지와 완전 종료를 사용할 수 있습니다.
- 에이전트가 시작한 서버는 메인 창을 닫아도 계속 실행되며, 에이전트 전용 콘솔은 로그 확인·자동완성·위험 명령 재확인을 제공합니다.
- 로컬 IPC는 현재 Windows 사용자 SID에만 권한을 부여한 이름 있는 파이프를 사용하고 요청 크기와 수신 시간을 제한합니다.
- GUI와 에이전트는 예약 파일의 프로세스 간 잠금과 PID·시작 시각 임대로 중복 실행을 차단합니다. 절전 복귀 시 놓친 작업 정책을 다시 평가합니다.
- 에이전트가 소유하지 않은 실행 중 서버에는 명령·종료·실행 중 백업을 시도하지 않습니다. GUI가 직접 시작한 서버는 기존처럼 창을 닫을 때 안전 종료됩니다.
- 런처 업데이트 전에는 에이전트 서버를 먼저 안전 종료합니다. 제한 시간 안에 종료되지 않으면 서버 보호를 위해 업데이트와 에이전트 종료를 취소합니다.
- 관리자 권한 Windows 서비스는 설치하지 않으며, 로그온 전 실행·Windows 알림·원격 웹/Discord 제어는 아직 지원하지 않습니다.

> [!WARNING]
> 최신 버전에서 생성한 월드를 구버전 서버로 열면 월드가 손상될 수 있습니다. 런처는 위험한 다운그레이드를 차단하지만, 중요한 월드는 별도 장치에도 백업해 두는 것을 권장합니다.

### 콘텐츠, 플레이어와 콘솔

- 설치된 플러그인·모드·데이터팩을 MineHarbor 관리 파일과 수동 설치 파일로 구분
- 개별·일괄 업데이트 확인, 활성화·비활성화, 복구 가능한 제거와 서버별 manifest 저장
- 현재 버전과 로더에 맞는 Modrinth 플러그인·모드·데이터팩 검색. 빈 검색어로 인기순 결과를 조회하고 제작자·다운로드 수·설명을 함께 표시
- 로컬 JAR·데이터팩 ZIP 파일을 검사한 뒤 선택한 서버나 월드에 직접 설치
- Modrinth 필수 의존성·순환 의존성, Minecraft 버전·로더 호환성 검사
- 선택한 월드의 `<월드 이름>/datapacks`에 설치하고 루트 `pack.mcmeta`, 압축 경로·중복 항목·파일 수·해제 크기를 검증
- 다운로드 크기와 SHA-512/SHA-1 검증 후 설치, 기존 파일 자동 백업
- 검색·설치·업데이트 진행률과 취소 기능, 창을 닫은 뒤 비동기 UI 콜백 차단
- 온라인 플레이어 이름 자동완성을 지원하는 화이트리스트, OP·DEOP, 추방, 차단·해제 플레이어 관리
- 검색 → 로그 분류 → 줄 바꿈 순서로 정리한 콘솔 도구막대와 일반 경고·호환성·오류 필터
- 읽던 위치를 유지하고 맨 아래를 볼 때만 새 로그를 따라가는 콘솔
- 개인정보를 가린 로그·설정·크래시 보고서 진단 묶음
- 다크·라이트 테마에 맞춘 제목 표시줄, 콘솔·목록 스크롤바, 둥근 입력·그룹 테두리, 드롭다운, 체크박스와 탭
- 창 크기·DPI에 맞춘 반응형 배치, 작은 화면에서만 필요한 스크롤, 키보드 포커스와 스크린 리더 설명
- 보조 창 X 입력이 뒤의 메인 버튼에 전달되지 않도록 같은 클릭과 닫기 지점 주변의 반복 클릭을 차단하고, 떨어진 위치의 의도적인 클릭은 즉시 허용
- X·Alt+F4로 런처를 닫을 때 항상 확인. 유휴 상태는 서버 종료 절차 없이 닫고, 작업 중에는 완료 대기, 서버 실행 중에는 안전 종료 후 닫기

### 서버 대시보드

- 서버 상태·가동 시간, Java 프로세스 CPU·메모리와 Java 버전
- 온라인 플레이어, 서버·월드·백업 용량, 최근 경고·오류와 다음 예약 작업
- 실제 외부 접속 검사 결과를 표시하며 확인되지 않은 상태를 성공으로 추정하지 않음
- Paper/Purpur 명령 브리지 연결 시 공개 서버 API에서 받은 TPS 1·5·15분 값과 MSPT 표시
- 브리지 미지원·연결 해제·권한 부족·서버 종료 상태는 임의 값 대신 명시적인 지원 불가 상태로 표시

### 빠른 명령과 자동완성

빠른 명령은 `카테고리 → 기능 → 명령` 구조로 정리됩니다. 예를 들어 `월드 → 난이도 → 어려움`, `월드 → 날씨 → 맑음`처럼 찾을 수 있으며 이름, 설명, 경로와 실제 명령어를 한 번에 검색합니다.

- `Ctrl+F`, 방향키, `Enter`, `Esc`로 명령 선택창 조작
- 명령 선택 후 필수·선택 인수를 둥근 인라인 토큰으로 표시하는 단계형 명령 빌더
- 미완성 인수는 회색, 현재 인수는 강조색, 잘못된 값은 위험색으로 표시하고 값 확정 시 다음 인수로 자동 이동
- `↑`·`↓`로 후보 이동, `Tab` 또는 미완성 상태의 `Enter`로 현재 값을 확정, `Shift+Tab`으로 이전 인수 이동
- 모든 필수 인수가 유효할 때만 전송 버튼을 활성화하며, 선택 인수가 남았으면 `Enter`로 생략하고 전송 가능
- `Ctrl+Space`로 후보 다시 열기, `Ctrl+↑`·`Ctrl+↓`로 명령 기록 탐색
- 후보가 많으면 창의 위·아래 가용 공간 중 넓은 쪽에서 최대 430px까지 확장하고, 적으면 필요한 높이만 사용
- 빠른 명령 카드는 오른쪽 고정 열을 유지하고, 콘솔은 별도 왼쪽 열에서 겹침 없이 표시
- 멀티 서버 콘솔에서 기본 명령과 온라인 플레이어 이름 자동완성
- 메인 콘솔의 기본 명령·연결된 플레이어 인수 자동완성과 예약 명령 편집의 기본 명령 자동완성
- 제재 목록·IP 차단, 시드·시간 조회, 게임 규칙, 데이터팩 활성화/비활성화와 안전한 조회형 고급 명령을 포함한 70개 이상의 기본 명령
- `{player}` 같은 필수 인수, `[reason]` 같은 선택 인수와 `[count=1]` 같은 기본값이 있는 선택 인수
- 온라인 플레이어·선택자·게임 모드·난이도·참/거짓·좌표·시간 추천과 검색 가능한 아이템·효과 후보
- 서버 Minecraft 버전에 맞지 않는 명령은 목록과 로컬 자동완성에서 제외
- 일반·확인·위험 3단계 정책을 적용하고 `reload`, 광범위 변경과 조건부 위험 값은 강한 경고로 구분
- Paper/Purpur 브리지 연결 시 `플러그인 → 플러그인 이름 → 명령어`로 실시간 분류

사용자 명령은 데이터 루트의 `config/quick-commands.json`에 별도로 저장되어 런처 업데이트 후에도 유지됩니다. 브리지는 실행할 때마다 만든 무작위 토큰과 임시 포트로 `127.0.0.1`에만 연결하며, 실제 명령 실행은 기존 콘솔 입력 경로를 사용합니다.

## 외부 접속

런처는 이미 잘 작동하는 공유기 설정을 우선 사용하며 함부로 바꾸지 않습니다.

```mermaid
flowchart TD
    A[서버 시작] --> B{로컬 TCP 포트가 열렸나요?}
    B -- 아니요 --> C[콘솔 오류와 포트 충돌 확인]
    B -- 예 --> D{외부 TCP 응답이 있나요?}
    D -- 예 --> E[TCP 응답 표시 · 서버 일치 미확인 · 공유기 설정 유지]
    D -- 아니요 --> F[UPnP 장치와 포트 충돌 확인]
    F --> G[필요한 매핑만 생성]
    G --> H{외부 접속 재검사 성공?}
    H -- 예 --> I[공인 IP와 포트 표시]
    H -- 아니요 --> J[수동 포트포워딩 안내 표시]
```

- 일반 외부 검사는 TCP 포트가 열렸는지만 알 수 있으므로 Minecraft 서버 일치 여부를 확정하지 않습니다. 응답이 있으면 후보 주소와 `서버 일치 미확인`을 함께 표시하고 공유기 설정은 변경하지 않습니다.
- MineHarbor가 만든 UPnP 매핑을 다시 검사해 응답한 경우에만 대시보드에 `확인됨`으로 표시합니다.
- 다른 내부 PC가 같은 외부 포트를 사용하면 덮어쓰지 않고 충돌로 알립니다.
- 직접 SSDP/SOAP 방식을 우선 사용하고 Windows COM을 백업 경로로 시도하며, 기본 외부 포트가 충돌하면 최대 8개의 대체 포트를 확인합니다.
- 기본 TCP 매핑과 Minecraft Query 사용 시 필요한 UDP 매핑만 시도합니다.
- 서버가 끝나면 현재 런처 세션이 만들고 기록과 정확히 일치하는 매핑만 삭제합니다.
- 최종 실패 시 내부 IPv4, 기본 게이트웨이, 포트, 공유기 관리 주소, 방화벽 및 이중 NAT·CGNAT 가능성을 안내합니다.
- 외부 검사 서비스 자체가 응답하지 않으면 접속 실패로 단정하거나 UPnP를 실행하지 않습니다.

## 현재 범위와 제한

| 기능 | 현재 범위 |
| --- | --- |
| 운영체제와 런타임 | Windows 10/11 x64 및 .NET Framework 4.8. SDK 스타일 프로젝트도 호환성 검증용 `net48`을 유지합니다. |
| 콘텐츠 공급자 | 자동 검색·의존성 해결·업데이트는 Modrinth를 사용합니다. 다른 출처의 JAR·ZIP은 파일 설치와 수동 파일 관리로 다룹니다. |
| 일정 실행 | 기본값은 열린 MineHarbor 창에서 평가합니다. 사용자가 백그라운드 운영(베타)을 켜면 사용자 계정용 트레이 에이전트가 창을 닫은 뒤에도 평가합니다. 관리자 권한 Windows 서비스는 설치하지 않습니다. |
| 운영 기록 | 서버 수명 주기, 에이전트와 예약 작업 결과를 로컬에 기록합니다. Windows 알림, Discord와 웹 원격 관리는 아직 제공하지 않습니다. |
| 실시간 서버 정보 | 명령 브리지의 라이브 플레이어·TPS·MSPT·플러그인 명령은 Paper/Purpur 1.13 이상에서 선택적으로 지원합니다. 나머지 서버는 로컬 명령 자동완성을 사용하며 얻을 수 없는 지표는 `지원되지 않음`으로 표시합니다. |
| 외부 접속 판정 | 일반 TCP 응답만으로 Minecraft 서버가 맞다고 확정하지 않습니다. MineHarbor가 만든 매핑의 사후 검사만 `확인됨`으로 표시합니다. |
| 코드 서명 | 릴리스마다 자체서명하며 공개 인증 기관의 신뢰 서명은 제공하지 않습니다. |

## 데이터와 개인정보

최초 실행에서 다음 중 하나를 선택할 수 있습니다.

| 데이터 위치 | 경로 |
| --- | --- |
| 사용자 데이터 | `%LOCALAPPDATA%\MineHarbor` |
| Portable 데이터 | EXE 옆의 `Minecraft-Servers-Data` |
| 사용자 지정 | 쓰기 가능하고 시스템 폴더가 아닌 선택 경로 |

기존 `Minecraft-Servers-Data`를 찾으면 우선 제안하지만 자동으로 이동·삭제·덮어쓰지 않습니다. 이전 제품 이름으로 만든 `%LOCALAPPDATA%\MinecraftServerLauncher`가 있으면 새 폴더를 강제로 만들지 않고 기존 서버 데이터를 계속 사용합니다. 각 서버는 `servers\<프로필 이름>` 아래에서 월드와 설정을 분리해 보관합니다.

런처는 사용 통계나 분석 정보를 수집하지 않으며 로그와 진단 묶음을 자동 전송하지 않습니다. 진단 묶음은 사용자가 직접 만들고 공유할 때만 PC 밖으로 나갑니다. 자세한 네트워크 사용처와 가림 항목은 [개인정보 처리 안내](PRIVACY.md)에서 확인할 수 있습니다.

## 안전하게 설계된 부분

- Java, 서버 파일, 콘텐츠, 브리지와 런처 업데이트의 HTTPS·허용 호스트·리디렉션·크기·해시 검증
- 기존 포트포워딩과 다른 기기의 UPnP 매핑을 덮어쓰지 않음
- 자동 OP는 정품 계정 인증이 켜졌을 때만 동작해 닉네임 사칭 위험 완화
- 브리지의 루프백 전용 통신, 실행별 임시 토큰과 외부 포트 미사용
- 진단 묶음에서 사용자 경로, IP, 서버 소유자, RCON 비밀번호 등 제거
- 복원·업데이트 실패 시 기존 데이터 또는 실행 파일로 되돌리기

## 문제 해결

| 증상 | 먼저 확인할 내용 |
| --- | --- |
| 서버가 바로 종료됨 | `콘솔 열기`에서 첫 오류를 확인하고 진단 요약을 살펴보세요. |
| 친구가 접속하지 못함 | 로컬 포트, Windows 방화벽, 포트포워딩, 공인 IP와 이중 NAT·CGNAT 안내를 확인하세요. |
| `Advanced terminal features...` 또는 `sun.misc.Unsafe` 경고가 보임 | 리디렉션된 GUI 콘솔이나 Java 라이브러리의 호환성 경고일 수 있습니다. 서버 실패와 구분되는 `호환성` 필터로 확인하세요. |
| 구버전 Paper가 도움말 또는 Java 에이전트 오류 후 종료됨 | 최신 런처로 갱신한 뒤 다시 시작하세요. 런처는 구버전에 맞는 실행 인수와 상대 JAR 경로를 사용합니다. |
| 서버 종류나 버전을 바꾼 뒤 오류가 발생함 | 플러그인·모드 호환성을 확인하고 필요하면 백업에서 복원하세요. |
| 서버 소유자 자동 OP가 동작하지 않음 | 정품 계정 인증과 닉네임 철자를 확인하세요. |
| 실시간 명령이 로컬 상태로만 표시됨 | Paper/Purpur 1.13 이상인지 확인하고 서버를 끈 뒤 `명령·브리지 관리`에서 설치 상태를 확인하세요. |
| 자동 업데이트나 콘텐츠 설치가 실패함 | 인터넷 연결, 보안 프로그램과 해당 프로젝트 API 접속 여부를 확인하세요. |

문제가 계속되면 민감 정보를 가린 `진단 묶음`을 만든 뒤 [GitHub Issues](https://github.com/Mangom72/MineHarbor/issues)에 증상과 재현 순서를 남겨 주세요. 보안 문제는 공개 이슈 대신 [보안 정책](SECURITY.md)의 방법으로 알려 주세요.

## 개발과 기여

Windows 10/11 x64, PowerShell 5.1 이상과 .NET Framework 4.x C# 컴파일러가 필요합니다. .NET 10 SDK가 있으면 SDK 스타일 `MineHarbor.csproj`의 `net48` 호환 빌드도 함께 확인할 수 있습니다.

```powershell
.\scripts\Prepare-BuildResources.ps1
.\build.ps1
.\test.ps1
dotnet build .\MineHarbor.csproj -c Release
```

설치 프로그램 빌드는 Inno Setup 6.7 이상이 필요합니다. 릴리스 워크플로는 Portable EXE·ZIP, 설치 프로그램, Paper/Purpur 명령 브리지, `SHA256SUMS.txt`와 `update.json`을 만듭니다. 빌드 원칙과 테스트 방법은 [CONTRIBUTING.md](CONTRIBUTING.md)를 확인해 주세요.

PR과 `main` 푸시의 일반 CI는 버전·문서 일치, SDK 스타일 `net48` 경고 없는 빌드, Portable EXE·ZIP, 명령 브리지와 실패 경로·UI 회귀를 검사합니다. 별도 릴리스 워크플로는 외부 Action을 전체 커밋 SHA로 고정하고, 릴리스 자산·SHA-256 목록·업데이트 메타데이터·자체서명 무결성과 임시 인증서 정리를 검증하며, 직전 공개 런처를 통한 자동 업데이트도 다시 확인합니다.

## 프로젝트 문서

- [변경 기록](CHANGELOG.md)
- [기여 안내](CONTRIBUTING.md)
- [개인정보 처리 안내](PRIVACY.md)
- [보안 정책](SECURITY.md)
- [콘텐츠·자동화 구조](docs/architecture/CONTENT_AUTOMATION.md)
- [백그라운드 에이전트 구조](docs/architecture/BACKGROUND_AGENT.md)
- [Paper/Purpur 복사 설정 호환성](docs/architecture/DUPLICATION_COMPATIBILITY.md)
- [빠른 명령 호환성](docs/architecture/QUICK_COMMAND_COMPATIBILITY.md)
- [.NET 현대화 검토](docs/architecture/DOTNET_MODERNIZATION.md)
- [전체 UI·UX·보안·UPnP 감사](docs/audits/FULL_UI_UX_SECURITY_UPNP_AUDIT.md)
- [MIT License](LICENSE)

---

## English

### Download

| Package | Best for | Link |
| --- | --- | --- |
| **Portable EXE** | Run one file without installation | **[Download from the permanent URL](https://github.com/Mangom72/MineHarbor/releases/latest/download/MineHarbor.exe)** |
| **Windows installer** | Start Menu, optional desktop shortcut, and uninstall support | **[Open the latest release](https://github.com/Mangom72/MineHarbor/releases/latest)** |
| **Portable ZIP** | Keep the launcher, README, and license together | **[Open the latest release](https://github.com/Mangom72/MineHarbor/releases/latest)** |

Current source version: `v1.12.0` · internal build: `26.2.45.76`. MineHarbor releases keep the same permanent Portable URL. Existing data under `%LOCALAPPDATA%\MinecraftServerLauncher` is detected and preserved; new user-data installations use `%LOCALAPPDATA%\MineHarbor`.

This README documents shipped behavior verified against the current `v1.12.0` source, automated tests, and public release assets—not roadmap items. Conditional and unsupported behavior is called out explicitly below.

> [!WARNING]
> Release executables carry the requested self-signed integrity signature, not a publisher identity trusted by a public certificate authority. Windows SmartScreen can therefore still warn. Verify the GitHub source and the release `SHA256SUMS.txt`.

### Minecraft servers without the setup maze

MineHarbor — Minecraft Server Launcher is a Windows desktop app for creating and operating Paper, Vanilla, Purpur, Fabric, Forge, NeoForge, and custom-JAR servers. It prepares a compatible Java runtime, downloads and verifies server files, keeps profiles isolated, manages backups and content, and helps make a server reachable from outside your network.

| | |
| --- | --- |
| **Start quickly**<br>Java, server JARs, and first-run settings are prepared in order. | **Stay compatible**<br>Java 8/11/16/17/21/25 is selected for the chosen server and Minecraft version. |
| **Operate in one place**<br>Manage multiple servers, backups, content, players, and consoles. | **Connect safely**<br>External TCP responses are reported separately from server identity; UPnP is attempted only after a confirmed closed-port result. |

### Quick start

1. **[Download the Portable EXE](https://github.com/Mangom72/MineHarbor/releases/latest/download/MineHarbor.exe)** and run it.
2. Choose a data location, server type, Minecraft version, preset, and memory, then accept the Minecraft EULA.
3. Press `Start server` and share the address shown by the launcher.

The main window appears first while update, profile, and current version data load in the background. Long operations report their current stage and progress, and content operations can be cancelled.

### System requirements

| Item | Requirement |
| --- | --- |
| Operating system | Windows 10 or Windows 11 x64 |
| Runtime | .NET Framework 4.8 |
| Memory | Depends on server size; 4 GB is the default recommendation |
| Internet | Required for initial Java/server preparation, updates, content search, and external-access checks |
| Storage | Depends on server files, worlds, plugins/mods, data packs, and backups |

## Highlights

- **Server profiles:** create, clone, import, rename, archive, select, run, and safely stop isolated servers; port conflicts can be reassigned after confirmation, and each address has a one-click copy action. Deleted server folders remain recoverable in Trash for 30 days. Exact-name entry is required only when moving a server to Trash; permanent deletion inside Trash uses a three-second locked confirmation.
- **Presets and settings:** survival difficulties, hardcore, normal or flat creative worlds, common `server.properties` controls, and version-aware Paper/Purpur switches for TNT/carpet/rail, gravity-block, and tripwire-hook duplication. Modern servers use `config/paper-global.yml`; legacy servers use nested `settings.unsupported-settings` in `paper.yml`. Paper-only keys are not written to Spigot, Vanilla, or modded servers; custom JARs require detected Paper keys. Changes preserve unrelated YAML and keep pre-change backups under `.mineharbor/configuration-backups`.
- **Compatible runtimes:** automatic Java 8/11/16/17/21/25 selection and download; explicit Java selection for custom JARs.
- **Updates and backups:** optional server auto-update, manual upgrades, staged profile restore, SHA-256 verification, export, scheduled or start/stop-hook backups, and count/day/size retention.
- **Scheduling:** per-server backup, start, stop, restart, and command jobs on intervals, daily times, selected weekdays, or a one-time local date. Missed runs can execute once, skip, or create a notification after a bounded delay. The editor previews the next run, risk, offline behavior, and five-minute conflicts.
- **Background operations (Beta):** after explicit opt-in, a per-user tray agent evaluates schedules after the GUI closes and can start, safely stop, restart, back up, and open a console for agent-owned servers. Optional Windows sign-in startup does not install an elevated service. Same-user secured named-pipe IPC, bounded messages, cross-process schedule locks, lease recovery, resume checks, and safe updater shutdown prevent duplicate or unsafe ownership.
- **Notifications and operations:** the main Operations view filters server lifecycle, crash/restart, and scheduled-job results by server, severity, and read state, and exports the visible list to CSV. Each server keeps up to 500 entries in `.mineharbor/operations-history.json` with atomic writes, a cross-process lock, and a verified SHA-256 hash chain.
- **Content:** installed plugin/mod/data-pack inventory, managed/manual distinction, compatibility and dependency checks, individual or batch updates, enable/disable/recoverable removal, verified Modrinth search, and local-file or world-targeted data-pack installation. Blank searches return popular results with author, download count, and description. Data packs are installed under the selected `<world>/datapacks` folder after root `pack.mcmeta`, path, duplicate-entry, entry-count, and expanded-size checks.
- **Dashboard:** status, uptime, Java CPU/memory/version, players, storage, warnings/errors, verified external access, next schedule, and bridge-provided TPS/MSPT without guessed values.
- **Operations:** online-player autocomplete for whitelist, OP, kick and ban controls; command/player suggestions in the main and managed consoles; time-independent command suggestions in the scheduler; plus search → category → word-wrap console tools with separate warning, compatibility, and error filters.
- **Diagnostics:** common startup-cause summaries and exportable bundles with paths, IP addresses, owner names, and secrets redacted.
- **Interface:** Korean/English, dark/light themed title bars, console/list scrollbars, rounded input/group frames, dropdowns, checkboxes, tabs and list surfaces, responsive windows, restrained scrolling, concise tooltips, keyboard/screen-reader metadata, background loading, visible progress, and cancellation for content operations.
- **Close safety:** closing a modeless tool window consumes the matching pointer release and briefly suppresses repeated clicks near its title-bar X without blocking deliberate clicks elsewhere. Closing the launcher always asks first; idle exits immediately without a server-stop path, active work waits, and a running server is stopped safely before exit.

### Background operations (Beta)

Enable this explicitly under `Server management → Background`. MineHarbor then runs `MineHarbor.exe --background-agent` in the current user account and optionally registers it for Windows sign-in. Its tray menu provides per-server start, safe stop, restart, immediate backup, and console access, plus pause, stop-all, and complete-exit actions. Servers started by the agent keep running after the GUI closes.

The GUI and agent communicate through a bounded named pipe whose ACL grants the current Windows SID only. Cross-process automation locks and PID/start-time leases prevent duplicate claims; resume events re-evaluate each job's bounded missed-run policy. The agent refuses to command, stop, or live-back-up a running server it does not own. GUI-started servers therefore retain the existing safe-close behavior. Launcher updates safely stop agent-owned servers first and abort instead of forcing an unsafe exit after the timeout.

This beta does not install an elevated Windows service and does not run before sign-in. Windows notifications and web/Discord remote control remain unsupported.

## Quick commands and live suggestions

Commands are organized as `Category → Function → Command`, such as `World → Difficulty → Hard` or `World → Weather → Clear`. Search matches display names, descriptions, hierarchy paths, and command text. The picker supports Ctrl+F, arrow keys, Enter, and Esc.

Cursor-aware suggestions, history, and editable templates work locally for every server type. The built-in catalog now contains more than 70 commands, including moderation lookup and IP bans, seed/time queries, common game rules, datapack enable/disable, and safe read-only advanced operations.

Choosing a command opens a step-by-step inline builder. Required `{player}`, optional `[reason]`, and defaulted optional `[count=1]` arguments appear as rounded tokens: incomplete values are gray, the active value is accented, and invalid input is highlighted without clearing other arguments. Confirming a value advances directly to the next argument. Up/Down selects candidates, Tab confirms, Shift+Tab returns to the previous argument, and Enter advances while required values are incomplete or sends once they are valid. Optional trailing values can be skipped. The send button remains disabled until every required argument is valid.

Player and target arguments combine connected names with Minecraft selectors; game mode, difficulty, booleans, numbers, time, and coordinates have useful recommendations; item and effect catalogs support substring search. Suggestion lists grow only as needed, up to 430 pixels in the larger available space above or below the input, without covering the input or Send button.

Commands outside the selected server's Minecraft version range are omitted from the picker and local suggestions. Normal, confirmation, and dangerous risk levels are applied consistently; `reload`, broad mutations, and conditionally destructive values receive a stronger warning. When the optional Paper/Purpur bridge is connected, registered plugin commands and live argument candidates also appear under `Plugins → Plugin name → Command`.

The quick-command card stays in a fixed right-side column while the console uses a separate left column, so toggling the console neither moves the card nor hides console output behind it.

The player-management field suggests connected player names. Main and managed consoles suggest common commands and available player arguments, while the scheduled-command editor suggests commands that do not depend on current players. Use Up/Down and Tab or Enter without leaving the keyboard.

The bridge binds only to `127.0.0.1`, uses a new random token and temporary port per run, opens no external port, and leaves command execution on the existing console-input path. User templates remain separate in `config/quick-commands.json` across launcher updates.

## External access flow

The launcher checks the local TCP listener and current external TCP reachability before touching UPnP. A generic port check cannot prove that the responding service is this Minecraft server, so an open result is shown as `server identity unverified` and router settings are left unchanged. After a confirmed closed-port result, it discovers UPnP through direct SSDP/SOAP first and Windows COM as a fallback, detects collisions, checks up to eight alternate external ports, creates only the required TCP mapping and optional Query UDP mapping, then tests again. Only a successful post-check of a mapping created by MineHarbor is shown as verified.

If access still fails, the manual guide shows the PC IPv4 address, default gateway, internal and external ports, router page, firewall status, and possible double NAT or CGNAT. On shutdown, only mappings created by the current launcher session and still matching its exact record are removed. An unavailable external check service is not treated as a closed port and does not trigger UPnP.

## Current scope and limitations

| Area | Current scope |
| --- | --- |
| OS and runtime | Windows 10/11 x64 and .NET Framework 4.8. The parallel SDK-style project remains on `net48` for compatibility verification. |
| Content providers | Automatic search, dependency resolution, and updates use Modrinth. JAR/ZIP files from other sources can be installed from file and are tracked separately. |
| Scheduling | Open MineHarbor windows evaluate jobs by default. When the user enables Background operations (Beta), a per-user tray agent continues after the GUI closes. No elevated Windows service is installed. |
| Operations history | Server lifecycle, agent, and scheduled-job results are stored locally. Windows notifications, Discord, and web remote management are not implemented. |
| Live server data | Live players, TPS/MSPT, and plugin commands require the optional bridge on Paper/Purpur 1.13+. Other server types retain local completion, and unavailable metrics are shown as unsupported rather than estimated. |
| External reachability | A generic TCP response does not prove Minecraft server identity. Only the post-check of a mapping created by MineHarbor is marked verified. |
| Code signing | Releases are self-signed per build and do not carry a public-CA trusted publisher signature. |

## Data, privacy, and safety

Choose user data (`%LOCALAPPDATA%\MineHarbor`), Portable data (`Minecraft-Servers-Data` beside the EXE), or a writable custom folder. Existing data under the legacy `%LOCALAPPDATA%\MinecraftServerLauncher` path is detected and reused without automatic moves, deletion, or overwrites.

The launcher collects no analytics or usage telemetry and never uploads logs or diagnostic bundles automatically. Downloads enforce HTTPS, allowed hosts and redirects, bounded sizes, and available hashes; existing router mappings are preserved; diagnostic bundles redact sensitive values; and automatic owner OP is disabled when online authentication is off. See the [privacy notice](PRIVACY.md) and [security policy](SECURITY.md) for details.

## Build and test

Build on Windows 10/11 x64 with PowerShell 5.1 or newer and the .NET Framework 4.x C# compiler.

```powershell
.\scripts\Prepare-BuildResources.ps1
.\build.ps1
.\test.ps1
dotnet build .\MineHarbor.csproj -c Release
```

Installer builds require Inno Setup 6.7 or newer. See [CONTRIBUTING.md](CONTRIBUTING.md) for build rules, tests, and the release workflow.

General CI on pull requests and `main` pushes checks version/document consistency, the warning-free SDK-style `net48` build, Portable EXE/ZIP, the command bridge, failure paths, and UI regressions. The separate release workflow pins external Actions to full commit SHAs and verifies release assets, the SHA-256 list, update metadata, self-signed integrity, temporary-certificate cleanup, and automatic updating through the previous public launcher.

## Support and policies

- [Latest release](https://github.com/Mangom72/MineHarbor/releases/latest)
- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Privacy](PRIVACY.md)
- [Security](SECURITY.md)
- [Content and automation architecture](docs/architecture/CONTENT_AUTOMATION.md)
- [Background agent architecture](docs/architecture/BACKGROUND_AGENT.md)
- [Paper/Purpur duplication compatibility](docs/architecture/DUPLICATION_COMPATIBILITY.md)
- [Quick-command compatibility](docs/architecture/QUICK_COMMAND_COMPATIBILITY.md)
- [.NET modernization review](docs/architecture/DOTNET_MODERNIZATION.md)
- [Full UI/UX, security, and UPnP audit](docs/audits/FULL_UI_UX_SECURITY_UPNP_AUDIT.md)
- [MIT License](LICENSE)
