# Security Policy

## 지원 버전

보안 수정은 최신 안정 릴리스를 우선 대상으로 합니다. 오래된 빌드에서 문제가 발생하면 최신 릴리스에서도 재현되는지 먼저 확인해 주세요.

## 취약점 제보

민감한 취약점은 공개 Issue에 세부 내용을 올리지 말고 저장소의 [비공개 Security Advisory](https://github.com/Mangom72/MineHarbor/security/advisories/new)로 제보해 주세요. 재현 조건, 영향을 받는 버전, 예상 영향과 가능한 완화 방법을 포함하면 확인에 도움이 됩니다.

서버 월드, 계정 정보, 공인 IP, 공유기 설정, 토큰 또는 개인 로그 원본은 제보에 포함하지 마세요. 필요한 경우 런처가 생성한 개인정보 제거 진단 묶음을 사용해 주세요.

명령 브리지는 루프백 주소에만 바인딩된 런처 리스너, 실행마다 새로 생성되는 256비트 토큰, 프로필·프로토콜 확인, JSON Lines 크기와 후보 개수 제한을 사용합니다. 브리지는 서버 명령을 실행하지 않으며 외부 네트워크 포트를 열지 않습니다. 세션 파일이나 토큰이 로그·진단 묶음·취약점 제보에 포함되지 않게 해 주세요.

플러그인과 모드는 서버에서 코드를 실행할 수 있으므로 신뢰하는 프로젝트만 설치해야 합니다. MineHarbor는 Modrinth CDN·크기·SHA-512/SHA-1, Minecraft 버전·로더와 필수 의존성을 확인하지만 제3자 콘텐츠 자체의 안전성을 보증하지 않습니다. 데이터팩 ZIP은 루트 `pack.mcmeta`, 경로 이탈, 중복 항목, 항목 수와 해제 크기를 검사합니다. 관리 콘텐츠 제거는 서버 내부 휴지통으로 이동하며 수동 파일은 manifest에서 명확히 구분합니다.

`.mineharbor/content-manifest.json`과 `.mineharbor/automation.json`은 크기·스키마·경로·중복을 검증하고 원자적으로 교체합니다. 손상된 설정은 자동으로 덮어쓰지 않습니다. 예약 명령은 줄바꿈과 제어 문자를 거부하고, 예약 작업은 프로세스 ID·시작 시각 임대로 중복 실행을 막습니다. 놓친 작업의 기본값은 한 번만 실행이며 무제한 따라잡기 실행을 하지 않습니다. 자동화 파일을 편집할 수 있는 사용자는 서버 콘솔 명령을 예약할 수 있으므로 서버 폴더의 Windows 권한을 신뢰할 수 있는 계정으로 제한해 주세요.

서버별 `.mineharbor/operations-history.json`은 원자적 교체와 프로세스 간 잠금을 사용하고 각 항목을 SHA-256 연속 해시로 연결합니다. 이는 우발적 또는 사후 파일 변경을 감지하지만, 운영체제 계정을 침해한 공격자에 대한 전자서명이나 외부 증명은 아닙니다. 해시 불일치·손상·미래 스키마를 발견하면 원본을 덮어쓰지 않으며, 운영 기록 실패가 서버 제어를 중단시키지 않도록 별도 오류로 처리합니다.

Paper/Purpur 복사 호환성 옵션은 해당 프로젝트가 공식 지원하지 않는 설정이므로 서버 안정성이나 플러그인 동작에 영향을 줄 수 있습니다. MineHarbor는 지원되는 Paper 계열 경로만 수정하고, YAML의 중복·잘못된 형식·연결 경로·과대 파일을 거부하며 변경 전 설정을 `.mineharbor/configuration-backups`에 보관합니다. Spigot·Vanilla·모드 서버에는 Paper 전용 키를 적용하지 않습니다. 서버 업데이트 후에는 원하는 복사 장치와 월드 백업 상태를 다시 확인해 주세요.

현재 GitHub Release의 Windows 실행 파일은 릴리스마다 새로 생성하고 즉시 폐기하는 자체서명 인증서를 사용합니다. 이는 다운로드 후 파일이 변경되지 않았는지 확인하는 보조 수단이며, 공개 인증 기관이 MineHarbor 배포자 신원을 보증한다는 뜻이 아닙니다. 인증서를 신뢰 저장소에 수동 설치하지 말고, GitHub Release의 `SHA256SUMS.txt`와 저장소 출처를 함께 확인해 주세요.

## Supported versions

Security fixes target the latest stable release. Please verify whether an issue still reproduces on the latest release before reporting it.

Report sensitive vulnerabilities through a [private GitHub Security Advisory](https://github.com/Mangom72/MineHarbor/security/advisories/new), not a public issue. Do not include worlds, account data, public IP addresses, router credentials, tokens, or unredacted logs.

The command bridge uses a loopback-only launcher listener, a fresh 256-bit token per run, profile and protocol validation, and bounded JSON Lines messages and suggestions. It never executes server commands or opens an external network port. Do not include bridge session files or tokens in reports.

Plugins and mods can execute code in the server process, so install only trusted projects. MineHarbor validates the Modrinth CDN, declared size, SHA-512/SHA-1, game version, loader, and required dependencies, but cannot guarantee third-party content safety. Data-pack ZIPs are checked for a root `pack.mcmeta`, path traversal, duplicate entries, entry count, and expanded size. Managed removals move files into server-local trash, and manually installed files remain clearly distinguished.

`.mineharbor/content-manifest.json` and `.mineharbor/automation.json` are bounded, schema/path/duplicate validated, and atomically replaced; corrupt files are not silently overwritten. Scheduled commands reject line breaks and control characters, and process-ID/start-time leases prevent duplicate jobs. A missed job runs at most once by default; unbounded catch-up execution is not used. Anyone who can edit a server's automation file can schedule console commands, so restrict Windows permissions on server directories to trusted accounts.

Each server's `.mineharbor/operations-history.json` uses atomic replacement and a cross-process lock, with entries linked by a SHA-256 hash chain. This detects accidental or after-the-fact file changes; it is not a digital signature or external attestation against an attacker who has compromised the Windows account. Hash mismatches, corruption, and future schemas are preserved rather than overwritten, and a history-store failure is isolated so it cannot stop local server control.

Paper/Purpur duplication compatibility switches use settings that those projects do not officially support and can affect stability or plugin behavior. MineHarbor modifies only supported Paper-family paths, rejects malformed or duplicate YAML, linked paths, and oversized files, and retains pre-change copies under `.mineharbor/configuration-backups`. Paper-only keys are not applied to Spigot, Vanilla, or modded servers. Recheck the intended machines and world backups after server updates.

Windows executables in the current GitHub Release use a fresh self-signed certificate that is discarded after each release. This is an additional file-integrity signal, not a public certificate authority's verification of the MineHarbor publisher identity. Do not manually install that certificate as a trusted root; verify the repository source and release `SHA256SUMS.txt` instead.
