# Changelog

제품 버전은 [Semantic Versioning](https://semver.org/)을 사용하며, `26.2.45.xx` 값은 별도의 내부 빌드 번호입니다.

Product versions follow [Semantic Versioning](https://semver.org/), while `26.2.45.xx` is a separate internal build number.

## [1.21.0] - 2026-07-27

### Korean

- **오류 문구 영어 지원 완료 (4/4)**: 런처 자동 업데이트, 서버 파일 다운로드(Paper·Purpur·Forge·NeoForge·Fabric·Vanilla), 서버 백업의 오류 문구 67개를 마지막으로 한국어·영어로 제공합니다.
- **이제 제품 전체의 오류 문구 387개가 두 언어를 지원합니다.** 영어로 사용하면 오류 대화상자도 영어로 표시됩니다.

### English

- **All error messages available in English (4 of 4)**: the final 67 messages — launcher auto-update, server file downloads (Paper, Purpur, Forge, NeoForge, Fabric, Vanilla), and server backup — are now available in both Korean and English.
- **All 387 error messages across the product now support both languages.** Running the launcher in English now shows English error dialogs.
## [1.20.0] - 2026-07-27

### Korean

- **오류 문구 영어 지원 확대 (3/4)**: 콘텐츠 관리(Modrinth 설치·업데이트·제거·의존성)와 Java 런타임 준비의 오류 문구 105개를 한국어·영어로 제공합니다.
- 예외 타입은 그대로 두고 영어 문구만 함께 담는 방식을 이어갑니다. 기존 예외 처리 동작에는 영향이 없습니다.

### English

- **More error messages available in English (3 of 4)**: 105 messages across content management (Modrinth install, update, removal, dependencies) and Java runtime preparation are now available in both Korean and English.
- This continues carrying the English wording alongside the unchanged exception type, so existing exception handling behaves exactly as before.
## [1.19.0] - 2026-07-27

### Korean

- **오류 문구 영어 지원 확대 (2/4)**: 백업·복원, 콘텐츠 다운로드, 중복 설정, 명령 브리지, UPnP 외부 접속, 서버 관리 기능의 오류 문구 95개를 한국어·영어로 제공합니다.
- 예외 타입은 그대로 두고 영어 문구만 함께 담는 v1.18.0의 방식을 이어갑니다. 기존 예외 처리 동작에는 영향이 없습니다.

### English

- **More error messages available in English (2 of 4)**: 95 messages across backup and restore, content downloads, duplication settings, the command bridge, UPnP external access, and server management features are now available in both Korean and English.
- This continues the v1.18.0 approach of carrying the English wording alongside the unchanged exception type, so existing exception handling behaves exactly as before.
## [1.18.0] - 2026-07-27

### Korean

- **영어 UI의 한국어 오류 메시지 수정**: 영어로 사용해도 오류 대화상자에 한국어 문구가 그대로 나오던 문제를 수정했습니다. 예외 타입은 바꾸지 않고 영어 문구만 함께 담아, 기존 예외 처리 동작에는 영향이 없습니다.
- **적용 범위**: 운영 기록, 백그라운드 에이전트, 예약 자동화, Discord 원격 제어, Windows 알림, 서버 휴지통, 저장 위치, 관리 서버 인계의 오류 문구 121개를 한국어·영어로 제공합니다.
- **표시 경로 정리**: 오류를 사용자에게 보여 주는 30곳이 현재 언어에 맞는 문구를 고르도록 했습니다. 아직 영어 문구가 없는 오류는 지금까지와 동일하게 원래 문구를 표시합니다.

### English

- **Korean error text in the English UI fixed**: error dialogs showed Korean text even when the launcher was used in English. Exception types are unchanged — the English wording is carried alongside — so existing exception handling behaves exactly as before.
- **Coverage**: 121 error messages across operations history, the background agent, scheduled automation, Discord remote control, Windows notifications, the server trash, storage location, and managed-server handoff are now available in both Korean and English.
- **Display paths updated**: the 30 places that surface errors to users now pick the wording for the current language. Errors that do not yet carry English wording continue to show their original text.
## [1.17.0] - 2026-07-26

### Korean

- **Discord 채널 알림 (선택)**: 서버 시작·종료·예기치 않은 종료를 허용 채널에 알립니다. 기본값은 꺼짐이며 설정 창에서 직접 켜야 합니다. 허용 채널로만 보내고 멘션은 차단하며, 분당 5건으로 제한해 채널을 도배하지 않습니다.
- **백업 결과 회신**: `/mineharbor backup`이 `백업을 시작했습니다`로 끝나지 않고 완료된 백업 파일 이름과 크기를 알려 줍니다. 월드가 커서 90초를 넘기면 백업은 계속 진행하면서 진행 중임을 회신하고 결과는 운영 기록에 남습니다.
- **상태에 접속자 수 표시**: 서버를 지정한 `/mineharbor status`에 명령 브리지가 연결된 경우 현재 접속자 수를 함께 표시합니다. 서버를 지정하지 않은 전체 조회는 프로필마다 조회가 반복되지 않도록 기존과 동일하게 동작합니다.

### English

- **Discord channel announcements (optional)**: server start, stop, and unexpected shutdown are announced in the approved channel. This is off by default and must be enabled in the settings window. Messages go only to the approved channel, mentions are suppressed, and the rate is capped at five per minute.
- **Backup results reported**: `/mineharbor backup` no longer stops at "Backup started" — it reports the resulting file name and size. If a large world takes more than 90 seconds the backup keeps running, the reply says so, and the result is recorded in the operations history.
- **Player count in status**: `/mineharbor status` for a specific server now includes the current player count when the command bridge is connected. The all-server view is unchanged so per-profile lookups are not repeated.
## [1.16.0] - 2026-07-26

### Korean

- **진단 번들 IPv6 주소 가림**: 진단 번들의 `latest.log`와 크래시 리포트에서 IPv4만 가리고 IPv6로 접속한 플레이어 주소는 그대로 남던 문제를 수정했습니다. 운영 기록과 동일한 기준을 사용하며 로그 시각(`12:34:56`)은 지우지 않습니다.
- **진단 번들 경로 가림 강화**: Windows 경로는 대소문자를 구분하지 않는데도 정확히 같은 표기만 가려, 로그에 다른 대소문자로 남은 사용자 폴더와 서버 경로가 노출되던 문제를 수정했습니다.
- **진단 번들에 운영 기록 추가**: 최근 운영 기록 200건을 `operations-history.txt`로 함께 담아 지원 요청 시 문제 발생 순서를 확인할 수 있습니다. 각 항목은 기록 시점에 이미 가려진 내용만 사용합니다.
- **Discord 도움말에 서버 목록 표시**: `/mineharbor help`가 현재 사용할 수 있는 서버 이름을 함께 보여 줍니다. 프로필 이름을 추측하지 않아도 되며, 서버가 많으면 일부만 표시하고 남은 개수를 알려 줍니다.

### English

- **IPv6 addresses redacted in diagnostic bundles**: `latest.log` and crash reports inside a diagnostic bundle redacted only IPv4 addresses, leaving the addresses of players connecting over IPv6 intact. The bundle now uses the same rule as the operations history and still preserves log timestamps (`12:34:56`).
- **Stronger path redaction in diagnostic bundles**: Windows paths are case-insensitive, but only exact-case matches were redacted, so user folders and server paths written with different casing leaked into shared bundles.
- **Operations history included in diagnostic bundles**: the 200 most recent operations-history entries are now written to `operations-history.txt` so support requests show the order events happened in. Only content already redacted at record time is included.
- **Discord help lists available servers**: `/mineharbor help` now shows the server names you can actually use, so profile names no longer have to be guessed. With many servers it lists a subset and reports how many were omitted.
## [1.15.4] - 2026-07-26

### Korean

- **IPv6 접속 주소 가림**: 운영 기록·Windows 알림·Discord `/mineharbor errors` 응답에서 IPv4만 `[IP]`로 가리고 IPv6로 접속한 플레이어 주소는 그대로 노출되던 문제를 수정했습니다. 로그 시각(`12:34:56`)이나 `Class::method` 같은 표기는 지우지 않습니다.
- **전체 상태 조회 정리**: 서버를 지정하지 않은 `/mineharbor status`가 응답 길이 상한에 걸려 뒤쪽 서버를 조용히 잘라내던 문제를 수정했습니다. 한 응답에서 다루는 서버 수를 제한하고 생략된 개수와 개별 조회 방법을 한국어·영어로 안내하며, 프로필마다 반복되던 조회 부하도 함께 줄입니다.
- **Discord 설정 체크박스 복구**: `저장된 토큰 제거`가 켜져 있을 때 `Discord 원격 제어 사용`이 아무 설명 없이 다시 꺼지던 문제를 수정했습니다. 이제 마지막에 선택한 항목이 유지되고 반대쪽이 자동으로 해제되며, 토큰을 지우면 원격 제어도 함께 꺼진다는 사실을 화면에서 먼저 알려 줍니다.
- **입력 예시 문구 정렬**: 여러 줄 입력의 예시 문구가 상자 한가운데 떠 있어 실제 입력 시작 위치와 어긋나던 문제를 수정했습니다. 여러 줄 입력은 첫 줄에, 한 줄 입력은 세로 가운데에 맞춥니다.
- **평문 봇 토큰 정리**: Discord 설정 창을 닫을 때 입력 컨트롤에 남아 있던 평문 봇 토큰을 지웁니다. 저장된 토큰은 이전과 같이 현재 Windows 사용자 범위 DPAPI로만 보관합니다.
- **설치 파일 체크섬 오류 처리**: Forge·NeoForge Installer의 `.sha256` 응답이 비어 있거나 형식이 다를 때 인덱스 예외로 중단되지 않고 검증 실패로 처리합니다. 체크섬 형식 검사도 함께 강화했습니다.

### English

- **IPv6 client addresses redacted**: operations history, Windows notifications, and Discord `/mineharbor errors` replies previously redacted only IPv4 addresses, leaking the addresses of players connecting over IPv6. Log timestamps (`12:34:56`) and tokens such as `Class::method` are left intact.
- **All-server status cleanup**: `/mineharbor status` without a server no longer silently drops trailing servers when the reply hits the length limit. The number of servers per reply is bounded, the omitted count and per-server lookup are explained in Korean and English, and the repeated per-profile lookup cost is reduced.
- **Discord settings checkbox fixed**: turning on `Enable Discord remote control` while `Remove saved token` was checked silently reverted with no explanation. The most recent choice now wins, the opposite option clears itself, and the form states up front that removing the token also turns remote control off.
- **Placeholder alignment**: placeholder text in multi-line editors floated in the vertical middle of the box, away from where typing actually starts. Multi-line editors now align the placeholder to the first line while single-line editors keep vertical centering.
- **Plaintext bot token cleared**: closing the Discord settings window clears the plaintext bot token left in the input control. The stored token continues to be protected with current-user Windows DPAPI only.
- **Installer checksum error handling**: an empty or unexpected `.sha256` response for the Forge or NeoForge installer is now reported as a checksum verification failure instead of throwing an index exception, and the checksum format check was tightened.

## [1.15.3] - 2026-07-26

### Korean

- **Discord 허용 목록 입력 복구**: 설정 창의 고정 높이 필드가 먼저 공간을 차지해 `허용 사용자`와 `허용 역할` 입력칸이 사실상 0 높이로 접히던 문제를 수정했습니다.
- **보안 설정 재배치**: 앱 연결 정보와 접근 허용 목록을 분리해 사용자·역할 입력칸을 항상 보이게 하고, 허용 서버 목록 옆에서 보안 경계를 한 번에 설정할 수 있게 했습니다.
- **즉시 검증과 복구 안내**: 필수 입력 누락과 잘못된 ID를 화면 안에서 즉시 설명합니다. 저장 시 일반 오류 창을 먼저 띄우지 않고 문제가 있는 입력칸으로 포커스를 이동하며, Discord 사용자 ID 복사 경로를 함께 표시합니다.
- **반응형 높이와 접근성**: 남는 세로 공간이 채널 입력칸을 과도하게 늘리지 않도록 별도 여백 행을 사용하고, 허용 목록 입력 최소 높이·한국어/영어 문구·키보드 포커스·스크린 리더 정보를 검증합니다.
- **보안 기본값 유지**: Discord 사용자 또는 역할 중 하나 이상을 요구하는 기존 허용 목록 정책, DPAPI 토큰 보호, 길드·채널·프로필 제한과 임의 콘솔·셸·파일 작업 차단은 그대로 유지합니다.

### English

- **Discord allowlist input restored**: fixed a layout defect where fixed-height connection fields consumed the available height and collapsed the Allowed users and Allowed roles editors to almost zero height.
- **Clear security layout**: app credentials and access allowlists are now separated. User and role editors remain visible beside the approved-server list so the complete security boundary can be configured in one view.
- **Inline validation and recovery guidance**: missing required values and invalid IDs are explained in the form. Saving focuses the exact field instead of first showing a generic error dialog, and the UI includes the Discord path for copying a user ID.
- **Responsive sizing and accessibility**: a dedicated spacer absorbs remaining height instead of stretching the channel editor. Tests cover allowlist minimum height, Korean/English copy, keyboard focus, and screen-reader metadata.
- **Secure defaults preserved**: the existing requirement for at least one allowed user or role, DPAPI token protection, guild/channel/profile restrictions, and the prohibition on arbitrary console, shell, and file operations remain unchanged.

## [1.15.2] - 2026-07-26

### Korean

- **Discord 가이드 문구 잘림 수정**: 좁은 창과 DPI 배율에서 4개 단계 설명 및 하단 보안 안내의 두 번째 줄이 카드 경계에 가려지던 문제를 수정했습니다.
- **내용 기반 카드 높이**: 고정 25% 행 높이와 말줄임 표시를 제거하고, 한국어·영어 문구가 실제로 차지하는 줄 수에 맞춰 각 카드와 보안 안내의 높이를 계산합니다.
- **작은 화면 대응**: 가이드 창은 내용과 작업 영역에 맞춰 높이를 자동 조정합니다. 화면이 부족한 경우에만 단계 목록 내부에 세로 스크롤을 표시하며 헤더, 보안 안내와 설정 버튼은 계속 보입니다. 가로 스크롤은 만들지 않습니다.
- **다크 테마 시각 검증**: 사용자 제보와 같은 다크 테마의 실제 WinForms 렌더링에서 모든 문구, 카드 간격과 버튼 위계를 확인했습니다.
- **회귀 검증**: 단계 행의 자동 크기, 설명·보안 문구의 자동 줄바꿈, 말줄임 금지와 내부 스크롤 범위를 기존 32개 런처 테스트 그룹에 추가했습니다.

### English

- **Discord guide clipping fixed**: the second lines of the four step descriptions and footer security note no longer get clipped by their cards in narrow windows or at scaled DPI.
- **Content-driven card heights**: fixed 25-percent rows and ellipsis behavior were removed. Every step card and the security note now measure the actual Korean or English wrapped text.
- **Small-screen behavior**: the guide fits its height to the content and current working area. Only the step list becomes vertically scrollable when space is insufficient, keeping the heading, security note, and setup actions visible without a horizontal scrollbar.
- **Dark-theme visual verification**: the real WinForms dark-theme rendering was checked against the reported layout for complete copy, balanced card spacing, and clear action hierarchy.
- **Regression coverage**: the existing 32 launcher groups now assert auto-sized step rows, wrapped step/security labels, no ellipsis, and the scoped internal scroll container.

## [1.15.1] - 2026-07-26

### Korean

- **Discord 첫 연결 가이드**: Discord 메뉴에 처음 들어갈 때 봇 토큰, 애플리케이션·서버·채널 ID, 허용 사용자 또는 역할과 허용 서버가 아직 등록되지 않았다면 4단계 시작 가이드를 먼저 표시합니다.
- **현대형 온보딩 UI**: 기존 다크·라이트 테마와 둥근 카드·버튼을 사용하고, Discord 앱 생성·길드 설치·ID 복사·MineHarbor 연결 순서를 한 화면에 정리했습니다. 창 전체에 불필요한 스크롤을 만들지 않으며 DPI, 키보드와 스크린 리더 정보를 지원합니다.
- **안전한 진입 흐름**: 가이드에서 명시적으로 `설정 시작`을 선택한 경우에만 기존 설정 화면으로 이동합니다. `나중에` 또는 `Esc`는 설정을 변경하지 않으며, 이미 등록된 사용자는 가이드를 건너뜁니다. 설정 화면에서도 가이드를 다시 열 수 있습니다.
- **보안 경계 유지**: Developer Portal은 고정된 공식 HTTPS 주소만 열고, 토큰의 현재 사용자 DPAPI 보호와 임의 콘솔·셸·파일 명령 차단을 가이드에서 명확히 안내합니다.
- **회귀 검증**: 미등록·등록 상태 판정, 진입 분기, 4개 단계 카드, 접근성 이름, `Enter`·`Esc` 동작과 기존 Discord 원격 제어 검사를 32개 런처 테스트 그룹 안에서 함께 검사합니다.

### English

- **First-run Discord guide**: entering the Discord menu now shows a four-step onboarding guide when no bot token, application/guild/channel IDs, allowed user or role, and approved server profile have been registered.
- **Modern onboarding UI**: the guide reuses MineHarbor's dark/light palette, rounded cards, and managed buttons to explain app creation, guild installation, ID collection, and MineHarbor connection in one view. It avoids unnecessary whole-form scrolling and supports DPI scaling, keyboard operation, and screen readers.
- **Explicit setup transition**: only `Start setup` advances to the existing settings form. `Not now` or `Esc` changes nothing, while complete registrations skip the guide. The guide can also be reopened from the settings footer.
- **Security boundary preserved**: the portal button opens only the fixed official HTTPS Developer Portal URL, and the guide calls out current-user DPAPI token protection plus the absence of arbitrary console, shell, or file commands.
- **Regression coverage**: the existing 32 launcher groups now also check unregistered/registered detection, entry routing, four step cards, accessible labels, and `Enter`/`Esc` behavior alongside the Discord remote-control suite.

## [1.15.0] - 2026-07-26

### Korean

- **Discord 원격 제어(베타)**: 사용자가 별도로 동의하고 직접 만든 봇을 연결하면 백그라운드 에이전트가 공개 수신 포트 없이 Discord Gateway에 연결하고 길드 전용 `/mineharbor` 명령을 등록합니다.
- **제한된 운영 명령**: 허용된 서버의 상태, 브리지 연결 시 온라인 플레이어, 최근 경고·오류를 조회하고 시작·백업을 요청할 수 있습니다. 안전 종료와 재시작은 요청자에게만 유효한 60초 단일 사용 확인 버튼을 거칩니다.
- **다중 허용 목록과 속도 제한**: 애플리케이션·길드·채널을 고정하고 Discord 사용자 또는 역할과 MineHarbor 서버 프로필을 각각 허용 목록으로 검사합니다. 사용자별 분당 요청 수를 제한하며 임의 콘솔·셸·파일 실행은 제공하지 않습니다.
- **자격 증명 보호**: 봇 토큰은 사용자 데이터의 `discord-remote.json`에 현재 Windows 사용자 범위 DPAPI 암호문으로만 저장합니다. 설정 파일은 64KiB 상한, 스키마 검증, 프로세스 간 잠금, 원자적 교체와 손상·미래 스키마 원본 보존을 사용합니다.
- **기존 소유권 경계 재사용**: Discord 작업은 에이전트의 검증된 시작·안전 종료·재시작·백업 경로만 호출합니다. 포트만 열린 외부 소유 서버를 종료하거나 실행 중 백업하지 않으며 원격 기능 실패가 로컬 서버 운영을 중단시키지 않습니다.
- **Gateway 신뢰성**: Discord API v10의 3초 상호작용 응답을 지연 응답으로 먼저 승인하고, heartbeat, 재연결·세션 재개, 응답 크기 제한과 HTTP `Retry-After`를 처리합니다. 특권 Gateway Intent와 공개 HTTP 상호작용 endpoint는 사용하지 않습니다.
- **UI·문서·회귀 검증**: 다크·라이트 테마, DPI, 키보드와 스크린 리더 정보를 갖춘 설정 화면을 백그라운드 설정과 트레이에 추가했습니다. DPAPI, 허용 목록, 역할 권한, 확인 소유권·만료·재사용, 속도 제한, 임의 명령 차단, 손상 설정 보존을 포함해 런처 테스트를 32개 그룹으로 확장했습니다.

### English

- **Discord remote control (Beta)**: after separate opt-in and user-created bot setup, the background agent connects through the outbound Discord Gateway and registers a guild-only `/mineharbor` command without opening a public listener.
- **Bounded operations**: approved-server status, bridge-backed online players, and recent warnings/errors can be queried; start and backup can be requested. Safe stop and restart require a 60-second, single-use confirmation owned by the requester.
- **Layered allowlists and throttling**: the application, guild, and channel are fixed, while Discord users or roles and MineHarbor profiles are independently allowlisted. Per-user requests are throttled, and no arbitrary console, shell, or file execution is exposed.
- **Credential protection**: the bot token is stored only as current-user Windows DPAPI ciphertext in `discord-remote.json`. The 64 KiB settings store is schema validated, cross-process locked, atomically replaced, and preserves corrupt or future-schema originals.
- **Existing ownership boundary reused**: Discord actions invoke only the agent's verified start, safe-stop, restart, and backup paths. A merely listening externally owned server is never stopped or live-backed up, and remote-integration failure cannot stop local server operation.
- **Gateway reliability**: API v10 interactions are deferred within Discord's initial-response window, with heartbeat, reconnect/session resume, bounded responses, and HTTP `Retry-After` handling. No privileged Gateway Intent or public HTTP interaction endpoint is required.
- **UI, documentation, and regression coverage**: a themed, DPI-aware, keyboard/screen-reader-labeled setup view is available from Background settings and the tray. DPAPI, allowlists, role authorization, confirmation ownership/expiry/replay, rate limiting, arbitrary-command rejection, and corrupt-settings preservation expand the launcher suite to 32 groups.

## [1.14.0] - 2026-07-26

### Korean

- **실행 중 서버 무중단 인계**: 백그라운드 운영을 켠 상태에서 멀티 서버 관리 창을 닫으면 해당 창이 시작한 실행 중 서버를 중단하지 않고 사용자 계정용 에이전트에 넘길 수 있습니다. `중단 없이 인계`, `모두 안전 종료`, `취소`를 명확히 구분합니다.
- **검증된 소유권 전환**: 관리 자식마다 현재 Windows SID 전용 제어 파이프와 새 256비트 토큰을 만들고, 자식·기존 소유자·새 소유자의 PID와 프로세스 시작 시각이 모두 일치할 때만 소유권을 원자적으로 바꿉니다. 포트 상태나 프로세스 이름만으로 인계하지 않습니다.
- **인계 후 콘솔과 운영 유지**: 명령과 최근 로그를 관리 자식이 직접 보유하므로 GUI의 표준 입출력 파이프가 닫혀도 서버가 계속 실행됩니다. 에이전트 콘솔, 안전 종료·재시작, 실행 중 백업과 예약 명령은 같은 자식 제어 채널을 사용합니다.
- **실패와 경합 처리**: 응답 유실은 멱등 재시도하고, 자식이 다른 살아 있는 소유자를 실제로 확인한 경우에만 복구 성공으로 처리합니다. 인계 도중 종료 사건과 추가 창 닫기는 결과가 정해질 때까지 보류해 조기 종료와 GUI·에이전트의 중복 재시작을 막습니다.
- **부분 실패 UX**: 여러 서버 중 일부 인계가 실패하면 관리 창을 닫지 않고 실패한 서버를 계속 보유합니다. 이미 인계된 서버는 에이전트가 계속 관리하며 사용자에게 실패 프로필을 표시합니다.
- **회귀 검증**: 실제 Minecraft 서버를 실행하지 않고 현재 사용자 제어 파이프, 잘못된 토큰, PID 재사용 방지, 원자적 소유자 전환, 부모 출력 파이프 종료 후 로그 보존, 에이전트 인계·중복 요청·명령·로그 경로를 검사해 런처 테스트를 31개 그룹으로 확장했습니다.

### English

- **Live server transfer without restart**: when Background operations is enabled, closing multi-server management can hand its running managed children to the per-user agent without stopping them. The dialog clearly separates transfer, safe stop, and cancel.
- **Verified ownership transition**: every managed child gets a current-Windows-SID-only control pipe and fresh 256-bit token. Ownership changes atomically only after the child, previous owner, and new owner PID plus process-start times all match. A port or process name is never treated as proof.
- **Console and operations continuity**: the managed child retains commands and recent logs, so closing the GUI's standard streams does not stop the server. Agent console, safe stop, restart, live backup, and scheduled commands use the same child control channel.
- **Failure and race handling**: lost replies use idempotent retries and count as recovered only when the child confirms a different live owner. Process exits and repeated window-close requests are deferred while transfer is unresolved, preventing premature closure and duplicate GUI/agent restart handling.
- **Partial-failure UX**: if only some servers transfer, the management window stays open and continues owning failures while successfully transferred servers continue under the agent. Failed profile names are shown to the user.
- **Regression coverage**: without starting a Minecraft server, the 31 launcher groups now cover the real current-user pipe, bad tokens, PID-reuse rejection, atomic ownership changes, retained logs after a broken parent output stream, agent adoption, duplicate requests, commands, and logs.

## [1.13.0] - 2026-07-26

### Korean

- **선택형 Windows 알림**: 기본 비활성화 상태에서 사용자가 `운영 기록`, `백그라운드` 또는 트레이의 알림 설정을 켠 경우에만 백그라운드 에이전트가 새 운영 기록을 Windows 작업 표시줄 알림으로 표시합니다.
- **중요도·종류·조용한 시간**: 정보·경고·오류 최소 중요도와 서버, 예약, 백업, 콘텐츠, 네트워크, 업데이트·보안 종류를 선택하고 자정을 지나는 조용한 시간도 설정할 수 있습니다.
- **알림 폭주 방지**: 에이전트 시작 전의 오래된 기록은 다시 알리지 않고, 새 이벤트를 최대 50개로 제한해 8초 안에 여러 건이 발생하면 가장 중요한 최신 사건과 추가 개수를 하나로 요약합니다.
- **개인정보와 실패 격리**: 알림은 운영 기록에서 이미 가린 서버 절대 경로, IPv4 주소, 토큰·비밀번호·웹훅 값을 다시 정리하며 명령 원문을 표시하지 않습니다. 알림 설정이나 기록 읽기 실패가 서버·예약 작업을 중단시키지 않습니다.
- **현대형 설정 UX**: 한국어·영어, 다크·라이트 테마, DPI, 키보드와 스크린 리더 정보가 적용된 설정 화면에서 `저장 후 테스트`를 사용할 수 있습니다. 설정은 `windows-notifications.json`에 크기·스키마 검증과 프로세스 간 잠금을 적용해 원자적으로 저장합니다.
- **회귀 검증**: 기본 비활성화, 설정 왕복, 손상·미래 스키마 보존, 자정 경계 조용한 시간, 중요도·종류 필터, 이전 기록 재생 방지, 알림 요약·가림과 IPC 테스트 알림을 포함해 런처 테스트를 30개 그룹으로 확장했습니다.

### English

- **Opt-in Windows notifications**: notifications remain disabled by default. Only after the user enables them from Operations, Background, or the tray does the background agent show new operation events as Windows taskbar notifications.
- **Severity, category, and quiet hours**: choose an Info, Warning, or Error threshold; select server, schedule, backup, content, network, or update/security categories; and configure quiet hours that may cross midnight.
- **Notification flood control**: history that predates agent startup is not replayed. Up to 50 new events are bounded, and events arriving within eight seconds are collapsed into the most important recent event plus an additional-count summary.
- **Privacy and failure isolation**: notifications re-sanitize the absolute server paths, IPv4 addresses, token/password/webhook-like values already redacted from operations history and never display raw commands. Notification-setting or history-read failures cannot stop servers or schedules.
- **Modern settings UX**: a Korean/English, dark/light, DPI-aware, keyboard- and screen-reader-labelled form includes Save and test. `windows-notifications.json` uses bounded schema validation, a cross-process lock, and atomic replacement.
- **Regression coverage**: the 30 launcher groups now cover default-off settings, round trips, corrupt/future preservation, midnight-spanning quiet hours, severity/category filters, old-history suppression, collapse/redaction, and the IPC test-notification path.

## [1.12.0] - 2026-07-26

### Korean

- **사용자 계정용 백그라운드 에이전트(베타)**: `서버 관리 → 백그라운드`에서 명시적으로 동의하면 `MineHarbor.exe --background-agent`가 시스템 트레이에 상주해 창을 닫은 뒤에도 서버별 예약 백업·시작·종료·재시작·명령을 평가합니다. Windows 로그인 자동 시작과 충돌 재시작은 각각 선택 사항이며 관리자 권한 서비스는 설치하지 않습니다.
- **트레이 서버 운영**: 에이전트가 시작한 서버는 GUI 종료 후에도 유지됩니다. 트레이에서 서버별 시작·안전 종료·재시작·즉시 백업·콘솔, 전체 안전 종료, 예약 일시 중지와 완전 종료를 제공하고, 에이전트 콘솔은 자동완성과 실제 위험 명령 재확인을 사용합니다.
- **안전한 소유권과 IPC**: GUI/에이전트 통신은 현재 Windows 사용자 SID 전용 ACL을 가진 로컬 이름 있는 파이프와 제한된 JSON 요청을 사용합니다. 에이전트는 자신이 시작한 서버만 제어하며 같은 포트를 사용하는 다른 프로세스에는 명령·종료·실행 중 백업을 시도하지 않습니다.
- **예약 중복 및 절전 복구**: 자동화 파일 접근을 서버 경로별 프로세스 간 뮤텍스로 직렬화하고 기존 PID·시작 시각 임대와 결합했습니다. 절전 복귀 시 놓친 작업 정책을 다시 평가하되 GUI와 에이전트가 같은 작업을 중복 청구하지 않습니다.
- **안전 종료와 업데이트**: 관리 자식의 부모가 사라져도 Java 서버에 먼저 `stop`을 보내며, 에이전트 종료와 런처 업데이트는 소유 서버가 제한 시간 안에 안전 종료된 경우에만 계속합니다. 종료되지 않으면 강제 종료 대신 작업을 취소합니다.
- **설정·문서·회귀 검증**: `background-agent.json`은 원자적 저장, 크기·스키마 검증, 손상/미래 스키마 보존을 사용합니다. 한국어·영어 README, 개인정보·보안·기여·구조 문서를 동기화하고 전체 런처 테스트를 29개 그룹으로 확장했습니다.

### English

- **Per-user background agent (Beta)**: after explicit opt-in under `Server management → Background`, `MineHarbor.exe --background-agent` stays in the tray and evaluates scheduled backup, start, stop, restart, and command jobs after the GUI closes. Windows sign-in startup and crash restart are independent options; no elevated service is installed.
- **Tray server operations**: servers started by the agent survive GUI exit. The tray provides per-server start, safe stop, restart, immediate backup, and console access, plus stop-all, schedule pause, and complete exit. The agent console includes completion and confirmation of the exact risky command.
- **Safe ownership and IPC**: GUI/agent communication uses a local named pipe whose ACL grants only the current Windows SID, with bounded JSON requests. The agent controls only servers that it started and refuses to command, stop, or live-back-up another process using the configured port.
- **Schedule deduplication and resume recovery**: per-server path-derived Windows mutexes serialize automation-file access and complement existing PID/start-time leases. Resume re-evaluates bounded missed-run policies without duplicate GUI/agent claims.
- **Safe shutdown and update**: managed children first send `stop` when their parent disappears. Agent shutdown and launcher update proceed only after owned servers stop within the timeout; otherwise the operation aborts instead of forcing an unsafe exit.
- **Settings, documentation, and regression coverage**: `background-agent.json` uses bounded, schema-validated atomic storage and preserves corrupt or future-schema files. Korean/English README, privacy, security, contribution, and architecture documents are synchronized, and the launcher suite now contains 29 test groups.

## [1.11.0] - 2026-07-26

### Korean

- **요일·일회성 예약**: 기존 반복 간격과 매일 시각에 선택 요일 및 `yyyy-MM-dd HH:mm` 일회성 일정을 추가했습니다. 기존 스키마 1 자동화 파일은 원본을 읽는 동안 덮어쓰지 않고 메모리에서 스키마 2로 호환하며, 미래 스키마는 보존한 채 거부합니다.
- **놓친 작업 정책**: 컴퓨터 종료·절전·관리 창 중지로 지연된 작업을 다음 실행 때 한 번 실행하거나, 최대 지연 시간을 넘으면 건너뛰거나, 실행하지 않고 알림만 기록할 수 있습니다. 일회성 작업은 임대를 얻는 순간 다시 예약되지 않도록 비활성화하고 기존 실행 임대로 중복 실행을 계속 차단합니다.
- **예약 미리보기 UX**: 예약 편집기에서 다음 예상 실행 시각, 작업 위험도, 서버가 꺼져 있을 때의 처리, 5분 이내 다른 작업 충돌을 저장 전에 표시합니다. 관리 도구는 한국어·영어 문구가 잘리지 않는 3×3 배치로 정돈했습니다.
- **알림 및 운영 기록**: 메인 화면에 서버별 시작·종료·충돌·자동 재시작과 예약 결과를 모아 보는 `운영 기록`을 추가했습니다. 서버·중요도·읽음 상태 필터, 개별/전체 읽음, 새로고침과 CSV 내보내기를 지원합니다.
- **로컬 감사 무결성**: `.mineharbor/operations-history.json`은 서버당 최대 500개만 저장하며 원자적 교체, 프로세스 간 뮤텍스, 최대 4MiB, 스키마·중복·시각 검증과 SHA-256 연속 해시를 사용합니다. 민감 표식·절대 서버 경로를 가리고, 손상이나 미래 스키마는 자동으로 덮어쓰지 않습니다.
- **회귀 검증**: 기존 자동화 마이그레이션, 미래/손상 스키마 보존, 요일 계산, 지연 작업 건너뛰기, 일회성 중복 방지, 운영 기록 읽음 상태·가림·해시 변조·접근성과 기존 전체 기능을 28개 런처 테스트 그룹에서 검증합니다.

### English

- **Weekday and one-time schedules**: Selected weekdays and `yyyy-MM-dd HH:mm` one-time dates now complement interval and daily schedules. Existing schema-1 automation files migrate in memory without being overwritten during reads, while future schemas are preserved and rejected.
- **Missed-run policies**: Jobs delayed by shutdown, sleep, or a closed management window can run once when available, skip after a bounded delay, or record a notification without running. One-time jobs are disabled as soon as their lease is claimed, and existing leases continue to prevent duplicates.
- **Schedule preview UX**: Before saving, the editor shows the next expected run, action risk, offline-server behavior, and other jobs within five minutes. Main management tools now use a readable 3×3 layout for both Korean and English.
- **Notifications and operations**: A new main Operations view aggregates server start, stop, crash, automatic restart, and scheduled-job results. It supports server/severity/read filters, selected or bulk read state, refresh, and CSV export.
- **Local audit integrity**: Each `.mineharbor/operations-history.json` retains at most 500 entries and uses atomic replacement, a cross-process mutex, a 4 MiB limit, schema/duplicate/time validation, and a SHA-256 hash chain. Sensitive markers and absolute server paths are redacted; corrupt or future-schema files are never overwritten automatically.
- **Regression coverage**: The 28 launcher test groups now cover legacy automation migration, future/corrupt schema preservation, weekday calculation, overdue skipping, one-time deduplication, operation read state, redaction, hash tampering, accessibility, and all previous features.

## [1.10.0] - 2026-07-26

### Korean

- **단계형 빠른 명령 빌더**: 명령을 선택하면 필수 `{player}`, 선택 `[reason]`, 기본값 선택 `[count=1]` 인수를 인라인 토큰으로 표시합니다. 미완성·현재·완료·잘못된 값을 각각 구분하고 한 값을 확정하면 포커스를 유지한 채 다음 인수로 이동합니다.
- **완성도 기반 전송**: 필수 인수와 선택 인수를 별도로 판정해 필수 값이 없거나 입력값이 잘못되면 전송을 차단합니다. 선택 인수는 생략할 수 있고 실제 명령에는 플레이스홀더나 불필요한 공백이 남지 않으며, 위험 확인에는 최종 완성 명령만 표시합니다.
- **키보드와 상태 유지**: 위·아래 방향키, `Tab`/`Enter`, `Shift+Tab`, `Esc`로 후보 선택부터 이전 단계 수정·취소·최종 전송까지 이어집니다. 이전 인수로 돌아가거나 목록을 닫고 다시 열어도 다른 값과 명령별 작성 초안을 유지합니다.
- **후보와 목록 UX 확대**: 온라인 플레이어·선택자, 게임 모드, 난이도, 불리언, 숫자·시간·좌표 추천과 부분 검색 가능한 아이템·효과 후보를 제공합니다. 후보 목록은 항목 수와 창의 가용 공간에 따라 최대 430px까지 위나 아래로 확장하며 입력란과 전송 버튼을 가리지 않습니다.
- **자동완성 회귀 방지**: 공통 콘솔·플레이어 자동완성도 최대 20개 후보와 확장 높이를 사용하고, 후보 확정 `Enter`가 동시에 명령 전송으로 이어지지 않게 처리했습니다. 단계 전환, 기본값, 값 유지, 잘못된 인수, 동적 목록 높이와 접근성을 자동 검증합니다.

### English

- **Step-by-step quick-command builder**: Selecting a command renders required `{player}`, optional `[reason]`, and defaulted optional `[count=1]` arguments as inline tokens. Incomplete, active, completed, and invalid states are distinct, and confirming one value advances without losing focus.
- **Completion-aware sending**: Required and optional arguments are validated independently. Missing or invalid required values block sending, optional values can be omitted, generated commands contain no leftover placeholders or extra spaces, and risk confirmation shows only the final complete command.
- **Keyboard flow and retained drafts**: Up/Down, `Tab`/`Enter`, `Shift+Tab`, and `Esc` cover candidate selection, backward editing, cancellation, and final sending. Other values and per-command drafts survive moving backward or closing and reopening suggestions.
- **Richer candidates and taller lists**: Connected players and selectors, game modes, difficulties, booleans, numeric/time/coordinate recommendations, and substring-searchable item/effect catalogs are available. Lists size to their content and use up to 430 pixels in the larger free space above or below the input without covering input controls.
- **Completion regression protection**: Shared console and player completion now shows up to 20 candidates with expanded height, and accepting a candidate with `Enter` cannot also send the command. Automated coverage verifies transitions, defaults, retained values, invalid arguments, dynamic height, and accessibility.

## [1.9.0] - 2026-07-25

### Korean

- **운영 명령 확장**: 플레이어·IP 차단 목록과 IP 차단/해제, 월드 시드, 시간 조회·추가, 유휴 추방 시간, 자주 쓰는 게임 규칙, 데이터팩 목록·활성화·비활성화, `reload`, 경험치·월드 경계·강제 로딩 조회를 추가해 기본 빠른 명령을 70개 이상으로 확장했습니다.
- **선택 인수와 자동완성**: `ban {player} [reason]`, `give {player} {item} [count]`처럼 대괄호 선택 인수를 지원합니다. 주소·플레이어, 시간 단위, 분, 비율, 날씨, 게임 규칙, 데이터팩, 함수, 차원과 거리 매개변수 후보를 추가했습니다.
- **Minecraft 버전 호환성**: 명령별 최소·최대 Minecraft 버전을 저장하고 선택된 서버 버전에 맞지 않는 명령을 선택창과 로컬 자동완성에서 제외합니다. 데이터팩은 1.13 이상, `playersSleepingPercentage`는 1.17 이상에서만 표시하며 26.x에서 바뀐 구형 `time query daytime` 구문은 숨깁니다.
- **실제 3단계 위험도**: 선언만 되어 있던 `Normal`, `Confirm`, `Dangerous`를 기본·사용자·브리지 명령과 직접 콘솔 확인에 연결했습니다. `reload`와 광범위 변경은 빨간 강한 경고를 사용하고, `keepInventory false`와 `doMobSpawning false`는 입력값에 따라 위험도를 높입니다.
- **편집기 UX**: 사용자 명령 편집기에 현대형 위험도 선택, 최소·최대 Minecraft 버전, 선택 인수 안내와 접근성 정보를 추가하고 기존 `Confirm` JSON을 자동 호환합니다.

### English

- **Expanded operations catalog**: Added player/IP ban lists, IP ban/pardon, world seed, time queries and additions, idle timeout, common game rules, datapack listing and enable/disable, `reload`, and read-only experience, world-border, and force-load queries, growing the built-in quick-command catalog beyond 70 entries.
- **Optional arguments and completion**: Templates now support optional square-bracket arguments such as `ban {player} [reason]` and `give {player} {item} [count]`. Local candidates cover addresses or players, durations, minutes, percentages, weather, game rules, datapacks, functions, dimensions, and distances.
- **Minecraft version compatibility**: Definitions retain minimum and maximum Minecraft versions, and incompatible entries are omitted from the picker and local completion. Datapack commands require 1.13, `playersSleepingPercentage` requires 1.17, and the legacy `time query daytime` syntax is hidden on 26.x.
- **Effective three-level risk model**: The existing `Normal`, `Confirm`, and `Dangerous` levels now drive built-in, user, bridge, and direct-console warnings. `reload` and broad mutations use a stronger red warning, while values such as `keepInventory false` and `doMobSpawning false` raise risk conditionally.
- **Editor UX**: The user-command editor now provides a modern risk selector, minimum and maximum Minecraft versions, optional-argument guidance, and accessibility metadata while migrating legacy `Confirm` JSON automatically.

## [1.8.1] - 2026-07-25

### Korean

- **보조 창 닫기 입력 관통 보강**: 보조 창의 제목 표시줄 닫기 버튼을 누른 뒤 포인터를 빠르게 움직여도 같은 물리 입력의 마우스 해제가 메인 창 컨트롤에 전달되지 않습니다. 이후 추가 클릭은 기존처럼 닫기 지점 주변에서만 잠시 차단하므로 다른 위치의 의도적인 조작은 유지됩니다.
- **실사용 중 검증 안정화**: 마우스를 사용하는 컴퓨터에서도 회귀 검사가 외부 포인터 이동에 흔들리지 않도록, 닫기 누름과 해제 사이에 포인터가 보호 반경 밖으로 이동하는 실패 경로를 명시적으로 검사합니다.

### English

- **Stronger tool-window close-through protection**: Releasing the same physical close click can no longer reach a launcher control even when the pointer moves quickly after pressing a tool window's title-bar close button. Subsequent clicks remain briefly guarded only near the close point, preserving deliberate interaction elsewhere.
- **Stable verification on an active desktop**: The regression test now explicitly moves the pointer state outside the guard radius between close press and release, avoiding dependence on incidental real-pointer movement while the computer is in use.

## [1.8.0] - 2026-07-25

### Korean

- **Paper/Purpur 복사 동작 설정**: 설정 화면에서 TNT·양탄자·레일, 엔드 차원문 중력 블록, 철사덫 갈고리 복사를 각각 허용할 수 있습니다. Paper/Purpur 1.19 이상은 `config/paper-global.yml`, 구버전은 `paper.yml`의 중첩된 `settings.unsupported-settings`를 사용하며, 서버 버전에 존재하는 항목만 활성화합니다.
- **안전한 서버별 적용**: Spigot·Vanilla·Fabric·Forge·NeoForge에는 Paper 전용 키를 쓰지 않으며, 직접 JAR은 기존 Paper 설정 파일을 감지한 경우에만 옵션을 활성화합니다. 기존 프로필은 사용자가 새 설정 화면에서 저장하기 전까지 수동 설정을 덮어쓰지 않습니다.
- **설정 무결성과 복구**: YAML의 주석과 관련 없는 항목을 보존해 원자적으로 저장하고, 변경 전 파일은 서버별 `.mineharbor/configuration-backups`에 보관합니다. 중복 구역·중복 키·잘못된 값·인라인 구조·탭 들여쓰기·연결 경로·과대 파일은 원본을 변경하지 않고 거부합니다.
- **설정 화면 UX 정리**: 서버 규칙 아래의 과도한 빈 공간을 제거하고 현대형 그룹 카드와 테마 체크박스로 복사 호환성 옵션을 배치했습니다. 한국어·영어 안내, 위험 확인, 지원 상태, 키보드·스크린 리더 정보와 작은 화면에서만 나타나는 세로 스크롤을 제공합니다.
- **전체 UI 회귀 게이트**: 제품 창의 DPI 설정을 점검하고 기본 Windows 버튼·드롭다운·체크박스·트랙바·스크롤바 생성이 다시 들어오지 않도록 정적 검사를 강화했습니다. 복사 설정의 경로·보존·백업·중복·Spigot 오적용·구버전 프로필 보호를 포함해 런처 테스트가 27개 그룹으로 늘었습니다.

### English

- **Paper/Purpur duplication controls**: Server Settings can independently allow TNT/carpet/rail duplication, end-portal gravity-block duplication, and tripwire-hook duplication. Paper/Purpur 1.19+ uses `config/paper-global.yml`; older releases use the nested `settings.unsupported-settings` section in `paper.yml`, and only controls present in that server generation are enabled.
- **Safe per-server targeting**: Paper-only keys are never written to Spigot, Vanilla, Fabric, Forge, or NeoForge. A custom JAR enables the controls only after an existing Paper configuration is detected. Existing profiles retain manually edited settings until the user saves through the new UI.
- **Configuration integrity and recovery**: YAML comments and unrelated entries are preserved during atomic writes, with pre-change copies retained under each server's `.mineharbor/configuration-backups`. Duplicate sections or keys, invalid values, inline maps, tab indentation, linked paths, and oversized files are rejected without changing the source.
- **Cleaner settings UX**: Excess empty space below the server rules was removed and the compatibility options now use a modern themed group card and checkboxes. Korean/English guidance, risk confirmation, support status, keyboard/screen-reader metadata, and vertical scrolling only on small screens are included.
- **Full UI regression gate**: Product windows were checked for DPI scaling, and static tests now reject newly introduced default Windows buttons, dropdowns, checkboxes, trackbars, or scrollbars. Launcher coverage grows to 27 groups with path selection, preservation, backup, duplicate rejection, Spigot isolation, and legacy-profile protection.

## [1.7.5] - 2026-07-25

### Korean

- **빠른 명령 위치 복원**: 빠른 명령 카드를 콘솔 표시 여부와 관계없이 구버전처럼 오른쪽 410px 고정 열에 유지합니다. 콘솔을 열고 닫아도 카드가 전체 폭과 오른쪽 사이를 오가지 않습니다.
- **콘솔 겹침 제거**: 콘솔과 빠른 명령을 독립된 2열 작업 영역에 배치해 빠른 명령 패널 뒤에 가려지던 콘솔 오른쪽 출력과 입력 영역을 모두 표시합니다.
- **레이아웃 회귀 검증**: 고정 열의 위치·폭, 콘솔 전환 후 위치 유지, 서로 다른 열 배치와 실제 컨트롤 경계의 비중첩을 자동 검증하고 Windows 다크 화면에서 열림·닫힘 상태를 확인했습니다.

### English

- **Restored quick-command position**: The quick-command card now stays in the legacy fixed 410-pixel right column whether the console is open or closed. Toggling the console no longer moves the card between full-width and right-side layouts.
- **No console overlap**: The console and quick commands now occupy separate workspace columns, keeping the console's right-side output and input area visible instead of hiding it behind the quick-command panel.
- **Layout regression coverage**: Tests now verify the fixed column position and width, stable placement across console toggles, separate cells, and non-intersecting control bounds. Open and closed states were also checked on the real Windows dark UI.

## [1.7.4] - 2026-07-25

### Korean

- **추가 닫기 클릭 차단**: 보조 창의 제목 표시줄 X를 누른 좌표 주변에서 시스템 더블클릭 시간 동안 추가 입력을 소비합니다. 마우스 스위치 채터링이나 지연된 두 번째 클릭이 뒤의 메인 창 버튼을 누르지 못하며, X에서 벗어난 정상 클릭은 즉시 허용합니다.
- **항상 표시되는 런처 종료 확인**: 서버가 꺼져 있어도 MineHarbor 종료 여부를 확인합니다. 일반 종료는 서버 종료 프로세스를 실행하지 않고 즉시 닫으며, 진행 중인 작업과 실행 중인 서버에는 각각 작업 완료 대기와 안전 종료를 설명하는 별도 문구를 사용합니다.
- **종료 흐름 회귀 검증**: 클릭 보호의 최소·최대 시간, 좌표 경계, 반복 클릭과 만료, 유휴·작업 중·서버 실행 중 질문 분기 및 일반 종료의 비지연 경로를 자동 검증합니다.

### English

- **Repeated close-click suppression**: Additional input near a tool window's title-bar close coordinate is consumed for the Windows double-click interval. Switch chatter or a delayed second click can no longer activate an underlying main-window action, while clicks away from the close point remain immediately available.
- **Launcher confirmation on every user close**: MineHarbor now asks before closing even while the server is off. An idle close exits without running the server-stop process, while in-progress work and a running server use separate copy explaining deferred completion and safe server shutdown.
- **Close-flow regression coverage**: Tests now cover minimum and maximum guard duration, coordinate boundaries, repeated clicks and expiry, all three close-question modes, and the non-deferred idle path.

## [1.7.3] - 2026-07-24

### Korean

- **빠른 명령 반응형 레이아웃**: 콘솔이 닫혀 있을 때 빠른 명령 카드가 작업 영역 전체 폭을 사용하고, 콘솔을 열면 읽을 수 있는 최소 폭을 보장하는 보조 패널로 전환됩니다. 영어 상태·안내·명령 관리 문구와 런처 업데이트 버튼의 불필요한 말줄임표를 제거했습니다.
- **콘솔 탐색 흐름 개선**: 검색, 로그 분류, 줄 바꿈을 왼쪽부터 작업 순서대로 배치하고 한국어·영어 문구가 잘리지 않도록 폭을 조정했습니다. 시작 준비가 끝난 뒤 기본 작업인 서버 시작으로 키보드 포커스를 이동합니다.
- **자동완성 범위 확대**: 메인 콘솔에는 기본 서버 명령과 연결된 플레이어 인수, 예약 명령 편집에는 실행 시점과 무관한 기본 명령 자동완성을 추가했습니다. 방향키, Tab, Enter, Esc와 스크린 리더 설명을 기존 관리 콘솔과 동일하게 제공합니다.
- **UX 회귀 검증**: 실제 Windows 125% DPI에서 한국어·영어, 다크·라이트, 콘솔 열림·닫힘을 확인했습니다. 제한 없는 실제 글자 폭, 반응형 패널 상태, 콘솔 도킹 순서와 자동완성 연결을 검사하는 26번째 런처 테스트 그룹을 추가했습니다.

### English

- **Responsive quick-command workspace**: The quick-command card now uses the full workspace while the console is closed, then becomes a readable minimum-width companion panel when the console opens. Unnecessary ellipses are gone from English status, guidance, command-management, and launcher-update text.
- **Improved console flow**: Search, log category, and word wrap now follow the task order from left to right, with enough room for both languages. Keyboard focus moves to the primary Start action after startup preparation completes.
- **More command completion**: The main console now suggests common commands and connected-player arguments, while the scheduled-command editor suggests time-independent common commands. Both use the same arrow, Tab, Enter, Esc, and screen-reader behavior as the managed console.
- **UX regression coverage**: Verified Korean/English, dark/light, and open/closed console states on Windows at 125% DPI. A 26th launcher test group measures unconstrained text width and checks responsive panel state, console docking order, and completion wiring.

## [1.7.2] - 2026-07-23

### Korean

- **보조 창 닫기 관통 차단 보강**: 제목 표시줄 X의 비클라이언트 입력이 시작되는 순간부터 주 창의 마우스 활성화를 거부하고, 같은 물리 입력의 클라이언트·비클라이언트·X 버튼·포인터 누름/해제를 창 종료까지 소비합니다. 창이 닫힌 뒤 다음 독립 클릭은 즉시 허용하며, 누락된 시스템 메시지에는 제한 시간 복구를 적용합니다.
- **자체서명 전용 릴리스**: 릴리스마다 임시 RSA-3072/SHA-256 코드 서명 인증서와 난수 비밀번호를 생성해 Portable EXE와 설치 프로그램을 자체서명하고, PFX와 인증서 저장소 항목을 항상 삭제합니다. 공개 자산 검증은 동일 자체서명 주체와 Authenticode 무결성을 확인합니다.
- **회귀 검증 확대**: 제목 표시줄 닫기 감지, 주 창 활성화 거부, 누름·해제 관통 차단, 다음 클릭 복구 및 임시 인증서 잔여물 제거를 자동 검증합니다.

### English

- **Stronger tool-window close isolation**: Starts protection at the title-bar X non-client input, rejects main-window mouse activation, and consumes the matching client, non-client, X-button, and pointer press/release until the tool window closes. The next independent click is allowed immediately, with a bounded timeout for missing system messages.
- **Self-signed-only releases**: Creates a one-release RSA-3072/SHA-256 code-signing certificate with a random password, self-signs the Portable EXE and installer, and always removes the PFX and certificate-store entry. Published-asset verification checks the expected self-signed subject and Authenticode integrity.
- **Expanded regressions**: Covers title-bar close detection, main-window activation rejection, press/release click-through blocking, next-click recovery, and cleanup of temporary certificate material.

## [1.7.1] - 2026-07-22

### Korean

- **보조 창 다크 모드 수정**: 콘솔·업데이트·메시지·콘텐츠·백업·네트워크·플레이어 창의 Windows 11 제목 표시줄과 테두리를 현재 앱 테마에 연결하고, 창 핸들이 다시 만들어져도 테마를 재적용합니다.
- **네이티브 컨트롤 테마 통합**: 리치 텍스트, 목록·체크 목록, 표 헤더와 스크롤바가 다크 팔레트를 사용하도록 공통 테마 경로를 보강했습니다. 핸들이 아직 없는 컨트롤도 생성 시점에 원하는 테마를 적용합니다.
- **콘솔과 프레임 정돈**: 메인·관리 콘솔의 기본 외곽선을 제거하고 검색·명령 입력을 둥근 입력 표면으로 통일했습니다. 관리 콘솔 출력이 도구막대·명령 영역 뒤로 겹치지 않게 했고, 빠른 명령 관리의 기본 GroupBox를 둥근 현대형 프레임으로 교체했습니다.
- **회귀 검증**: 보조 창의 다크 배경, 목록·입력·그룹 테두리, 표 헤더와 기본 외곽선 제거를 실제 팔레트 값으로 검증합니다. 25개 런처 테스트 그룹과 10개 브리지 프로토콜 테스트를 통과했습니다.

### English

- **Fixed dark mode in secondary windows**: Connected Windows 11 title bars and borders for console, update, message, content, backup, network, and player windows to the current app theme, including handle recreation.
- **Unified native-control theming**: Rich text, list/check-list, table headers, and scrollbars now use the dark palette. Controls without a handle receive the requested theme when their handle is created.
- **Refined console and group frames**: Removed classic console borders, moved main-console search and command fields onto rounded input surfaces, prevented managed-console output from overlapping its toolbars, and replaced native quick-command GroupBox frames with a rounded modern control.
- **Regression coverage**: Added palette-value checks for secondary-window backgrounds, lists, input/group borders, table headers, and classic-border removal. All 25 launcher test groups and 10 bridge protocol tests pass.

## [1.7.0] - 2026-07-20

### Korean

- **현대형 테마 컨트롤**: 다크·라이트 팔레트를 공유하는 둥근 체크박스, 드롭다운, 탭, 목록 헤더·행과 상태 표 구분선을 추가했습니다. 고정 격자와 항상 표시되던 대시보드 스크롤을 제거하고 키보드 포커스·스크린 리더 정보를 유지했습니다.
- **플레이어·명령 자동완성**: 플레이어 관리 화면에서 연결된 플레이어 이름을 제안하고, 멀티 서버 콘솔에서 기본 명령과 온라인 플레이어 인수를 위아래 방향키 및 Tab/Enter로 완성할 수 있습니다.
- **외부 포트 판정 수정**: 일반 TCP 포트 검사 결과를 Minecraft 서버 확인으로 오인하지 않도록 `서버 일치 미확인` 상태를 도입했습니다. MineHarbor가 생성한 UPnP 매핑의 사후 검사만 `확인됨`으로 표시하고 외부 검사 요청의 캐시를 차단합니다.
- **휴지통 UX 개선**: 서버 이름 입력은 휴지통으로 보내는 단계에만 유지하고 연한 서버명 예시를 표시합니다. 휴지통의 영구 삭제는 이름 재입력 대신 3초 동안 잠긴 테마 확인 창을 사용합니다.
- **창 닫기 안전성**: 모델리스 도구 창의 제목 표시줄 X를 눌러 닫을 때 같은 위치의 메인 창 버튼이 함께 눌리지 않도록 짧은 마우스 메시지 보호 구간을 추가했습니다.
- **검증**: 자동완성, 보수적 외부 상태, 3초 확인, 현대형 상태 표와 클릭 관통 보호 회귀를 포함한 25개 런처 테스트 그룹 및 10개 브리지 프로토콜 테스트를 통과했습니다. 네트워크 테스트는 실제 공유기 대신 루프백 가짜 UPnP 장치와 가짜 COM을 사용합니다.

### English

- **Modern themed controls**: Added rounded checkboxes, dropdowns, tabs, list headers/rows, and metric separators sharing the dark/light palette. Removed the fixed dashboard grid and always-enabled scrolling while preserving keyboard focus and screen-reader metadata.
- **Player and command completion**: Player management now suggests connected player names, while managed-server consoles complete common commands and online-player arguments with Up/Down and Tab/Enter.
- **Correct external-port classification**: Generic TCP results are now reported as `server identity unverified` instead of being treated as proof of Minecraft reachability. Only post-checks for UPnP mappings created by MineHarbor become verified, and external-check requests bypass caches.
- **Safer Trash UX**: Exact-name input remains only when moving a server to Trash and shows a light server-name cue. Permanent deletion inside Trash now uses a themed confirmation locked for three seconds instead of asking for the name again.
- **Close-button safety**: Added a short mouse-message guard so closing a modeless tool with its title-bar X cannot also click a main-window button underneath.
- **Verification**: Passed 25 launcher test groups and 10 bridge protocol tests, including completion, conservative external status, timed confirmation, modern metric layout, and click-through guards. Network tests use loopback fake UPnP devices and fake COM instead of a real router.

## [1.6.0] - 2026-07-20

### Korean

- **통합 콘텐츠 관리**: `.mineharbor/content-manifest.json`을 도입해 설치된 플러그인·모드·데이터팩을 수동 파일과 구분하고, 호환 버전·로더 및 필수 의존성을 검사한 검색·설치·개별/일괄 업데이트·비활성화·복구 가능한 제거를 추가했습니다.
- **데이터팩 검증**: Vanilla, Paper, Purpur와 직접 JAR 프로필의 월드를 찾아 `world/datapacks`에 설치하며, 루트 `pack.mcmeta`, 안전한 ZIP 경로, 중복 항목, 압축 파일 수와 해제 크기를 검증합니다.
- **서버 자동화**: `.mineharbor/automation.json`에 서버별 정기 백업, 시작 전·종료 후 백업, 예약 시작·종료·재시작·명령, 플레이어 사전 공지, 다음/최근 실행 결과와 개수·기간·총용량 보존 정책을 저장합니다. MineHarbor 관리 창이 실행 중일 때 일정을 평가하며, 원자적 실행 임대로 중복 실행과 만료된 실행 상태를 처리합니다.
- **운영 대시보드**: 서버 상태·가동 시간, Java CPU·메모리·버전, 플레이어, 서버·월드·백업 용량, 최근 경고·오류, 외부 접속 결과와 다음 예약을 표시합니다. Paper/Purpur 브리지가 실제 제공한 경우에만 TPS/MSPT를 표시합니다.
- **비동기·UI 구조 개선**: 콘텐츠·자동화·상태 수집을 별도 partial 서비스/화면으로 분리하고, 장시간 작업에 `Task`, `async/await`, `CancellationToken`, 진행률과 닫힌 UI 콜백 차단을 적용했습니다.
- **빌드와 검증 강화**: PR/main CI와 `net48` SDK 스타일 병행 프로젝트, 버전·문서 일치 검사, Portable EXE·브리지 검증을 추가하고 손상 manifest, 해시 불일치, 의존성 순환, 잘못된 데이터팩, 중복 일정, 백업 실패, 브리지 연결 해제와 UI 종료 회귀를 포함한 24개 테스트 그룹을 통과했습니다.

### English

- **Unified content management**: Added `.mineharbor/content-manifest.json`, managed/manual distinction, compatibility and required-dependency checks, search/install, individual or batch updates, enable/disable, and recoverable removal for plugins, mods, and data packs.
- **Data-pack validation**: Discovers worlds for Vanilla, Paper, Purpur, and custom-JAR profiles, installs into `world/datapacks`, and validates root `pack.mcmeta`, safe ZIP paths, duplicates, entry counts, and expanded size.
- **Server automation**: Stores per-server recurring and start/stop-hook backups, scheduled start/stop/restart/commands, player warnings, next/latest results, and count/day/size retention in `.mineharbor/automation.json`. Schedules are evaluated while a MineHarbor management window is running; atomic execution leases prevent duplicate jobs and recover expired runs.
- **Operations dashboard**: Shows status, uptime, Java CPU/memory/version, players, server/world/backup size, recent warnings/errors, external-access results, and the next job. TPS/MSPT appear only when actually supplied by the Paper/Purpur bridge.
- **Async and UI structure**: Split content, automation, and status collection into dedicated partial services/forms and applied `Task`, `async/await`, cancellation, progress, and closed-UI callback guards to long operations.
- **Build and verification**: Added PR/main CI, a parallel SDK-style `net48` project, version/document consistency checks, Portable and bridge validation, and 24 test groups covering corrupt manifests, hash mismatch, dependency cycles, invalid data packs, duplicate schedules, backup failures, bridge disconnects, and closed UIs.

## [1.5.23] - 2026-07-20

### Korean

- **자동 업데이트와 명령 브리지 복구**: GitHub Release 별칭에서 CDN까지 이어지는 리디렉션을 허용 호스트·저장소·버전·파일명으로 검증하고, 메타데이터와 바이너리 크기를 제한해 실제 공개 브리지 설치와 이전 런처 자동 업데이트가 모두 동작하도록 수정했습니다.
- **네트워크·이미지 보안 강화**: Modrinth API와 CDN 응답의 리디렉션·크기를 제한하고, 아이콘 변환 URL을 안전하게 인코딩하며 디코딩된 이미지의 가로·세로와 전체 픽셀 수를 제한했습니다.
- **UI·UX·접근성 개선**: 영어 업데이트 화면에 영어 릴리스 노트를 표시하고, 보조 창 DPI 배율·버튼 줄바꿈·타이머 해제를 보완했으며, 입력·목록·콘솔의 스크린 리더 이름과 영어 빠른 명령 문구를 개선했습니다.
- **빌드 공급망 강화**: GitHub Actions를 전체 커밋 SHA로 고정하고 체크아웃 자격 증명 유지를 끄며, 코드 서명 비밀을 필요한 단계로만 제한하고 임시 PFX를 항상 정리하도록 변경했습니다.
- **검증과 유지보수**: UPnP 반복 시작·중단, 매핑 소유권·대체 포트·지연 정리 회귀를 포함한 22개 런처 테스트와 8개 브리지 프로토콜 테스트를 통과했으며, 깨진 변경 이력 인코딩과 오래된 일회성 스크립트를 정리했습니다.

### English

- **Restored automatic updates and bridge downloads**: Validated redirects from GitHub Release aliases through the CDN by host, repository, version, and filename, bounded metadata and binaries, and restored both public bridge installation and updates from older launchers.
- **Hardened network and image handling**: Bounded redirects and response sizes for the Modrinth API and CDN, safely encoded image-proxy URLs, and limited decoded image dimensions and total pixel count.
- **Improved UI, UX, and accessibility**: Displayed English release notes in the English update dialog, improved secondary-window DPI scaling and button wrapping, disposed timers, and added screen-reader names for inputs, lists, and consoles.
- **Hardened the build supply chain**: Pinned GitHub Actions to full commit SHAs, disabled persisted checkout credentials, scoped signing secrets to required steps, and always removed temporary PFX files.
- **Expanded verification and maintenance**: Passed 22 launcher test groups—including repeated UPnP start/stop, ownership, alternate-port, and delayed-cleanup regressions—plus 8 bridge protocol tests, and repaired corrupted changelog history and obsolete one-off scripts.

## [1.5.22] - 2026-07-20

### Korean

- 기존 저장소 별칭 URL을 메타데이터에 유지해 `v1.5.20` 이하 런처의 자동 업데이트 호환을 복구했습니다.
- 새 런처는 정식 저장소와 이전 별칭의 엄격한 GitHub Release 경로만 허용합니다.

### English

- Restored automatic updates for launchers through `v1.5.20` by retaining the legacy repository-alias URL in release metadata.
- New launchers accept only strict GitHub Release paths from the canonical repository or its legacy alias.

## [1.5.21] - 2026-07-20

### Korean

- 직접 SSDP/SOAP UPnP를 기본 경로로 완성하고 Windows COM 백업과 최대 8개 대체 외부 포트를 추가했습니다.
- 실행 세대·취소·소유권 검증으로 반복 시작·중단과 지연 정리 경합을 안정화했습니다.

### English

- Completed direct SSDP/SOAP UPnP as the primary path, retained Windows COM fallback, and added up to eight alternate external ports.
- Stabilized repeated start/stop and delayed cleanup races with generations, cancellation, and exact ownership checks.

## [1.5.20] - 2026-07-18

### Korean

- UPnP 실행별 소유권, 외부 접속 진단, Forge/Inno Setup 검증, 백업 압축 해제 제한과 위험 명령 확인을 강화했습니다.

### English

- Hardened per-run UPnP ownership, external-access diagnostics, Forge/Inno Setup verification, backup extraction limits, and dangerous-command confirmation.

## 이전 릴리스 요약 / Earlier release summary

| 버전 | 날짜 | 한국어 | English |
|---|---|---|---|
| 1.5.19 | 2026-07-18 | UPnP 매핑 소유권과 외부 접속 복구, 공급망·백업 보안을 강화했습니다. | Hardened UPnP ownership and external-access recovery, plus supply-chain and backup security. |
| 1.5.18 | 2026-07-16 | 불완전한 소켓 UPnP를 안정적인 COM 경로로 되돌리고 타이틀 바와 버튼 정렬을 수정했습니다. | Reverted incomplete socket UPnP to the stable COM path and fixed title-bar and button alignment. |
| 1.5.17 | 2026-07-16 | 시작 중 HWND 재생성 뒤에도 DWM 타이틀 바 테마가 유지되도록 수정했습니다. | Preserved DWM title-bar theming after startup HWND recreation. |
| 1.5.16 | 2026-07-16 | Windows 11 기본 타이틀 바를 앱 테마와 동기화하면서 스냅·DPI 동작을 유지했습니다. | Synchronized the Windows 11 native title bar with the app theme while preserving snap and DPI behavior. |
| 1.5.15 | 2026-07-15 | 소켓 기반 UPnP, 고아 매핑 추적, 안전한 XML 파싱과 비동기 탐색을 도입했습니다. | Introduced socket-based UPnP, orphan tracking, safe XML parsing, and asynchronous discovery. |
| 1.5.14 | 2026-07-15 | 콘텐츠의 WebP·SVG 아이콘을 호환 PNG로 변환해 표시했습니다. | Added compatible PNG conversion for WebP and SVG content icons. |
| 1.5.13 | 2026-07-15 | 다운로드·TLS·CRLF 보안, 공인 IP 백업 서비스, 업데이트 복구 UX를 강화했습니다. | Hardened downloads, TLS, and CRLF handling, added public-IP fallbacks, and improved update recovery UX. |
| 1.5.12 | 2026-07-15 | 업데이트 무결성, Job Object, 강제 종료 예외와 UI 비동기 처리를 개선했습니다. | Improved update integrity, Job Objects, force-stop exceptions, and asynchronous UI handling. |
| 1.5.11 | 2026-07-15 | 프로세스 수명 주기와 업데이트 안정성을 정비하고 환경 변수 삽입과 강제 종료 교착을 수정했습니다. | Refactored process lifecycle and update stability, and fixed environment-variable injection and force-stop deadlocks. |
| 1.5.10 | 2026-07-15 | 라이트 모드 보조 버튼 대비와 Pretendard 기본 글꼴 크기를 개선했습니다. | Improved secondary-button contrast in light mode and adjusted the Pretendard base size. |
| 1.5.9 | 2026-07-15 | 전체 UI 글꼴을 Pretendard로 통일했습니다. | Standardized the UI font on Pretendard. |
| 1.5.8 | 2026-07-15 | 서버 실행 중 창 닫기 확인이 건너뛰어지던 문제를 수정했습니다. | Restored exit confirmation while a server is running. |
| 1.5.7 | 2026-07-15 | Job Object, 최대 10초 정상 종료 대기와 중복 종료 방지를 추가했습니다. | Added Job Objects, up to 10 seconds of graceful-shutdown waiting, and duplicate-stop prevention. |
| 1.5.6 | 2026-07-14 | WebP 콘텐츠 아이콘의 프록시 변환 호환성을 수정했습니다. | Fixed proxy conversion compatibility for WebP content icons. |
| 1.5.5 | 2026-07-14 | 느린 공유기를 위해 UPnP 연결·확인 제한 시간을 조정했습니다. | Adjusted UPnP connection and verification timeouts for slower routers. |
| 1.5.4 | 2026-07-14 | 콘텐츠 목록을 빠르게 탐색할 때 아이콘 로딩이 멈추던 교착을 수정했습니다. | Fixed an icon-loading deadlock during rapid content browsing. |
| 1.5.3 | 2026-07-14 | 직접 설정 중심 흐름, 월드 유형, 언어·테마 전환과 상태 표시 렌더링을 개선했습니다. | Improved direct setup, world type, language/theme switching, and status rendering. |
| 1.5.2 | 2026-07-14 | 간헐적 시작 충돌과 업데이트 안내 언어 선택을 수정했습니다. | Fixed an intermittent startup crash and localized update notes. |
| 1.5.1 | 2026-07-14 | 좁은 창의 관리 버튼, 다크 모드 입력 테두리와 업데이트 버전 판별을 수정했습니다. | Fixed narrow-window management buttons, dark input borders, and update version detection. |
| 1.5.0 | 2026-07-14 | 로컬 릴리스·코드 서명 도구와 둥근 UI 성능·접근성 개선을 추가했습니다. | Added local release and code-signing tools plus rounded-UI performance and accessibility improvements. |
| 1.4.0 | 2026-07-14 | Windows 기본 알림을 MineHarbor 공통 대화상자로 교체하고 버튼·키보드 동작을 통일했습니다. | Replaced native alerts with shared MineHarbor dialogs and standardized buttons and keyboard behavior. |
| 1.3.2 | 2026-07-14 | 업데이트 파일 최소 크기 제약을 완화하면서 구버전 호환 크기를 유지했습니다. | Relaxed update minimum-size checks while retaining legacy compatibility sizing. |
| 1.3.1 | 2026-07-14 | 작은 신규 런처 파일을 구버전이 거부하던 자동 업데이트 호환 문제를 수정했습니다. | Fixed old launchers rejecting smaller new update binaries. |
| 1.3.0 | 2026-07-14 | 수동 업데이트 확인, 버전 무시, 외부 Java 다운로드와 변경 이력 기반 릴리스 노트를 추가했습니다. | Added manual update checks, version ignore, external Java downloads, and changelog-driven release notes. |
| 1.2.1 | 2026-07-14 | UPnP COM을 STA 스레드로 고정하고 탐색·매핑 재시도와 결과 확인을 강화했습니다. | Moved UPnP COM to an STA thread and strengthened discovery, mapping retries, and verification. |
| 1.2.0 | 2026-07-13 | 관리 창을 모델리스로 전환하고 다중 서버 포트 충돌·상태 복구를 개선했습니다. | Made management windows modeless and improved multi-server port conflicts and state recovery. |
| 1.1.0 | 2026-07-13 | 빠른 명령 후보의 Tab·Shift+Tab·Enter 키보드 흐름을 개선했습니다. | Improved Tab, Shift+Tab, and Enter navigation for quick-command suggestions. |
| 1.0.0 | 2026-07-13 | MineHarbor 이름, 서버 휴지통과 기존 데이터·업데이트 호환 경로를 도입했습니다. | Introduced the MineHarbor name, server trash, and legacy data/update compatibility paths. |
| 0.4.2 | 2026-07-13 | 검색 가능한 3단계 빠른 명령 선택창과 테마 대응 스크롤을 추가했습니다. | Added a searchable three-level quick-command picker and theme-aware scrolling. |
| 0.4.1 | 2026-07-13 | 서버 관리 버튼의 한국어·영어 글자 잘림을 수정했습니다. | Fixed Korean and English text clipping in server-management buttons. |
| 0.4.0 | 2026-07-13 | 기본·사용자 빠른 명령과 루프백 전용 Paper/Purpur 실시간 브리지를 추가했습니다. | Added built-in and user quick commands plus an optional loopback-only Paper/Purpur live bridge. |
| 0.3.3 | 2026-07-12 | 내장 Paper JAR을 제거하고 공식 최신 빌드·SHA-256 검증 경로로 통일했습니다. | Removed the bundled Paper JAR and standardized on verified official latest builds. |
| 0.3.2 | 2026-07-12 | 벡터 아이콘, 테마 대응 컨트롤과 더 선명한 설정 계층을 도입했습니다. | Introduced vector icons, theme-aware controls, and a clearer setup hierarchy. |
| 0.3.1 | 2026-07-12 | 반응형 배치, 고대비·키보드·스크린 리더 접근성과 입력 검증을 개선했습니다. | Improved responsive layout, high-contrast, keyboard and screen-reader accessibility, and validation. |
| 0.3.0 | 2026-07-11 | 재현 가능한 Portable·설치 빌드, 데이터 위치 선택, 검증된 자동 업데이트와 릴리스 자동화를 도입했습니다. | Introduced reproducible portable/installer builds, selectable data storage, verified updates, and release automation. |
