# Changelog

이 프로젝트는 제품 버전에 [Semantic Versioning](https://semver.org/)을 사용합니다. `26.2.45.xx` 값은 별도의 내부 빌드 번호입니다.

## [0.3.0] - 2026-07-11

내부 빌드: `26.2.45.24`

### Added

- Portable 및 Windows 설치 프로그램 배포 구조
- 사용자 데이터, Portable, 사용자 지정 서버 데이터 위치 선택과 영구 저장
- 단일 `version.json` 기반 제품 버전·빌드 번호 생성
- `update.json`, `SHA256SUMS.txt`와 GitHub Actions 릴리스 자동화
- MIT 라이선스, 보안 정책, 개인정보 안내와 기여 문서
- 데이터 위치, 업데이트 메타데이터, 해시, 교체 및 설정 UI 회귀 테스트

### Changed

- 런처 업데이트를 사용자 승인 후에만 실행하도록 변경
- 다운로드 크기와 SHA-256 검증, 기존 EXE 백업, 새 실행 확인과 복구 절차 보강
- 최초 설정 화면에 작은 화면 스크롤, 고정 저장/취소 버튼, 근접 입력 오류 표시와 비활성화 이유 도움말 추가

### Preserved

- 서버 프로필, 월드, 백업, 콘텐츠, Java 런타임과 포트포워딩/UPnP 기존 데이터 구조
- 기존 `Minecraft-Servers-Data` 자동 감지 및 비파괴 사용

## English

Version `0.3.0` introduces reproducible Portable and installer builds, selectable persistent data storage, separate semantic product and internal build versions, approved and verified launcher updates with rollback, release automation, policy documents, and core regression tests. Existing server data structures and non-destructive Portable data discovery are preserved.
