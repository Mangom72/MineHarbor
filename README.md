# Minecraft Server Launcher

Windows에서 마인크래프트 서버를 쉽게 열기 위한 단일 실행 파일 런처입니다. Java 런타임, 서버 파일 준비, 최초 설정, 업데이트, 백업, 포트포워딩 점검을 최대한 자동화합니다.

[최신 EXE 다운로드](./releases/latest/download/Paper-26.2-Server.exe)

## 주요 기능

- Paper, Vanilla, Purpur, Fabric, Forge, 직접 JAR 실행 지원
- Minecraft 버전 드롭다운 선택
- 스냅샷/프리릴리즈는 체크박스를 켰을 때만 표시
- 여러 서버 프로필을 분리해서 관리
- 서버 파일 자동 다운로드
- 서버 파일 수동 JAR 지정
- 서버 업그레이드 버튼
- 서버 파일 자동 업데이트 옵션
- 내장 Java 25 런타임 사용
- 한국어/영어 UI 전환
- 다크/라이트 모드
- 최초 실행 서버 설정 마법사
- 자주 쓰는 서버 프리셋 제공
- 서버 메모리 설정
- `server.properties` 주요 항목 GUI 설정
- 런처 자동 업데이트
- 서버 파일 교체 전 자동 백업
- 설정 변경 전 `server.properties` 백업
- 포트포워딩 및 외부 접속 가능 여부 점검
- 서버 소유자 자동 OP
- 필요할 때만 여는 내장 콘솔

## 빠른 시작

1. `Paper-26.2-Server.exe`를 원하는 폴더에 둡니다.
2. EXE를 실행합니다.
3. 최초 실행 설정창에서 서버 프로필, 서버 종류, Minecraft 버전, 프리셋, 서버 이름, 포트, 메모리를 정합니다.
4. Minecraft EULA에 동의합니다.
5. `서버 시작하기`를 누릅니다.
6. 런처에 표시되는 주소를 친구에게 전달합니다.

서버 데이터는 EXE가 있는 위치의 `Minecraft-Servers-Data` 폴더에 생성됩니다.

## 서버 종류와 버전

지원되는 서버 종류:

- Paper
- Vanilla, Minecraft 기본 서버
- Purpur
- Fabric
- Forge
- 직접 JAR 지정

버전 목록은 선택한 서버 종류에 맞는 공개 API에서 가져옵니다. 스냅샷/프리릴리즈는 기본적으로 숨겨져 있으며, `스냅샷/프리릴리즈 표시`를 켜면 목록에 포함됩니다.

직접 JAR을 선택하면 런처가 해당 파일을 프로필 폴더로 복사한 뒤 실행합니다. 직접 JAR은 런처가 자동으로 교체하지 않습니다.

## 멀티 서버 프로필

설정창의 `서버 프로필 이름`으로 서버를 분리할 수 있습니다. 프로필마다 별도 폴더가 만들어지므로 월드, 플러그인, 설정을 따로 유지할 수 있습니다.

폴더 예시:

```text
Paper-26.2-Server.exe
Minecraft-Servers-Data/
  .active-server-profile
  servers/
    기본 서버/
      server.properties
      world/
      plugins/
    creative-flat/
      server.properties
      world/
```

## 제공 프리셋

- 평화로움 야생
- 쉬움 야생
- 보통 야생
- 어려움 야생
- 하드코어 야생
- 크리에이티브 월드 일반 지형
- 크리에이티브 월드 평지
- 직접 설정

크리에이티브 프리셋은 명령 블록을 자동으로 허용합니다. 직접 설정을 고르면 게임 모드, 난이도, 하드코어 여부 등을 직접 조정할 수 있습니다.

## 설정되는 주요 서버 옵션

- 서버 프로필 이름
- 서버 종류
- Minecraft 버전
- 서버 이름
- 최대 접속 인원
- 서버 포트
- 서버 메모리
- 게임 모드
- 난이도
- 월드 유형
- PvP
- 화이트리스트
- 명령 블록
- 정품 계정 인증
- 시야 거리
- 시뮬레이션 거리
- 서버 파일 자동 업데이트
- 서버 소유자

## 서버 업그레이드와 백업

`서버 업글` 버튼을 누르면 현재 활성 프로필의 서버 파일을 선택한 서버 종류와 Minecraft 버전에 맞춰 최신 파일로 갱신합니다.

서버 종류나 Minecraft 버전을 바꾸면 기존 월드, 플러그인, 모드가 호환되지 않을 수 있습니다. 런처는 변경 전에 경고하고, 서버 데이터 백업을 만든 뒤 진행합니다. 중요한 월드는 별도로 수동 백업하는 것을 권장합니다.

백업 위치:

- `Minecraft-Servers-Data/servers/<프로필>/server-backups`
- `Minecraft-Servers-Data/servers/<프로필>/server-jar-backups`
- `Minecraft-Servers-Data/servers/<프로필>/configuration-backups`

## 서버 소유자 자동 OP

설정창의 `서버 소유자` 항목에 마인크래프트 닉네임을 입력하면, 해당 사용자가 서버에 접속할 때 자동으로 OP 권한을 받을 수 있습니다.

보안을 위해 정품 계정 인증이 켜진 상태를 권장합니다. 온라인 인증이 꺼져 있으면 닉네임 사칭 위험이 있으므로 자동 OP가 비활성화됩니다.

## 포트포워딩 안내

기본 마인크래프트 서버 포트는 `25565/TCP`입니다. 외부 접속을 허용하려면 공유기에서 이 포트를 서버 PC의 내부 IP로 포워딩해야 합니다.

일반적인 설정 순서:

1. 런처에서 서버 포트를 확인합니다.
2. Windows 방화벽에서 해당 TCP 포트의 인바운드 접속을 허용합니다.
3. 공유기 관리자 페이지에 접속합니다.
4. 포트포워딩 메뉴에서 외부 포트와 내부 포트를 같은 값으로 설정합니다.
5. 내부 IP에는 서버를 실행하는 PC의 IPv4 주소를 입력합니다.
6. 프로토콜은 TCP로 설정합니다.
7. 서버를 실행한 뒤 런처의 외부 접속 점검 결과를 확인합니다.

외부 접속 주소는 보통 `공인 IP:포트` 형식입니다. 같은 집 안에서는 공인 IP 접속이 안 될 수 있으므로, 외부 네트워크의 친구에게 접속 테스트를 부탁하는 것이 가장 확실합니다.

## 네트워크 사용

런처는 다음 작업을 위해 인터넷에 접속할 수 있습니다.

- 런처 최신 버전 확인
- Minecraft 버전 목록 확인
- Paper, Purpur, Fabric, Forge, Vanilla 서버 파일 정보 확인
- 서버 파일 다운로드
- 업데이트 파일 다운로드
- 외부 접속 가능 여부 점검

## 문제 해결

| 증상 | 확인할 것 |
| --- | --- |
| 친구가 접속하지 못함 | 포트포워딩, Windows 방화벽, 서버 포트, 공인 IP를 확인하세요. |
| 같은 집에서는 접속되는데 외부에서 안 됨 | 공유기 포트포워딩과 통신사 공유기/이중 NAT 여부를 확인하세요. |
| 서버가 바로 종료됨 | 콘솔을 열어 오류 로그를 확인하세요. |
| 월드 유형을 바꿨는데 적용되지 않음 | 이미 생성된 월드에는 월드 유형 변경이 적용되지 않습니다. 새 월드 생성 시 적용됩니다. |
| 서버 종류나 버전을 바꾼 뒤 오류가 남 | 플러그인/모드 호환성을 확인하고, 필요한 경우 백업에서 복원하세요. |
| 자동 업데이트가 실패함 | 인터넷 연결과 선택한 서버 프로젝트 API 접속 가능 여부를 확인하세요. |
| 서버 소유자 자동 OP가 안 됨 | 정품 계정 인증이 켜져 있는지, 서버 소유자 닉네임이 정확한지 확인하세요. |

## 현재 로컬 빌드

- 버전: `26.2.45.13`
- 파일명: `Paper-26.2-Server.exe`

---

# English

Minecraft Server Launcher is a single-file Windows launcher for running Minecraft servers with minimal setup. It prepares Java, manages server files, provides a first-run setup wizard, checks updates, creates backups, and helps verify port forwarding.

[Download latest EXE](./releases/latest/download/Paper-26.2-Server.exe)

## Features

- Paper, Vanilla, Purpur, Fabric, Forge, and custom JAR support
- Minecraft version dropdown
- Snapshots/pre-releases are hidden unless explicitly enabled
- Separate multi-server profiles
- Automatic server file download
- Manual server JAR selection
- Server upgrade button
- Optional server file auto update
- Bundled Java 25 runtime
- Korean/English UI
- Dark/light mode
- First-run setup wizard
- Common server presets
- Server memory selection
- GUI configuration for major `server.properties` options
- Launcher auto update
- Automatic backups before server file replacement
- `server.properties` backup before configuration changes
- Port-forwarding and external reachability check
- Server owner auto-OP
- Built-in console that can be opened only when needed

## Quick start

1. Put `Paper-26.2-Server.exe` in the folder where you want to run the server.
2. Run the EXE.
3. Choose a profile, server type, Minecraft version, preset, server name, port, and memory.
4. Accept the Minecraft EULA.
5. Press `Start server`.
6. Share the displayed address with friends.

Server data is created in `Minecraft-Servers-Data` next to the EXE.

## Server types and versions

Supported server types:

- Paper
- Vanilla
- Purpur
- Fabric
- Forge
- Custom JAR

Version lists are loaded from public APIs for the selected server type. Snapshots and pre-releases are hidden by default and shown only when the snapshot checkbox is enabled.

Custom JAR files are copied into the active profile folder before launch. The launcher does not replace custom JARs automatically.

## Multi-server profiles

Use `Server profile name` to keep separate servers. Each profile has its own folder, world, plugins, and settings.

## Upgrades and backups

The `Upgrade server` button updates the active profile’s server file for the selected server type and Minecraft version.

Changing server type or Minecraft version can break worlds, plugins, or mods. The launcher warns before saving and creates backups around server changes. Manual backups are still recommended for important worlds.

## Network use

The launcher may access the internet to:

- Check launcher updates
- Load Minecraft version lists
- Resolve Paper, Purpur, Fabric, Forge, and Vanilla server downloads
- Download server files
- Download launcher updates
- Verify external reachability

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| Friends cannot connect | Check port forwarding, Windows Firewall, server port, and public IP. |
| LAN works but external access fails | Check router port forwarding and double NAT. |
| Server exits immediately | Open the console and check the error log. |
| World type change does not apply | Existing worlds keep their generated type. Create a new world to apply it. |
| Errors after changing server type/version | Check plugin/mod compatibility and restore from backup if needed. |
| Auto update fails | Check internet access and selected server project API availability. |
| Owner auto-OP does not work | Check online authentication and the exact owner nickname. |

## Current local build

- Version: `26.2.45.13`
- File name: `Paper-26.2-Server.exe`
