# Windows 알림 구조 / Windows notification architecture

## 한국어

MineHarbor v1.13.0의 Windows 알림은 별도 네트워크 서비스나 외부 공급자를 사용하지 않습니다. 사용자가 알림을 명시적으로 켜고 사용자 계정용 백그라운드 에이전트가 실행 중일 때만 기존 `NotifyIcon`을 통해 Windows 작업 표시줄 알림을 표시합니다. Microsoft의 [`NotifyIcon.ShowBalloonTip` 문서](https://learn.microsoft.com/dotnet/api/system.windows.forms.notifyicon.showballoontip)에 따라 실제 표시 시간과 형태는 Windows 접근성 및 알림 설정을 따릅니다.

설정은 사용자 데이터 폴더의 `windows-notifications.json`에 저장합니다. 스키마 버전, 최대 64KiB, 현재 경로 기반 프로세스 간 뮤텍스와 원자적 파일 교체를 사용하며 손상 또는 미래 스키마 파일을 자동으로 덮어쓰지 않습니다. 기본값은 비활성화이고, 활성화 후에는 정보·경고·오류 최소 중요도와 서버·예약·백업·콘텐츠·네트워크·업데이트/보안 종류를 선택할 수 있습니다.

조용한 시간은 로컬 시각의 시작·종료 분으로 저장하며 자정을 지나는 범위와 시작·종료가 같은 하루 전체 범위를 지원합니다. 조용한 시간에 생긴 사건은 운영 기록에는 남지만 작업 표시줄 알림으로 다시 재생하지 않습니다.

에이전트가 처음 기록을 읽을 때 서버별 최신 항목을 기준점으로 삼으므로 시작 전의 오래된 알림을 갑자기 반복하지 않습니다. 이후 새 기록만 검사하고, 보존 개수 제한으로 기준점이 사라진 경우에도 현재 최신 항목으로 이동해 과거 전체를 재생하지 않습니다. 대기 항목은 최대 50개이며 8초 안에 여러 건이 생기면 중요도가 가장 높은 최신 사건과 나머지 개수 하나로 요약합니다.

알림은 운영 기록에 저장된 한국어 또는 영어 요약만 사용합니다. 서버 절대 경로, IPv4 주소, 토큰·비밀번호·비밀값·웹훅 형태를 표시 전에 다시 가리고 명령 원문은 포함하지 않습니다. 설정·기록 읽기 또는 Windows 표시 실패는 서버 제어, 백업과 예약 실행에서 격리됩니다.

현재 구현은 Windows 알림 내구 저장소, 알림별 동작 버튼, 플레이어 접속·퇴장 전용 이벤트와 외부 전송을 제공하지 않습니다. 전체 사건과 읽음 상태의 기준 저장소는 계속 서버별 `.mineharbor/operations-history.json`입니다.

## English

MineHarbor v1.13.0 Windows notifications use no separate network service or external provider. They reuse the background agent's existing `NotifyIcon` only after explicit notification opt-in while the per-user agent is running. As documented for [`NotifyIcon.ShowBalloonTip`](https://learn.microsoft.com/dotnet/api/system.windows.forms.notifyicon.showballoontip), display duration and presentation follow Windows accessibility and notification settings.

Preferences are stored in `windows-notifications.json` under user data with a schema version, 64 KiB read limit, path-derived cross-process mutex, and atomic replacement. Corrupt or future schemas are preserved. The default is disabled; after enabling, users can select an Info, Warning, or Error threshold and server, schedule, backup, content, network, or update/security categories.

Quiet hours store local start/end minutes, support midnight-spanning ranges, and treat equal start/end values as all day. Events remain in operations history but are not replayed as taskbar notifications after quiet hours.

On first observation the agent records each server's latest entry as its baseline, preventing a startup replay of old events. Only later entries are considered. If retention removes the baseline, the monitor advances to the current latest entry instead of replaying history. Pending events are capped at 50; events within an eight-second burst collapse into the most important recent event plus a remaining count.

Only the Korean or English operation summary is displayed. Absolute server paths, IPv4 addresses, and token/password/secret/webhook-like values are sanitized again, and raw commands are never included. Setting, history-read, or Windows-display failures are isolated from server control, backup, and scheduling.

The current implementation does not provide a durable Windows notification-center store, per-notification action buttons, dedicated player join/leave events, or external delivery. Per-server `.mineharbor/operations-history.json` remains the authoritative event and read-state store.
