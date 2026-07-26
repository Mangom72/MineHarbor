# 백그라운드 에이전트 / Background Agent

## 한국어

MineHarbor v1.12.0의 백그라운드 운영은 관리자 권한 Windows 서비스가 아니라 현재 사용자 계정에서 실행되는 선택형 베타 기능입니다. 사용자가 `서버 관리 → 백그라운드`에서 명시적으로 켠 경우에만 `MineHarbor.exe --background-agent`가 실행됩니다. 설정은 사용자 데이터 폴더의 `background-agent.json`에 스키마 버전과 함께 원자적으로 저장하며, Windows 로그인 자동 시작은 별도로 동의한 경우에만 현재 사용자 `Run` 항목에 등록합니다.

### 프로세스와 소유권

- GUI와 에이전트는 서로 다른 단일 인스턴스 뮤텍스를 사용하므로 함께 실행할 수 있습니다.
- 에이전트는 자신이 시작한 `--managed-profile` 자식만 표준 입력·출력으로 제어합니다.
- 자식은 에이전트 PID와 프로세스 시작 시각을 확인해 PID 재사용을 차단합니다.
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

현재 베타는 사용자 로그인 이전 실행, 관리자 권한 서비스, Windows 알림 센터, 웹/Discord 원격 관리, 실행 중 GUI 서버의 무손실 소유권 이전을 제공하지 않습니다. GUI가 직접 시작한 서버는 기존처럼 GUI 종료 확인 후 안전 종료됩니다.

## English

MineHarbor v1.12.0 Background operations is an opt-in beta that runs in the current user account, not an elevated Windows service. `MineHarbor.exe --background-agent` starts only after explicit consent in `Server management → Background`; Windows sign-in registration is a separate opt-in.

The GUI and agent have separate single-instance mutexes. The agent controls only `--managed-profile` children that it started and validates the parent PID plus start time. Normal agent shutdown sends `stop` to every owned server and exits only after they stop within the timeout. A managed child whose parent disappears also attempts a safe console stop before exiting. Servers using the configured port but not owned by the agent are reported as external ownership and are never commanded, stopped, or live-backed up.

GUI/agent IPC uses a local named pipe derived from the current Windows SID, with a pipe ACL that grants only that SID. JSON Lines requests, responses, and receive time are bounded. No LAN or internet listener is opened, and no arbitrary shell or executable command is supported.

Per-server automation schema 2 remains the source of truth. A path-derived Windows mutex serializes cross-process file access, while PID/start-time leases prevent duplicate claims. Resume events re-evaluate the existing bounded `run-once`, `skip`, or `notify-only` missed-run policy. Live backups use `save-off`, `save-all flush`, backup, and `save-on` only for agent-owned servers.

Launcher updates request safe agent shutdown and begin executable replacement only after the agent exits; a timeout aborts the update instead of forcing a server exit. The beta does not provide pre-sign-in execution, an elevated service, Windows notifications, web/Discord remote control, or lossless transfer of a GUI-owned running server. GUI-owned servers keep the existing safe-close behavior.
