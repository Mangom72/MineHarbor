# 빠른 명령 호환성과 위험도 / Quick-command compatibility and risk

## 명령 정의

기본·사용자·브리지 명령은 같은 `QuickCommandDefinition` 모델을 사용합니다. 템플릿의 `{player}`는 필수 인수이고 `[reason]`은 생략 가능한 선택 인수입니다. 선택 인수는 명령 끝에만 둘 수 있으며 여러 단어를 받는 `reason`, `message`, `command`는 마지막 인수에서 나머지 토큰을 함께 처리합니다.

사용자 명령은 `config/quick-commands.json`에 저장합니다. 기존 `Confirm=true` 항목은 읽을 때 `Confirm` 위험도로 승격하며, 새 저장에서는 `Risk`와 호환용 `Confirm`을 함께 동기화합니다. 최소·최대 Minecraft 버전 중 하나라도 지정된 명령은 현재 서버 버전을 정상적으로 해석할 수 있고 범위 안에 있을 때만 선택창과 로컬 자동완성에 나타납니다.

## 위험도

| 단계 | 실행 동작 | 예 |
| --- | --- | --- |
| `Normal` | 즉시 전송 | 목록·시드·시간·월드 경계 조회 |
| `Confirm` | 일반 경고 후 확인 | OP, 차단, 데이터팩 활성화/비활성화, 게임 규칙 변경 |
| `Dangerous` | 빨간 위험 경고와 영향 범위 재확인 | `reload`, 광범위 변경, `keepInventory false`, `doMobSpawning false` |

빠른 명령 입력, 메인 콘솔과 다중 서버 콘솔은 모두 같은 위험도 계산을 사용합니다. 명령 정의의 고정 위험도와 루트 명령·선택자·인수 값으로 계산한 조건부 위험도 중 더 높은 값을 적용합니다. `worldborder get`과 `forceload query`처럼 같은 루트의 읽기 전용 하위 명령은 변경 명령과 구분합니다.

## 버전 경계

- `datapack`과 `forceload` 기본 명령은 Minecraft 1.13 이상에서 표시합니다.
- `gamerule playersSleepingPercentage`는 1.17 이상에서 표시합니다.
- `time query daytime`은 기존 구문을 사용하는 1.13~1.21.11에서만 표시합니다. 26.x에서도 유지되는 `time query gametime`은 계속 제공합니다.
- 버전 범위가 없는 오래된 공통 명령은 기존과 같이 모든 서버 종류에 표시합니다.
- Paper/Purpur 브리지가 연결된 경우 서버가 보내는 실시간 명령·인수 후보를 우선하며, 로컬 목록은 오프라인 또는 브리지 미연결 상태의 안전한 기본값입니다.

## English summary

Built-in, user, and bridge commands share one definition model. Curly braces mark required arguments and square brackets mark trailing optional arguments. Legacy `Confirm=true` user JSON migrates to the `Confirm` risk level, while new entries persist an explicit three-level risk and optional Minecraft version range. The picker and local completion omit commands outside the selected server version.

`Normal` commands run immediately, `Confirm` commands use a standard warning, and `Dangerous` commands use a stronger red warning. Static definition risk is combined with root- and value-sensitive classification, while read-only exceptions such as `worldborder get` remain normal. Datapack and force-load commands require 1.13, sleeping percentage requires 1.17, and the legacy daytime-query syntax is hidden after 1.21.11.
