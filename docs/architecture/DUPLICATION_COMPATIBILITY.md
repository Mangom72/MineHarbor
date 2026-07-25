# Paper/Purpur 복사 호환성 / Paper/Purpur duplication compatibility

## 지원 범위

MineHarbor는 Paper가 공개한 비지원 호환 설정 가운데 다음 세 항목을 서버별로 관리합니다.

| 화면 옵션 | Paper 설정 키 | 동작 |
| --- | --- | --- |
| TNT·양탄자·레일 복사 | `unsupported-settings.allow-piston-duplication` | 같은 피스톤 동작에 의존하는 TNT, 양탄자와 레일 복사를 함께 허용 |
| 모래 등 중력 블록 복사 | `unsupported-settings.allow-unsafe-end-portal-teleportation` | 엔드 차원문을 이용하는 중력 블록 복사를 허용 |
| 철사덫 갈고리 복사 | `unsupported-settings.skip-tripwire-hook-placement-validation` | 철사덫 갈고리 복사에 필요한 배치 검증을 건너뜀 |

Paper와 이를 기반으로 하는 Purpur는 Minecraft 1.19 이상에서 `config/paper-global.yml`의 최상위 `unsupported-settings`, 1.18.2 이하에서 `paper.yml`의 `settings.unsupported-settings`를 사용합니다. 피스톤 설정은 모든 대상 세대에서 지원하지만, 중력 블록 설정은 Paper 1.20.4 이상, 철사덫 설정은 1.21.4 이상에서만 새로 생성합니다. 직접 JAR 프로필과 알 수 없는 버전은 기존 Paper 설정 파일과 실제 생성된 신형 키가 확인된 범위에서만 지원합니다. Spigot, Vanilla, Fabric, Forge와 NeoForge에는 동일한 공식 설정 키가 없으므로 MineHarbor가 Paper 전용 YAML을 생성하지 않습니다.

이 설정은 Paper가 공식 지원하지 않으며 버전별로 동작이 바뀌거나 제거될 수 있습니다. 설정 화면은 처음 활성화할 때 위험을 확인하고 서버가 꺼진 상태에서만 저장합니다. 서버를 다시 시작할 때 선택값을 다시 확인하되, v1.8.0 이전 프로필은 사용자가 새 설정 화면에서 저장하기 전까지 기존 수동 YAML을 덮어쓰지 않습니다.

## 파일 안전성

- 서버 루트 밖의 경로와 재분석 지점 파일·폴더를 거부합니다.
- 4MiB를 넘는 YAML, 탭 들여쓰기, 중복 `unsupported-settings`, 중복 관리 키, `true`/`false`가 아닌 값과 인라인 구역을 거부합니다.
- 기존 주석과 관리 대상이 아닌 키를 보존하고 같은 폴더의 임시 파일을 검증한 뒤 교체합니다.
- 실제 변경 전에 `.mineharbor/configuration-backups`에 설정별 최근 5개 백업을 유지합니다.
- 선택값과 파일이 이미 같으면 다시 쓰거나 중복 백업하지 않습니다.

공식 근거는 PaperMC의 [버그 수정 설명](https://docs.papermc.io/paper/misc/paper-bug-fixes/), [전역 설정 참조](https://docs.papermc.io/paper/reference/global-configuration/), [CLI 설정 경로](https://docs.papermc.io/paper/reference/cli-arguments/), 중력 블록 옵션을 추가한 [Paper PR #10191](https://github.com/PaperMC/Paper/pull/10191), 철사덫 옵션을 추가한 [Paper PR #12091](https://github.com/PaperMC/Paper/pull/12091)입니다. Purpur는 Paper의 드롭인 대체 서버로 상위 Paper 설정을 함께 사용합니다.

## English summary

MineHarbor manages the documented unsupported Paper compatibility keys for piston-based TNT/carpet/rail duplication, end-portal gravity-block duplication, and tripwire-hook duplication. Paper and Purpur use top-level `unsupported-settings` in `config/paper-global.yml` on Minecraft 1.19+ and nested `settings.unsupported-settings` in legacy `paper.yml`. Piston duplication is available across the target generations; MineHarbor creates the gravity-block key only on 1.20.4+ and the tripwire key only on 1.21.4+. A custom JAR exposes newer controls only when the corresponding generated keys are detected. Spigot, Vanilla, Fabric, Forge, and NeoForge never receive Paper-only keys.

Writes are constrained to the server root, reject linked or malformed/duplicate/oversized YAML, preserve comments and unrelated entries, replace through a same-directory temporary file, and retain five pre-change backups. Existing pre-v1.8.0 profiles are not automatically enrolled, preventing manually maintained Paper settings from being overwritten before the user saves the new controls.
