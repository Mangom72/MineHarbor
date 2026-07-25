# 빠른 명령 호환성과 위험도 / Quick-command compatibility and risk

## 명령 정의

기본·사용자·브리지 명령은 같은 `QuickCommandDefinition` 모델을 사용합니다. 템플릿의 `{player}`는 필수 인수이고 `[reason]`은 생략 가능한 선택 인수이며 `[count=1]`은 추천 기본값이 있는 선택 인수입니다. 선택 인수는 명령 끝에만 둘 수 있으며 여러 단어를 받는 `reason`, `message`, `command`는 마지막 인수에서 나머지 토큰을 함께 처리합니다. 기본값은 토큰과 후보에 표시하지만 사용자가 값을 지정하지 않으면 명령에서 생략해 Minecraft의 기본 동작을 사용합니다.

사용자 명령은 `config/quick-commands.json`에 저장합니다. 기존 `Confirm=true` 항목은 읽을 때 `Confirm` 위험도로 승격하며, 새 저장에서는 `Risk`와 호환용 `Confirm`을 함께 동기화합니다. 최소·최대 Minecraft 버전 중 하나라도 지정된 명령은 현재 서버 버전을 정상적으로 해석할 수 있고 범위 안에 있을 때만 선택창과 로컬 자동완성에 나타납니다.

## 단계형 작성 상태

명령 선택 시 템플릿을 고정 문구와 인수로 분리한 `QuickCommandBuilderState`를 만들고 명령별 초안을 유지합니다. 첫 필수 인수부터 시작해 값을 확정할 때 다음 인수로 이동하며, 이전 단계로 돌아가거나 후보 목록을 닫아도 다른 인수 값은 유지합니다. 미완성 인수는 회색 토큰, 현재 인수는 강조 토큰, 잘못된 값은 위험 테두리로 표시합니다.

전송 가능 여부는 토큰 색이나 플레이스홀더 존재 여부가 아니라 각 인수의 `Required`와 값 검증 결과로 계산합니다. 모든 필수 인수가 유효하면 선택 인수를 비운 채 전송할 수 있고, 잘못된 선택 인수를 입력한 경우에는 값을 지우거나 수정할 때까지 전송할 수 없습니다. 명령 선택과 후보 확정은 서버 명령을 실행하지 않으며 최종 전송 동작에서만 위험도를 계산하고 완성 명령을 확인합니다.

후보 목록은 온라인 플레이어와 Minecraft 선택자, 열거형 값, 숫자·시간·좌표 추천, 아이템·효과 부분 검색을 제공합니다. 항목이 적으면 필요한 높이만 사용하고 많으면 창의 위·아래 가용 공간 중 넓은 쪽에서 최대 430px까지 확장합니다. 공통 콘솔·플레이어 자동완성은 같은 비중첩 배치 원칙과 최대 20개 후보를 사용합니다.

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

Built-in, user, and bridge commands share one definition model. Curly braces mark required arguments, square brackets mark trailing optional arguments, and `[count=1]` supplies an optional recommendation while omission preserves Minecraft's default behavior. A per-command builder draft retains values across backward navigation and suggestion closing. Completion depends on required/optional metadata and value validation rather than visual placeholders; selecting a command or candidate never executes it.

Incomplete, active, completed, and invalid arguments are rendered distinctly. Up/Down moves through candidates, Tab confirms and advances, Shift+Tab returns, Esc closes or cancels, and Enter advances while required values are incomplete or sends once they are valid. Suggestions cover connected players/selectors, enum and numeric recommendations, and searchable item/effect catalogs, growing only as needed up to 430 pixels without covering the input.

Legacy `Confirm=true` user JSON migrates to the `Confirm` risk level, while new entries persist an explicit three-level risk and optional Minecraft version range. The picker and local completion omit commands outside the selected server version.

`Normal` commands run immediately, `Confirm` commands use a standard warning, and `Dangerous` commands use a stronger red warning. Static definition risk is combined with root- and value-sensitive classification, while read-only exceptions such as `worldborder get` remain normal. Datapack and force-load commands require 1.13, sleeping percentage requires 1.17, and the legacy daytime-query syntax is hidden after 1.21.11.
