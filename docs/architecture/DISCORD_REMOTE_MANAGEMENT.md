# Discord 원격 제어 구조 / Discord remote-control architecture

## 한국어

MineHarbor v1.15.1의 Discord 원격 제어는 사용자 계정용 백그라운드 에이전트에 붙는 별도 동의 베타 기능입니다. 사용자가 직접 Discord 애플리케이션과 봇을 만들고 `bot` 및 `applications.commands` 범위로 한 Discord 서버에 설치한 뒤 애플리케이션·서버·채널 ID, 허용 사용자 또는 역할, 허용 MineHarbor 서버를 설정해야 시작됩니다. 관리자 권한 서비스나 공개 수신 포트는 만들지 않습니다.

설정은 `서버 관리 → 백그라운드 → Discord 원격` 또는 에이전트 트레이의 `Discord 원격 제어`에서 엽니다. 보호된 토큰, 유효한 애플리케이션·서버·채널 ID, 허용 사용자 또는 역할과 허용 서버가 모두 없으면 4단계 등록 가이드를 먼저 표시합니다. `설정 시작`만 실제 설정 화면으로 이동하며 `나중에`와 `Esc`는 아무 값도 바꾸지 않습니다. 설정 화면의 `설정 가이드`에서 언제든 다시 열 수 있습니다. 먼저 백그라운드 운영을 켜고 [Discord Developer Portal](https://discord.com/developers/applications)에서 만든 봇의 토큰과 ID를 입력해야 합니다. Portal 버튼은 이 고정된 공식 HTTPS 주소만 엽니다. 토큰 입력란을 비워 저장하면 기존 암호문을 유지하며, `저장된 봇 토큰 제거`를 선택한 경우에만 삭제합니다.

### 연결과 명령 등록

에이전트는 Discord API v10에서 길드 전용 `/mineharbor` 명령을 등록하고 외부 방향 `wss` Gateway 연결을 유지합니다. [Discord 공식 상호작용 문서](https://docs.discord.com/developers/platform/interactions)에 따라 상호작용은 Gateway로 받고 응답은 HTTPS callback/webhook으로 보냅니다. 길드 전용 명령은 해당 길드에 즉시 반영되며, MineHarbor는 자체 명령 하나만 이름 기반 upsert하고 다른 애플리케이션 명령을 일괄 덮어쓰지 않습니다.

Gateway는 Hello의 heartbeat 간격, ACK 누락, sequence, Ready의 session ID와 resume URL, Reconnect와 Invalid Session을 처리합니다. [Discord Gateway 지침](https://docs.discord.com/developers/events/gateway)에 맞춰 가능한 경우 세션을 재개하고 지수형 상한 재연결을 사용합니다. 상호작용은 [3초 초기 응답 제한](https://docs.discord.com/developers/interactions/receiving-and-responding)을 지키기 위해 먼저 비공개 지연 응답으로 승인한 뒤 실제 결과를 원래 응답에 기록합니다. HTTP 429는 `Retry-After`를 따르고 Gateway·HTTP 수신 크기를 제한합니다.

슬래시 명령과 버튼 상호작용은 별도 이벤트 분류이므로 Gateway Intent와 연결되지 않습니다. MineHarbor는 intents 값을 0으로 식별하며 Message Content, Guild Members 또는 Presence 같은 특권 Intent를 요구하지 않습니다. 서버 메시지를 읽거나 일반 채팅 명령을 분석하지 않습니다.

### 인증과 권한

`discord-remote.json`은 기본 비활성화이고 64KiB로 제한합니다. 봇 토큰은 현재 Windows 사용자 범위 DPAPI로 암호화하며 평문은 설정, 로그, 운영 기록과 진단 묶음에 기록하지 않습니다. 파일은 스키마를 정확히 확인하고 경로별 프로세스 간 뮤텍스와 원자적 교체를 사용합니다. 손상 파일과 미래 스키마는 원본을 덮어쓰지 않습니다.

모든 상호작용에서 다음을 다시 확인합니다.

1. 설정한 Discord 애플리케이션 ID
2. 설정한 Discord 서버 ID
3. 설정한 채널 ID
4. 허용 사용자 ID 또는 상호작용 멤버가 가진 허용 역할 ID
5. 허용 MineHarbor 서버 프로필
6. 처리한 상호작용 ID의 재사용 여부
7. 사용자별 최근 1분 요청 수

서버 자동완성은 허용 프로필만 최대 25개 반환합니다. 응답은 최대 1,800자로 제한하고 `allowed_mentions.parse`를 빈 목록으로 보내 로그나 서버 이름에 포함된 문자열이 실제 Discord 멘션을 만들지 않게 합니다.

### 지원 작업과 확인

지원 작업은 `help`, `status`, `players`, `errors`, `start`, `stop`, `restart`, `backup`입니다. `players`는 관리 자식의 Paper/Purpur 브리지가 실제 연결된 경우에만 이름을 표시하며 연결되지 않았으면 지원되지 않는다고 반환합니다. `errors`는 에이전트가 보유한 제한된 최근 로그에서 경고·오류 후보만 최대 5개 골라 서버 절대 경로, IPv4와 비밀값 형태를 가린 뒤 보냅니다.

`stop`과 `restart`는 명령 선택만으로 실행되지 않습니다. 암호학적 난수 확인 ID를 메모리에 만들고 요청 사용자·Discord 서버·채널·프로필·작업에 묶습니다. [Discord 버튼 구성 요소](https://docs.discord.com/developers/components/reference)의 Danger 버튼을 누른 경우에만 실행하며 60초 뒤 만료되고 성공·취소 어느 쪽이든 한 번 사용하면 제거됩니다.

변경 작업은 새 프로세스 탐색을 하지 않고 기존 백그라운드 에이전트의 `StartProfile`, `StopProfile`과 안전 백업 경로만 호출합니다. 에이전트가 소유하지 않은 실행 중 서버는 같은 포트를 사용하더라도 안전 종료·재시작·실행 중 백업을 거부합니다. 임의 콘솔, PowerShell·CMD·실행 파일, 파일 업로드·실행은 Discord API에 노출하지 않습니다.

변경 결과는 서버별 운영 기록에 `discord` 출처로 남기며 전체 Discord 사용자 ID 대신 끝 4자리만 기록합니다. Discord 연결 실패는 상태만 갱신하고 재연결하며 서버 프로세스, 예약 평가, 로컬 IPC와 Windows 알림을 중단시키지 않습니다.

### 검증 범위

자동 검사는 실제 봇 토큰이나 Discord 서버를 사용하지 않습니다. 임시 설정에서 기본 비활성화, DPAPI 평문 비저장·복호화, ID와 프로필 검증, 손상·미래 스키마 보존, 사용자·역할·길드·채널 권한, 서버 자동완성, 확인 소유권·만료·재사용, 사용자별 속도 제한, 임의 명령 차단, 응답 길이, 미등록/등록 가이드 분기와 설정·가이드 UI 접근성을 검사합니다. 실제 Discord API 연결은 사용자가 만든 테스트 길드와 봇에서 설치 권한·방화벽·프록시 정책을 포함해 별도로 확인해야 합니다.

현재 구현은 Discord 이벤트 알림 전송, Discord에서의 임의 콘솔, 웹 원격 관리, 여러 길드·채널 동시 연결과 Discord 계정 자체 관리를 제공하지 않습니다.

## English

MineHarbor v1.15.1 Discord remote control is a separately opted-in beta attached to the per-user background agent. The user must create a Discord application/bot, install it to one guild with the `bot` and `applications.commands` scopes, and configure the application, guild, channel, allowed users or roles, and allowed MineHarbor profiles. No elevated service or public inbound listener is created.

Open the settings from `Server management → Background → Discord remote` or `Discord remote control` in the agent tray. When the protected token, valid application/guild/channel IDs, allowed user or role, and approved profile have not all been registered, a four-step guide appears first. Only `Start setup` advances to settings; `Not now` and `Esc` change nothing. `Setup guide` in the settings footer reopens it later. Enable background operations first, then enter the bot token and IDs created in the [Discord Developer Portal](https://discord.com/developers/applications). The portal button opens only that fixed official HTTPS URL. Leaving the token box blank preserves the existing ciphertext; only the explicit remove-token option deletes it.

The agent upserts one guild-scoped `/mineharbor` command through Discord API v10 and maintains an outbound `wss` Gateway connection. Interactions arrive through the Gateway and are answered through HTTPS callbacks/webhooks. Heartbeat ACKs, sequence numbers, Ready session/resume data, reconnect, invalid sessions, bounded payloads, exponential reconnect, the three-second initial interaction deadline, and HTTP `Retry-After` are handled. Interactions are not tied to a Gateway Intent, so MineHarbor identifies with intents 0 and requests no Message Content, Guild Members, or Presence privileged intent.

`discord-remote.json` is disabled by default and bounded to 64 KiB. The bot token is current-user Windows DPAPI ciphertext and is never written in clear text to settings, logs, operations history, or diagnostics. Exact schema validation, a path-derived cross-process mutex, atomic replacement, and preservation of corrupt/future-schema originals match the other agent settings stores.

Every interaction rechecks the configured application, guild and channel, an allowed user or member role, an allowed MineHarbor profile, interaction replay, and a per-user one-minute throttle. Autocomplete returns at most 25 approved profiles. Responses are capped at 1,800 characters and use an empty allowed-mentions parse list.

The supported actions are `help`, `status`, `players`, `errors`, `start`, `stop`, `restart`, and `backup`. Players are reported only when the managed child's Paper/Purpur bridge is actually connected. Recent errors are selected from the bounded agent log and sanitized. Stop and restart create an in-memory cryptographically random confirmation bound to the requesting user, guild, channel, profile, and action; it expires after 60 seconds and is removed after either confirmation or cancellation.

Mutations reuse only the existing background agent's verified start, safe-stop, restart, and backup paths. A running server that the agent does not own remains untouchable even if its port is listening. No arbitrary console, PowerShell/CMD/executable launch, file upload, or file execution API is exposed. Mutating results are recorded with the `discord` source and only the last four user-ID digits. Integration failure is isolated from the server process, scheduler, local IPC, and Windows notifications.

Automated tests use no real bot token or Discord server. They cover DPAPI storage, validation and corrupt/future preservation, guild/channel/user/role/profile authorization, autocomplete, confirmation ownership/expiry/replay, throttling, arbitrary-command rejection, response bounds, registered/unregistered onboarding routing, and settings/guide UI accessibility. A user-owned test guild remains necessary for an end-to-end Discord installation, firewall, and proxy check.

Outbound Discord notifications, arbitrary remote console, web remote management, multi-guild/channel operation, and Discord-account management are not currently implemented.
