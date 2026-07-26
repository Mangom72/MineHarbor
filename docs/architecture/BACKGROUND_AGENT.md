# 백그라운드 에이전트 / Background Agent

## 한국어

MineHarbor v1.12.0의 백그라운드 운영은 관리자 권한 Windows 서비스가 아니라 현재 사용자 계정에서 실행되는 선택형 베타 기능입니다. 사용자가 `서버 관리 → 백그라운드`에서 명시적으로 켠 경우에만 `MineHarbor.exe --background-agent`가 실행됩니다. 설정은 사용자 데이터 폴더의 `background-agent.json`에 스키마 버전과 함께 원자적으로 저장하며, Windows 로그인 자동 시작은 별도로 동의한 경우에만 현재 사용자 `Run` 항목에 등록합니다.

### 프로세스와 소유권

- GUI와 에이전트는 서로 다른 단일 인스턴스 뮤텍스를 사용하므로 함께 실행할 수 있습니다.
- 에이전트는 자신이 시작했거나 멀티 서버 관리에서 검증된 절차로 인계받은 `--managed-profile` 자식만 제어합니다.
- 관리 자식은 현재 사용자 SID 전용 제어 파이프, 실행별 256비트 토큰과 자식·현재 소유자·새 소유자의 PID 및 프로세스 시작 시각을 확인해 PID 재사용과 추측 인계를 차단합니다.
- 멀티 서버 관리 창을 닫을 때 실행 중 서버를 중단 없이 인계하거나 모두 안전 종료하거나 취소할 수 있습니다. 부분 실패 시 창은 실패 서버를 계속 보유하고 성공한 서버만 에이전트가 관리합니다.
- 에이전트가 정상 종료될 때는 모든 소유 서버에 `stop`을 보내고 제한 시간 안에 종료됐을 때만 에이전트를 끝냅니다.
- 부모 프로세스가 예상치 않게 사라진 관리 자식도 Java 프로세스에 먼저 `stop`을 보내고 종료를 기다립니다.
- 다른 MineHarbor 창이나 사용자가 직접 실행해 같은 포트를 사용하는 서버는 외부 소유로 표시하며, 명령·종료·실행 중 백업을 수행하지 않습니다.

### IPC 보안

GUI와 트레이 에이전트는 로컬 이름 있는 파이프를 사용합니다. 파이프 이름은 현재 Windows 사용자 SID의 SHA-256 파생값을 포함하고, 파이프 ACL은 그 SID에만 전체 권한을 부여합니다. 요청은 JSON 한 줄이며 16KiB로 제한하고, 응답과 수신 시간에도 상한을 둡니다. 인터넷이나 LAN 리스너를 열지 않습니다.

지원하는 내부 요청은 상태, 로그, 시작, 안전 종료, 재시작, 백업, 서버 콘솔 명령, 예약 일시 중지·재개와 에이전트 종료입니다. 임의 PowerShell·CMD·실행 파일 호출은 지원하지 않습니다.

### 예약과 복구

에이전트는 서버별 자동화 스키마 2를 그대로 사용합니다. GUI와 에이전트의 파일 접근은 서버 경로별 Windows 뮤텍스로 직렬화하고, 각 실행은 PID·시작 시각 임대를 기록합니다. 따라서 절전 복귀나 GUI/에이전트 전환 뒤에도 같은 작업을 동시에 청구하지 않습니다. 놓친 작업은 기존 `run-once`, `skip`, `notify-only`와 최대 지연 정책을 사용하며 무제한 따라잡기 실행을 하지 않습니다.

백업은 에이전트가 소유한 실행 중 서버에서만 `save-off`, `save-all flush`, 백업, `save-on` 순서를 사용합니다. 소유하지 않은 실행 중 서버는 실패로 기록하고 파일 복사를 강행하지 않습니다. 시작 전·종료 후 백업과 개수·기간·용량 보존 정책도 기존 서버별 설정을 따릅니다.

### 업데이트와 제한

런처 업데이트는 에이전트에 안전 종료를 요청하고 종료를 확인한 뒤에만 실행 파일 교체를 시작합니다. 서버가 제한 시간 안에 종료되지 않으면 강제 종료하지 않고 업데이트를 중단합니다. 업데이트된 GUI가 다시 시작되면 활성화된 에이전트에 재연결합니다.

v1.13.0부터 사용자가 별도로 동의하면 에이전트의 기존 트레이 아이콘으로 새 운영 기록의 Windows 작업 표시줄 알림을 표시합니다. 알림은 중요도·종류·조용한 시간을 적용하고, 오래된 기록을 재생하지 않으며 짧은 시간의 여러 사건을 하나로 요약합니다. 자세한 경계는 [Windows 알림 구조](WINDOWS_NOTIFICATIONS.md)를 따릅니다.

현재 베타는 사용자 로그인 이전 실행, 관리자 권한 서비스, 영구 Windows 알림 센터 저장소와 웹/Discord 원격 관리를 제공하지 않습니다. 무중단 인계는 멀티 서버 관리가 이 버전의 제어 채널을 포함해 시작한 자식에 한정됩니다. 메인 런처 내부의 Java 서버와 외부 소유 서버는 인계하지 않으며 기존 안전 종료 또는 외부 소유 상태를 유지합니다. 자세한 신뢰 경계와 실패 처리는 [관리 서버 인계 구조](MANAGED_SERVER_HANDOFF.md)를 따릅니다.

## English

MineHarbor v1.12.0 Background operations is an opt-in beta that runs in the current user account, not an elevated Windows service. `MineHarbor.exe --background-agent` starts only after explicit consent in `Server management → Background`; Windows sign-in registration is a separate opt-in.

The GUI and agent have separate single-instance mutexes. The agent controls only `--managed-profile` children it started or received through the verified multi-server handoff. Each child uses a current-SID-only control pipe, fresh 256-bit token, and exact child/current-owner/new-owner PID plus process-start-time validation. Closing multi-server management offers live transfer, safe stop, or cancel; partial failure keeps the window and failed ownership while successful transfers remain with the agent. Normal agent shutdown sends `stop` to every owned server and exits only after they stop within the timeout. A managed child whose current owner disappears also attempts a safe console stop before exiting. Servers using the configured port but not owned by the agent are reported as external ownership and are never commanded, stopped, or live-backed up.

GUI/agent IPC uses a local named pipe derived from the current Windows SID, with a pipe ACL that grants only that SID. JSON Lines requests, responses, and receive time are bounded. No LAN or internet listener is opened, and no arbitrary shell or executable command is supported.

Per-server automation schema 2 remains the source of truth. A path-derived Windows mutex serializes cross-process file access, while PID/start-time leases prevent duplicate claims. Resume events re-evaluate the existing bounded `run-once`, `skip`, or `notify-only` missed-run policy. Live backups use `save-off`, `save-all flush`, backup, and `save-on` only for agent-owned servers.

Starting with v1.13.0, separately opted-in Windows taskbar notifications reuse the agent's tray icon for new operations. Severity/category filters, quiet hours, old-history suppression, and burst summaries follow the [Windows notification architecture](WINDOWS_NOTIFICATIONS.md).

Launcher updates request safe agent shutdown and begin executable replacement only after the agent exits; a timeout aborts the update instead of forcing a server exit. The beta does not provide pre-sign-in execution, an elevated service, a durable Windows notification-center store, or web/Discord remote control. Live transfer is limited to managed children started by this version's multi-server management; Java hosted directly inside the main launcher and externally owned servers retain their existing safe-close or external-ownership behavior. See [managed server handoff](MANAGED_SERVER_HANDOFF.md) for the trust and failure boundaries.
