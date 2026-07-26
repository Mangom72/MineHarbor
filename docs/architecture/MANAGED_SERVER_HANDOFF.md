# 관리 서버 인계 / Managed server handoff

## 한국어

MineHarbor v1.14.0은 백그라운드 운영을 명시적으로 켠 경우, 멀티 서버 관리 창이 `--managed-profile`로 시작한 실행 중 서버를 중단하지 않고 사용자 계정용 백그라운드 에이전트로 인계할 수 있습니다. 메인 런처 프로세스 안에서 직접 호스팅하는 Java 서버, 사용자가 직접 실행한 서버와 다른 프로그램의 서버는 인계 대상으로 추정하지 않습니다.

### 신뢰 경계

- 각 관리 자식은 실행할 때마다 `MineHarbor.ManagedChild.<난수>` 이름의 로컬 파이프와 256비트 난수 토큰을 새로 만듭니다.
- 파이프 ACL은 현재 Windows 사용자 SID에만 전체 권한을 부여합니다. LAN이나 인터넷 리스너를 열지 않습니다.
- 인계 설명에는 프로필, 제어 파이프, 토큰, 자식 PID·시작 시각과 현재 소유자 PID·시작 시각이 들어갑니다.
- 에이전트는 등록된 프로필과 자식 프로세스를 다시 확인하고, 자식은 현재 소유자가 설명과 정확히 일치하며 새 에이전트 PID·시작 시각이 실제로 살아 있을 때만 소유자를 원자적으로 교체합니다.
- 포트 응답, 프로세스 이름 또는 PID만으로는 소유권을 인정하지 않습니다.

토큰은 자식 프로세스 인수와 해당 프로세스를 시작한 부모 메모리에만 존재하며 로그, 운영 기록, 진단 묶음이나 설정 파일에 저장하지 않습니다. 같은 Windows 사용자 계정 안의 로컬 IPC를 보호하기 위한 실행별 비밀값이며 원격 인증 자격 증명으로 사용하지 않습니다.

### 인계 흐름

1. 멀티 서버 관리 창은 실행 중 로컬 세션을 고정하고 인계 중 프로세스 종료 사건과 추가 창 닫기를 보류합니다.
2. 에이전트는 자식 제어 채널의 프로필, 자식 프로세스와 현재 소유자를 검증합니다.
3. 자식은 새 에이전트 프로세스가 살아 있음을 확인한 뒤 소유자를 한 번에 바꿉니다.
4. 에이전트가 프로세스 종료 감시, 콘솔 명령, 로그, 백업, 예약과 재시작 책임을 등록한 뒤 성공을 응답합니다.
5. GUI는 자신의 표준 입출력 읽기를 닫고 로컬 세션을 제거합니다. 자식의 안전한 출력 래퍼는 부모 파이프가 닫힌 뒤에도 로그를 보존하며 쓰기 실패로 서버를 종료하지 않습니다.

인계 요청은 같은 자식에 대해 멱등하게 처리합니다. GUI는 응답이 없으면 한 번 재시도하며, 그래도 응답이 없을 때는 자식이 기존 소유자와 다른 실제 생존 소유자를 보고한 경우에만 완료로 복구합니다. 여러 서버 가운데 일부만 실패하면 창을 유지하고 실패 서버의 소유권을 놓지 않으며, 성공한 서버만 에이전트가 계속 관리합니다.

### 종료과 복구

관리 자식은 현재 소유자의 PID와 시작 시각을 주기적으로 확인합니다. 인계 뒤 이전 GUI가 종료되어도 새 에이전트가 살아 있으면 서버를 유지합니다. 현재 소유자가 예기치 않게 사라지면 기존 정책대로 Java 서버에 `stop`을 보내고 제한 시간 동안 안전 종료를 기다립니다. 에이전트 종료와 런처 업데이트도 인계 서버를 다른 에이전트 소유 서버와 동일하게 안전 종료하며 강제 인계를 추측하지 않습니다.

현재 구현은 실행 중인 메인 런처 내부 Java 프로세스를 별도 자식 호스트로 바꾸지 않습니다. 따라서 무중단 인계 지원 범위는 이번 버전의 멀티 서버 관리가 시작한 관리 자식으로 제한됩니다.

## English

MineHarbor v1.14.0 can transfer a running `--managed-profile` child started by multi-server management to the explicitly enabled per-user background agent without stopping the server. A Java server hosted directly inside the main launcher, a manually started server, or a server from another program is never inferred as transferable.

Each managed child receives a fresh local `MineHarbor.ManagedChild.<random>` pipe and 256-bit token. The pipe ACL grants only the current Windows SID and opens no LAN or internet listener. The transfer descriptor includes the profile, child PID/start time, current-owner PID/start time, pipe, and token. The agent revalidates the registered profile and exact child identity; the child changes ownership atomically only when the current owner matches and the exact new agent process is alive. Port state, process names, and PID alone are not ownership proof.

The child retains a bounded recent-log buffer and command endpoint. A safe output wrapper tolerates the old parent's redirected stream closing, so log writes cannot terminate the server after a successful transfer. The agent then owns console commands, logs, safe stop/restart, live backup, schedules, crash handling, and process-exit monitoring.

Requests are idempotent. The GUI retries a lost reply once and otherwise accepts recovery only when the child reports a different exact live owner. Process exits and repeated window-close requests are deferred while a transfer is unresolved. For a multi-server partial failure, the window remains open and continues owning failures while successful transfers stay with the agent.

The managed child continuously validates its current owner PID and start time. The old GUI may exit after transfer, while loss of the actual current owner triggers the existing safe Java `stop` path. This version does not re-host or transfer a Java process running directly inside the main launcher; live handoff is limited to managed children started by this version's multi-server management.
