# 개인정보 처리 안내 / Privacy

## 한국어

MineHarbor — Minecraft Server Launcher는 자동 사용 통계나 분석 정보를 수집하지 않으며, 오류 로그나 진단 묶음을 자동으로 전송하지 않습니다. 진단 묶음은 사용자가 직접 생성하고 공유할 때만 외부로 전달됩니다.

런처는 다음 기능을 위해 네트워크에 접속합니다.

- GitHub Releases: 런처 업데이트 정보와 사용자가 승인한 업데이트 파일 다운로드
- PaperMC Fill API, Purpur API: 서버 버전과 서버 JAR 확인 및 다운로드
- Mojang 버전 메타데이터 및 다운로드 서버: Vanilla 서버 버전과 파일 확인
- Fabric Meta, Forge 파일/Maven, NeoForge Maven: 해당 서버 로더 확인 및 다운로드
- Eclipse Adoptium API와 GitHub 릴리스 자산: 호환 Java 런타임 확인 및 다운로드
- Modrinth API/CDN: 플러그인·모드·데이터팩 검색, Minecraft 버전·로더·의존성 확인, 아이콘 표시와 선택한 콘텐츠 다운로드
- wsrv.nl: Windows가 직접 표시하지 못하는 Modrinth SVG/WebP 아이콘을 최대 256×256 PNG로 변환하며, 해당 공개 아이콘 URL만 전달
- portchecker.io: 공인 IP 확인과 사용자가 시작한 외부 TCP 포트 응답 검사. 이 결과만으로 Minecraft 서버 일치 여부를 확정하지 않음
- 로컬 공유기 UPnP: 외부 접속 검사 실패 후에만 자동 포트 매핑 시도
- playit.gg 문서: 사용자가 해당 안내 버튼을 선택했을 때 브라우저로 열기
- Discord API/Gateway: 사용자가 Discord 원격 제어를 별도로 켠 경우에만 길드 전용 명령 등록, 상호작용 수신과 응답

Paper/Purpur 실시간 명령 브리지는 인터넷이나 LAN에 연결하지 않습니다. 런처가 실행마다 만든 임시 포트의 `127.0.0.1` 리스너에만 연결하며, 무작위 세션 토큰·프로필 이름·프로토콜 버전을 확인합니다. 세션 파일과 토큰은 서버가 종료되면 삭제되고 진단 묶음에 포함되지 않으며 로그에도 기록하지 않습니다. 브리지 JAR 설치·업데이트를 사용자가 선택한 경우에만 GitHub Release에서 자산을 내려받아 크기와 SHA-256을 검증합니다.

백그라운드 운영(베타)은 사용자가 명시적으로 켠 경우에만 현재 Windows 사용자 계정에서 실행됩니다. GUI와 에이전트의 통신은 현재 사용자 SID에만 권한을 부여한 로컬 이름 있는 파이프를 사용하며 인터넷이나 LAN 포트를 열지 않습니다. 활성화·Windows 로그인 자동 시작·충돌 재시작·일시 중지 설정은 사용자 데이터 폴더의 `background-agent.json`에 저장됩니다. 자동 시작을 별도로 선택하면 실행 파일 경로와 `--background-agent` 인수만 현재 사용자 Windows `Run` 항목에 기록되며 토큰이나 서버 명령은 기록하지 않습니다.

멀티 서버 관리가 시작한 각 관리 자식은 실행 중 서버를 에이전트에 안전하게 인계하기 위해 현재 사용자 SID 전용 로컬 이름 있는 파이프와 실행마다 새 256비트 토큰을 사용합니다. 이 채널은 인터넷이나 LAN 포트를 열지 않으며 콘솔 명령과 제한된 최근 로그만 전달합니다. 토큰은 자식 프로세스 인수와 실행 중 메모리에만 존재하고 설정 파일·운영 기록·로그·진단 묶음에 저장되지 않습니다.

Windows 작업 표시줄 알림은 기본적으로 꺼져 있으며 사용자가 별도로 켠 경우에만 백그라운드 에이전트가 표시합니다. 중요도·종류·조용한 시간 설정은 사용자 데이터 폴더의 `windows-notifications.json`에 로컬로 저장됩니다. 알림은 새 운영 기록의 한국어 또는 영어 요약만 사용하며 명령 원문을 표시하지 않습니다. 서버 절대 경로, IPv4 주소와 토큰·비밀번호·웹훅처럼 보이는 값은 표시 전에 다시 가리고, MineHarbor 서버로 전송하지 않습니다.

Discord 원격 제어(베타)는 기본적으로 꺼져 있으며 사용자가 직접 만든 봇, 대상 Discord 서버·채널, 허용 사용자 또는 역할과 허용 MineHarbor 서버를 지정한 경우에만 백그라운드 에이전트가 Discord API와 Gateway에 연결합니다. 봇 토큰은 사용자 데이터 폴더의 `discord-remote.json`에 현재 Windows 사용자 범위 DPAPI 암호문으로 저장하며 화면·로그·운영 기록·진단 묶음에는 출력하지 않습니다. 애플리케이션·Discord 서버·채널·사용자·역할 ID와 허용 서버 이름은 같은 로컬 설정에 저장됩니다.

Discord는 명령을 처리하기 위해 호출한 사용자와 역할, Discord 서버·채널 ID, 선택한 MineHarbor 서버 이름과 명령 종류를 받습니다. MineHarbor의 응답에는 요청에 따라 서버 상태·가동 시간, 브리지 연결 시 온라인 플레이어 이름, 또는 가림 처리한 최근 경고·오류가 포함될 수 있습니다. 안전 종료·재시작·시작·백업 결과도 Discord에 반환됩니다. 임의 콘솔 명령, IP 주소, 전체 사용자 경로와 토큰을 전송하지 않으며 응답의 Discord 멘션은 비활성화합니다. 변경 작업의 로컬 운영 기록에는 전체 Discord 사용자 ID 대신 끝 4자리만 남깁니다.

설치 콘텐츠 기록은 각 서버의 `.mineharbor/content-manifest.json`, 백업·재시작·명령 일정과 최근 실행 결과는 `.mineharbor/automation.json`에 로컬로 저장됩니다. 서버 시작·종료·충돌·자동 재시작과 예약 결과는 서버별 `.mineharbor/operations-history.json`에 최대 500개까지 저장됩니다. 운영 기록은 절대 서버 경로, IPv4 주소와 토큰·비밀번호·웹훅처럼 보이는 값을 가리고 SHA-256 연속 해시로 변경 여부를 검사하지만, 사용자가 CSV 내보내기를 선택하면 표시 중인 서버 이름과 운영 문구가 선택한 파일에 포함됩니다. Paper/Purpur 복사 호환성 설정을 변경하면 기존 YAML은 서버의 `.mineharbor/configuration-backups`에 최대 5개까지 로컬 보관됩니다. 이 파일과 대시보드의 CPU·메모리·플레이어·용량·오류·TPS/MSPT 값은 원격 분석 서버로 전송되지 않습니다. TPS/MSPT는 연결된 Paper/Purpur 브리지가 공개 서버 API에서 얻을 수 있을 때만 로컬 루프백으로 전달합니다.

공인 IP와 서버 포트는 외부 TCP 응답을 확인하기 위해 portchecker.io에 요청될 수 있습니다. 이 검사는 응답한 서비스가 현재 Minecraft 서버인지 식별하지 못하므로 MineHarbor는 결과를 별도 미확인 상태로 표시합니다. 런처는 공유기 관리 비밀번호를 읽거나 전송하지 않습니다.

진단 묶음에는 운영체제 버전, 런처 제품 버전과 빌드 번호, CPU 논리 코어 수, 총 메모리, 서버 종류·Minecraft 버전·설정 파일, 최근 서버 로그와 최대 3개의 충돌 보고서가 포함될 수 있습니다. 사용자 프로필 경로, 서버 절대 경로, IPv4 주소, 서버 소유자 이름, RCON 비밀번호, 서버 IP와 일부 민감 설정은 제거하거나 대체합니다. 파일 크기가 지나치게 크거나 재분석 지점인 파일은 포함하지 않습니다.

자동 업데이트는 `Mangom72/MineHarbor` GitHub Release의 정식 경로나 기존 저장소 별칭에서만 내려받고, 메타데이터의 크기와 SHA-256을 모두 확인합니다. 공개 릴리스 실행 파일은 릴리스마다 생성하고 폐기하는 자체서명 인증서로 무결성을 표시하지만 공개 인증 기관의 신뢰 서명은 아닙니다.

## English

MineHarbor — Minecraft Server Launcher does not collect analytics or usage telemetry. It does not automatically upload errors, logs, or diagnostic bundles. A diagnostic bundle leaves the computer only when the user explicitly shares it.

The launcher accesses GitHub Releases for launcher updates; PaperMC, Purpur, Mojang, Fabric, Forge, and NeoForge services for server metadata and files; Eclipse Adoptium and its GitHub release assets for Java; Modrinth for plugin, mod, and data-pack search, compatibility, dependencies, icons, and downloads; wsrv.nl to convert public Modrinth SVG/WebP icon URLs to PNG images capped at 256×256; and portchecker.io for public-IP and external-TCP checks initiated by the launcher. A generic TCP result cannot identify the responding Minecraft server, so MineHarbor reports it as unverified. UPnP discovery happens only after an external connection check fails. The launcher does not read or transmit router administrator passwords.

Diagnostic bundles may include OS information, launcher product/build versions, logical processor count, total memory, server type and version, redacted settings, recent logs, and up to three crash reports. User-profile paths, absolute server paths, IPv4 addresses, owner names, RCON passwords, server IP values, and selected sensitive settings are removed or replaced. Oversized files and reparse points are excluded.

Launcher updates are downloaded only from the canonical or legacy-alias paths of the `Mangom72/MineHarbor` GitHub Release and are checked against both the declared size and SHA-256. Public release executables carry a per-release self-signed integrity signature whose certificate is then discarded; it is not a public-CA trust signature.

The optional Paper/Purpur command bridge never connects to the internet or LAN. It connects only to the launcher's temporary `127.0.0.1` listener and validates a fresh random session token, profile, and protocol version. The session file is deleted when the server stops, the token is not logged, and the session file is excluded from diagnostic bundles. The bridge JAR is downloaded from the GitHub Release only after user consent and is verified by size and SHA-256.

Background operations (Beta) runs in the current Windows user account only after explicit opt-in. GUI/agent communication uses a local named pipe whose ACL grants only the current user SID and opens no LAN or internet port. Enablement, optional Windows sign-in startup, crash restart, and pause state are stored in `background-agent.json` under user data. If sign-in startup is separately selected, only the executable path and `--background-agent` argument are stored in the current-user Windows `Run` entry; no token or server command is stored there.

Each managed child started by multi-server management uses a current-user-SID-only local named pipe and a fresh 256-bit token when transferring a running server safely to the agent. This channel opens no LAN or internet port and carries only console commands and a bounded recent-log buffer. The token exists only in the child process arguments and live process memory; it is not stored in settings, operations history, logs, or diagnostic bundles.

Windows taskbar notifications are disabled by default and appear only after separate opt-in while the background agent is running. Severity, category, and quiet-hour preferences are stored locally in `windows-notifications.json` under user data. Notifications contain only the Korean or English summary of a new operation and never include a raw command. Absolute server paths, IPv4 addresses, and token/password/webhook-like values are sanitized again before display and are not sent to a MineHarbor server.

Discord remote control (Beta) is disabled by default. The background agent connects to Discord API/Gateway only after the user supplies their own bot, target guild/channel, allowed users or roles, and allowed MineHarbor profiles. The bot token is persisted only as current-user Windows DPAPI ciphertext in `discord-remote.json` and is not rendered in the UI after save or written to logs, operations history, or diagnostic bundles. Application, guild, channel, user, role IDs, and approved profile names are stored locally in the same settings file.

Discord receives the invoking user/roles, guild/channel IDs, selected MineHarbor profile name, and command type as part of interaction handling. Depending on the command, MineHarbor may respond with server status and uptime, bridge-backed online player names, redacted recent warnings/errors, or start/backup/safe-stop/restart results. Arbitrary console commands, IP addresses, full user paths, and tokens are not sent, and allowed mentions are disabled in responses. Local history for mutating actions stores only the last four digits of the Discord user ID.

Managed-content records are stored locally in each server's `.mineharbor/content-manifest.json`. Backup, restart, and command schedules plus their latest results are stored in `.mineharbor/automation.json`. Server starts, stops, crashes, automatic restarts, and scheduled-job results retain up to 500 local entries in each server's `.mineharbor/operations-history.json`. Operations history redacts absolute server paths, IPv4 addresses, and values that resemble tokens, passwords, or webhooks and verifies a SHA-256 hash chain; exporting CSV writes the visible server names and operation messages to the file selected by the user. When Paper/Purpur duplication compatibility settings change, up to five prior YAML copies are retained locally under the server's `.mineharbor/configuration-backups`. These files and dashboard CPU, memory, player, storage, error, TPS, and MSPT data are not sent to an analytics service. TPS/MSPT are sent only over local loopback when the connected Paper/Purpur server exposes the corresponding public APIs.
