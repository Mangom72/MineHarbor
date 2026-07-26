# CODEX_HANDOFF.md

## 0.00000 Current implementation state (v1.15.1 released)

- Release-state branch: `codex/v1.15.1-release-state` (feature: `codex/discord-onboarding-v1.15.1`)
- Pull request [#40](https://github.com/Mangom72/MineHarbor/pull/40) is merged. PR CI `30196858135`, main CI `30196903438`, and release run `30196946031` passed.
- Version source of truth: `version.json` = 1.15.1 / build 26.2.45.80
- Entering Discord remote settings without a complete protected token, valid application/guild/channel IDs, an allowed user or role, and an approved profile now opens a four-step registration guide first.
- The guide reuses the launcher palette, rounded cards, and managed buttons; supports Korean/English, DPI, keyboard, and screen readers; and avoids whole-form scrolling. Only Start setup advances, while Not now/Escape changes nothing.
- Complete registrations skip onboarding. The settings footer can reopen the guide without changing saved settings.
- The only external link is the fixed official HTTPS Discord Developer Portal. Current-user DPAPI storage, all allowlists, ownership checks, confirmations, throttling, and the arbitrary-console/shell/file prohibition are unchanged.
- The existing Discord test group now covers registration detection, onboarding routing, four cards, accessible actions, and Enter/Escape mapping. A temporary visual harness confirmed the Korean layout without reading or modifying real Discord credentials.
- Resource preparation, the framework build, and the local self-signed release candidate passed. The temporary certificate/PFX was removed and `RELEASE_ARTIFACTS_PASSED=7` was reported.
- Both the ordinary and signed Portable executable passed version consistency, all 32 launcher groups, Portable version/smoke, 10 bridge cases, modern-dialog scan, and security regression scan.
- The existing modeless-window click-through integration test was made deterministic by explicitly arming its handle, cursor point, and guard deadline; production click-guard behavior is unchanged.
- The [v1.15.1 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.15.1) contains seven verified public assets. The workflow removed its temporary RSA-3072/SHA-256 certificate/PFX, and an independent public download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.15.0->1.15.1` before rerunning all 32 launcher groups and 10 bridge cases against the public executable.

## 0.0000 Current implementation state (v1.15.0 released)

- Release-state branch: `codex/v1.15.0-release-state` (feature: `codex/discord-remote-v1.15.0`)
- Pull request [#38](https://github.com/Mangom72/MineHarbor/pull/38) is merged. PR CI `30190800441`, main CI `30190836081`, and release run `30190885852` passed.
- Version source of truth: `version.json` = 1.15.0 / build 26.2.45.79
- Discord remote control is a separately opted-in beta owned by the per-user background agent. It opens no inbound listener and uses outbound Discord Gateway/REST connections only.
- The bot token is protected with current-user Windows DPAPI. Application, guild, channel, user-or-role, profile, replay, and per-user rate checks are applied before every operation.
- The guild-only `/mineharbor` command exposes help, status, bridge-backed players, recent warning/error summaries, start, backup, and 60-second single-use confirmed safe stop/restart. Arbitrary console, shell, file access, DMs, and control of externally owned processes are not exposed.
- Stop/restart confirmation state is random, bound to the same actor/guild/channel/profile/action, expires after 60 seconds, and is single-use. Responses disable mentions and are length bounded.
- The themed settings UI masks tokens, preserves an existing token when the field is blank, requires explicit removal, limits profiles, and includes Korean/English, DPI, keyboard, and screen-reader metadata.
- Local tests cover DPAPI non-plaintext storage, corrupt/future schema preservation, direct-user and role authorization, wrong application/guild/channel/profile rejection, arbitrary command rejection, confirmation expiry/replay/wrong-user rejection, autocomplete scoping, rate limiting, response bounds, and UI accessibility. They do not use a real Discord bot or Minecraft server.
- `Prepare-BuildResources.ps1`, the framework-compiler build, and the full test suite pass with `VERSION_CONSISTENCY_OK`, 32 launcher groups, Portable version/smoke, 10 bridge protocol cases, modern-dialog scan, and security regression scan.
- `Publish-LocalRelease.ps1 -NoPublish` passed ephemeral RSA-3072/SHA-256 self-signing and `RELEASE_ARTIFACTS_PASSED=7`, removed the certificate/PFX, and the full suite passed again against the signed Portable EXE.
- PR and main CI passed the .NET 10 SDK warning-as-error `net48` build, Portable/bridge builds, the full suite, and verification-artifact upload.
- The [v1.15.0 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.15.0) contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.14.0->1.15.0` before rerunning all 32 launcher groups and 10 bridge cases against the updated public executable.

## 0.000 Current implementation state (v1.14.0 released)

- Release-state branch: `codex/v1.14.0-release-state` (feature: `codex/server-handoff-v1.14.0`)
- Pull request [#36](https://github.com/Mangom72/MineHarbor/pull/36) is merged. PR CI `30189035839`, main CI `30189074256`, and release run `30189118176` passed.
- Version source of truth: `version.json` = 1.14.0 / build 26.2.45.78
- Multi-server management children started by this version can transfer to the explicitly enabled per-user background agent without stopping. The close flow distinguishes live transfer, safe stop-all, and cancel.
- Every child uses a fresh current-user-SID-only pipe and 256-bit token. The registered profile, exact child identity, previous owner, and new owner are checked by PID and process-start time before an atomic ownership change.
- The child retains a bounded recent-log buffer and server-console endpoint. A safe output wrapper isolates the old parent's closed redirected stream, preserving agent console, safe stop/restart, live backup, schedules, and crash monitoring.
- Agent-side ownership-transfer reply loss is reconciled by rereading and validating the child's exact new owner before session registration. GUI requests are idempotent; partial failure keeps the window and failed ownership while successful transfers stay with the agent.
- `Prepare-BuildResources.ps1`, `build.ps1 -SkipDependencyDownload`, and `test.ps1 -LauncherPath artifacts\MineHarbor.exe` pass with `VERSION_CONSISTENCY_OK`, 31 launcher groups, Portable version/smoke, 10 bridge protocol cases, modern-dialog scan, and security regression scan. Tests do not start Minecraft.
- `Publish-LocalRelease.ps1 -NoPublish` passed ephemeral RSA-3072/SHA-256 self-signing and `RELEASE_ARTIFACTS_PASSED=7`, removed the certificate/PFX, and the full suite passed again against the signed Portable EXE.
- Live handoff is intentionally limited to managed children started by this version's multi-server dashboard. The Java server hosted directly by the main launcher, manually started servers, and other programs' servers remain unsupported for lossless transfer.
- The [v1.14.0 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.14.0) contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.13.0->1.14.0` before rerunning all 31 launcher groups and 10 bridge cases against the updated public executable.

## 0.00 Current implementation state (v1.13.0 released)

- Release-state branch: `codex/v1.13.0-release-state` (feature: `codex/windows-notifications-v1.13.0`)
- Pull request [#34](https://github.com/Mangom72/MineHarbor/pull/34) is merged. PR CI `30187511660`, main CI `30187553178`, and release run `30187596732` passed.
- Version source of truth: `version.json` = 1.13.0 / build 26.2.45.77
- Windows taskbar notifications are opt-in and require the per-user background agent. Operations, Background settings, and the tray expose the themed notification settings form.
- `windows-notifications.json` is bounded to 64 KiB and uses exact schema validation, a path-derived cross-process mutex, and atomic replacement. Corrupt and future schemas remain untouched.
- Severity thresholds, six category groups, and local quiet hours that cross midnight are supported. Equal start/end represents a full quiet day.
- Each agent lifetime establishes a latest-entry baseline per server, so existing history is not replayed. Up to 50 pending events are bounded and an eight-second burst collapses to the most important recent event plus a remaining count.
- Notification text reuses and re-sanitizes operation summaries, never includes raw commands, and strips server paths, IPv4 addresses, and token/password/webhook-like values. Failures remain isolated from server control and automation.
- Local verification passes 30 launcher groups, 10 bridge protocol cases, Portable version/smoke, modern-dialog, and security scans. PR and main CI also passed the .NET 10 SDK warning-as-error `net48` build.
- The [v1.13.0 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.13.0) contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.12.0->1.13.0` before rerunning the full suite against the updated executable.
- Durable Windows notification-center storage/actions, dedicated player join/leave notifications, web/Discord remote control, and lossless transfer of a GUI-owned running server are not claimed.

## 0.0 Current implementation state (v1.12.0 released)

- Release-state branch: `codex/v1.12.0-release-state` (feature: `codex/background-agent-v1.12.0`)
- Pull request [#32](https://github.com/Mangom72/MineHarbor/pull/32) is merged. PR CI `30186372130`, main CI `30186411385`, and release run `30186453428` passed.
- Version source of truth: `version.json` = 1.12.0 / build 26.2.45.76
- The opt-in per-user `MineHarbor.exe --background-agent` owns tray-started managed-profile children and continues schedules after GUI exit. It does not install an elevated service.
- The tray provides profile start, safe stop, restart, immediate backup, console, stop-all, pause/resume, open GUI, and complete exit. The agent console reuses themed controls, local completion, and exact-command risk confirmation.
- GUI/agent IPC is a bounded local named pipe whose ACL grants the current Windows SID only. No LAN listener, arbitrary shell, or executable launch API is exposed.
- The agent refuses to command, stop, or live-back-up a running server it did not start. Managed children validate parent PID/start time and attempt a safe `stop` if the parent disappears.
- Automation JSON read-modify-write paths now use a path-derived cross-process mutex in addition to PID/start-time leases. Resume events re-evaluate existing bounded missed-run policies.
- Launcher update and agent exit request safe stops and abort rather than force exiting when an owned server misses the timeout.
- Local verification passes 29 launcher test groups, 10 bridge protocol cases, Portable version/smoke, modern-dialog, and security scans. PR and main CI also passed the .NET 10 SDK warning-as-error `net48` build.
- The [v1.12.0 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.12.0) contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.11.0->1.12.0` before rerunning the full suite against the updated executable.
- v1.12.0 did not include Windows notifications. They are introduced separately in v1.13.0. Web/Discord remote control, elevated/pre-sign-in service operation, cloud backup, Fabric/Forge/NeoForge bridges, and lossless transfer of a GUI-owned running server remain unclaimed.

## 0.1 Current implementation state (v1.11.0 released)

- Release-state branch: `codex/v1.11.0-release-state` (feature: `codex/operations-foundation`)
- Pull request [#30](https://github.com/Mangom72/MineHarbor/pull/30) is merged. PR CI `30183624045`, main CI `30183685789`, and release run `30183726971` passed.
- Version source of truth: `version.json` = 1.11.0 / build 26.2.45.75
- Automation schema 2 adds selected-weekday and one-time schedules, per-job missed-run policy (`run-once`, `skip`, or `notify-only`), a maximum-delay bound, one-time claim deduplication, and a pre-save preview of next run, risk, offline behavior, and five-minute conflicts.
- Schema-1 automation files migrate only in memory until the user saves. Corrupt and future schemas remain untouched and are rejected.
- Each server can retain up to 500 lifecycle and scheduled-job events in `.mineharbor/operations-history.json`. Entries use bounded validation, atomic replacement, a path-scoped cross-process mutex, redaction, and a SHA-256 hash chain. Read state is intentionally outside the audit hash.
- The main Operations view supports server/severity/unread filters, selected or bulk read state, refresh, CSV export, DPI scaling, themes, and accessibility metadata.
- Local verification passed with `VERSION_CONSISTENCY_OK`, `PASSED=28`, `PORTABLE_VERSION_OK`, `PORTABLE_SMOKE_OK`, `BRIDGE_PROTOCOL_PASSED=10`, `MODERN_DIALOG_SCAN_OK`, and `SECURITY_REGRESSION_SCAN_OK`. PR and main CI additionally passed the .NET 10 SDK warning-as-error `net48` build.
- The [v1.11.0 release](https://github.com/Mangom72/MineHarbor/releases/tag/v1.11.0) contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.10.0->1.11.0` before rerunning the full suite against the updated executable.
- No background agent, system tray, Windows notifications, web/Discord remote control, cloud backup, or Fabric/Forge/NeoForge bridge is claimed. Schedules still require an open MineHarbor main or multi-server management window.

## 0. Current implementation state (v1.10.0 released)

- Release-state branch: `codex/v1.10.0-release-state` (feature: `codex/command-builder-ux-v1.10.0`)
- Pull request [#28](https://github.com/Mangom72/MineHarbor/pull/28) is merged. Final PR CI `30169678662`, main CI `30169724770`, and release run `30169793820` passed.
- Version source of truth: `version.json` = 1.10.0 / build 26.2.45.74
- The quick-command field is now a step-by-step inline token builder backed by `QuickCommandBuilderState` and `QuickCommandTokenInput`.
- Templates distinguish required `{player}`, optional `[reason]`, and defaulted optional `[count=1]` arguments. Completion and send availability use required metadata plus value validation, not placeholder appearance.
- Confirming a candidate advances to the next argument without losing focus. Up/Down, Tab/Enter, Shift+Tab, and Esc support continuous keyboard composition while retaining values and per-command drafts.
- Player/target selectors, live player names, enum values, numeric/time/coordinate recommendations, and searchable item/effect catalogs are available. Quick-command suggestions expand to 430px/40 candidates; shared completion expands to 380px/20 candidates and avoids the input bounds.
- Local and published-update validation passed all 27 launcher groups, 10 bridge protocol cases, Portable version/smoke, modern-dialog, and security scans. CI passed the SDK-style `net48` build with warnings as errors.
- The v1.10.0 release contains seven verified public assets. The release workflow removed its temporary self-signed certificate/PFX, and an independent download confirmed `RELEASE_ARTIFACTS_PASSED=7`, `PUBLISHED_ASSETS_MODE_OK`, and `PUBLIC_AUTO_UPDATE_OK=1.9.0->1.10.0` before rerunning the full suite against the downloaded executable.

## Previous v1.9.0 release state

- Release-state branch: `codex/v1.9.0-release-state` (feature: `codex/quick-command-safety-v1.9.0`)
- Version source of truth: `version.json` = 1.9.0 / build 26.2.45.73
- The built-in quick-command catalog now contains more than 70 entries, including player/IP sanctions, idle limits, seed/time queries, gamerules, data packs, reload, and read-only experience, world-border, and force-load checks.
- Templates distinguish required `{player}` arguments from trailing optional `[reason]` arguments. Final reason, message, and command values may consume multiple words, and local completion includes the new address, duration, minutes, percentage, weather, gamerule, datapack, function, dimension, distance, and hide-particles types.
- Definitions may set minimum and maximum Minecraft versions. Datapack/force-load commands require 1.13, sleeping percentage requires 1.17, and the legacy `time query daytime` suggestion stops after 1.21.11.
- The declared `Normal`, `Confirm`, and `Dangerous` risk levels now drive picker colors and both quick-command and direct-console confirmation flows. Value-sensitive rules raise `reload`, broad mutations, `keepInventory false`, and `doMobSpawning false`, while read-only `worldborder get` and `forceload query` remain normal.
- The user-command editor exposes a themed risk selector and accessible version-bound fields. Existing `Confirm` JSON migrates without losing behavior.
- The v1.8.0 public update path succeeded, but a repeated full-suite run while the desktop was in use exposed that the close-release guard still depended on the live pointer remaining within 64px. The paired release is now consumed regardless of pointer movement; only subsequent presses remain coordinate-scoped.
- Server settings expose three independent Paper-compatible controls: piston/TNT/rail/carpet duplication, gravity-block end-portal duplication, and tripwire-hook duplication.
- Paper/Purpur 1.19+ uses top-level `unsupported-settings` in `config/paper-global.yml`; older versions use nested `settings.unsupported-settings` in `paper.yml`. Gravity-block controls require 1.20.4+, tripwire controls require 1.21.4+, and custom JARs expose newer controls only for keys already generated. Spigot, Vanilla, Fabric, Forge, and NeoForge never receive Paper-only keys.
- Existing profiles keep `manage-duplication-settings=false` until the settings dialog is saved, preventing a version upgrade from silently overwriting manually maintained YAML.
- YAML writes reject malformed or ambiguous managed sections, path escapes, and reparse points; effective changes create at most five `.mineharbor/configuration-backups` copies per target filename.
- The setup dialog uses the existing themed group and check boxes, removes its former 146px empty region, and avoids scrolling at normal size. Static UI validation covers standard controls and DPI scaling across all 22 forms.
- New per-server state: `.mineharbor/content-manifest.json` and `.mineharbor/automation.json`
- New user areas: installed content/Modrinth/data packs, backup schedules and commands, and a live server status dashboard
- New service boundaries: `ContentManagementServices.cs`, `ServerAutomation.cs`, and the UI integration in `ContentManagementUi.cs` / `ServerManagementFeatures.cs`
- Tool-window title-bar closes retain a short coordinate-scoped input guard, so mouse double-click chatter cannot immediately activate an overlapping launcher control while deliberate clicks elsewhere remain available.
- User-initiated launcher close now always asks for confirmation. Idle close exits directly without invoking server termination; active work and a running server use separate Korean/English wording and retain deferred or safe-stop behavior.
- The quick-command card now remains in a fixed legacy-width right column. The console uses a separate left column, so toggling it no longer moves the card or leaves console content hidden behind it.
- Final local v1.9.0 validation passed 27 launcher test groups, 10 bridge protocol cases, Portable smoke/version, static UI and security scans. Seven release artifacts passed the ephemeral RSA-3072/SHA-256 self-signing, installer, archive, bridge, and checksum verification path, and the temporary certificate and PFX were removed. No real server, router, port mapping, or foreground UI was changed. No system-wide .NET SDK is installed, so the SDK-style `net48` build remains a required GitHub CI gate.
- CI: `.github/workflows/ci.yml` validates PRs and main separately from `.github/workflows/build-release.yml`; both build the SDK-style `net48` project and the legacy Portable path. Feature PR #25, its run `30162595403`, main run `30162646922`, and release run `30162698640` passed. The release run removed its ephemeral certificate/PFX and verified all seven public assets. A separately downloaded public v1.8.1 launcher updated to v1.9.0, and the resulting executable passed the same 27 launcher groups, 10 bridge cases, Portable, UI, and security regression suite.
- The default runtime remains .NET Framework 4.8. `MineHarbor.csproj` is the migration bridge; do not switch to .NET 10 until updater, COM/UPnP, WinForms, installer, and Portable compatibility tests are equivalent.
- Do not infer dashboard values. Paper/Purpur TPS/MSPT is shown only when the local bridge reports it; unsupported or disconnected values remain explicit.

This document was created for handoff purposes by analyzing the previous AI (Codex) chat history (`CODEX_CHAT_HISTORY.md`) and the current state of the project.

## 1. Ultimate Goal of the Program
**MineHarbor (Minecraft Server Launcher)**: A modern GUI server launcher for Windows that helps users easily create various types of Minecraft servers (Paper, Purpur, Vanilla, Fabric, Forge, NeoForge) without complex configuration. It automatically manages Java runtimes and server files, assists with external access via UPnP/port-forwarding, and provides multi-server management, Modrinth plugin/mod installation, backups, and command auto-completion.

## 2. User Confirmed Requirements
- **UI/UX**: Modern and rounded design inspired by the "Toss" app. Remove unnecessary alert dialogs, use responsive layouts, and support dark/light themes. Keep button texts concise and use hover tooltips for detailed descriptions.
- **Server Environment**: Automatically download Java runtimes and server files (no offline caching embedded in the EXE).
- **Auto-Update**: Support auto-updating for both the launcher itself (via `update.json` and GitHub Releases) and server files. Removed the minimum 1MB padding restriction for the launcher file.
- **Port Forwarding**: Attempt automatic UPnP mapping if port forwarding fails. Monitor connection status and provide manual setup guides upon failure. Delete ONLY the ports opened by the launcher when closing.
- **Multi-Server Management**: Create, duplicate, delete (with a 30-day trash bin and permanent deletion), archive, and set a default server.
- **Command Bridge**: Communicate with Paper/Purpur servers (127.0.0.1) to provide console and plugin command auto-completion. Apply safety prompts for dangerous commands like `stop`.
- **Localization**: Support Korean and English.
- **No Personal Hardcoding**: Removed hardcoded OP / log-hiding features tied to a specific username (`Mangom72`). Replaced with a generalized "Server Owner" OP system. Repository and executable names are unified to `MineHarbor`.

## 3. Implemented Features So Far
- Single Portable EXE execution and Inno Setup-based installer support.
- Multi-server/profile management with isolated data per server.
- Automatic downloading and caching of server files and Java.
- UPnP automatic mapping, port monitoring, and external IP copying.
- Server Start/Safe Stop, console viewer, and filtering (e.g., coloring harmless compatibility warnings in blue).
- Command auto-completion (Paper/Purpur bridge integration and custom command UI).
- Player management (OP, Whitelist, Kick, Ban).
- Plugin/Mod search and download (via Modrinth API).
- Full backup and 30-day trash bin logic.
- CI/CD release pipeline via GitHub Actions (Automated build, test, and SHA-256 validation).

## 4. Features In Progress or Incomplete
- **Fabric/Forge Bridge**: External bridge command auto-completion for non-Paper servers (like Fabric) is not yet implemented.
- **Authenticode trust**: Release files are self-signed with an ephemeral per-release certificate. This proves embedded-signature integrity but does not establish a publicly trusted publisher or SmartScreen reputation, so warnings can still appear.
- **High DPI Scaling**: Needs further testing and optimization for edge cases like 125%, 150%+ multi-monitor environments.
- **Modern .NET Runtime**: The repository now has an SDK-style `net48` project, but moving the shipped runtime to .NET 10 remains a staged migration rather than a completed target switch.
- **Metrics Bridge Coverage**: TPS/MSPT metrics are implemented for the Paper/Purpur bridge. Fabric/Forge and Vanilla expose these values as unsupported instead of estimates.

## 5. User Rejected or Discarded Ideas
- **Hardcoded Username Features**: The exclusive auto-OP and log-hiding features for a specific user were rejected for security and versatility.
- **Embedded Server JAR**: Initially planned to embed the server JAR inside the EXE for faster first execution, but rejected due to massive file size inflation. Opted for online downloads with hash verification.
- **1MB Minimum Padding**: The forced dummy data to meet older launcher update mechanisms was used once as a hotfix and then discarded.
- **Long Button Texts**: Rejected designs where long text caused truncation or ellipses (`...`). Replaced with short words and tooltips.

## 6. Important Design Decisions & Rationale
- **`version.json` as SSOT**: `productVersion` and `buildNumber` are managed centrally in `version.json` for both the app and GitHub Actions.
- **Separated UI and Background Logic**: Modularized UPnP, bridge communication, and server management logic into separate classes to reduce complexity.
- **Bridge Communication Security**: Uses a local (127.0.0.1) connection with randomized session token validation to ensure security.
- **STA Thread for UPnP**: UPnP utilizes Windows COM objects, so a Single-Threaded Apartment (STA) model was strictly applied to prevent crashes.
- **Logical Deletion (Trash Bin)**: To prevent accidental data loss, deleted servers are moved to a `servers-trash` directory and preserved for 30 days.

## 7. Discovered Errors and Resolutions
- **Old Paper Versions Crashing Early**: The `--nogui` flag caused errors on older versions. Resolved by applying `--nogui` only to Vanilla/Fabric.
- **Misunderstood Compatibility Warnings**: `sun.misc.Unsafe` warnings are harmless, so they are displayed in blue text to avoid panic.
- **Old JAR Path Errors in Java 11 (Korean Paths)**: Changed execution arguments from absolute paths to relative paths.
- **UI Button Activation Issues**: Fixed a bug where player management buttons remained disabled after the server started.
- **Horizontal Scrolling Issues**: Replaced static coordinates with a responsive `TableLayoutPanel` and dynamic scrollbar UI to adapt to Dark/Light themes properly.

## 8. Current Role per File
- `ModernLauncherGui.cs`: Main window, UI state, and main event loop.
- `ManagedServerDashboard.cs`: Dashboard for switching and managing multiple servers.
- `QuickCommandUi.cs` / `QuickCommandPickerUi.cs`: UI for console commands and the auto-completion popup.
- `QuickCommandsAndBridge.cs`: Communication protocols and bridge handling with Minecraft servers.
- `UpnpExternalAccess.cs`: Handles UPnP communication if port forwarding fails.
- `StorageConfiguration.cs`: Manages user data storage paths (LocalAppData vs. current directory).
- `BackupAndProfileTools.cs`: Manages server profile copying, backups, and restores.
- `ContentAndDiagnostics.cs`: Installation logic for plugins and mods (Modrinth API).
- `ServerTrash.cs`: Manages the 30-day trash bin for server deletion.
- `RuntimeCompatibility.cs`: Java environment checking, downloading, and version management.
- `NetworkAndPlayerTools.cs`: Player permissions management (OP, Ban, Whitelist, etc.).
- `ModernDialogs.cs`: Custom alert/confirmation windows with the modern design.

## 9. Behaviors That MUST Be Maintained
- **UPnP Port Protection**: Only delete ports that were explicitly opened by the launcher. NEVER touch existing user mappings.
- **Data Backup Priority**: Always prompt for a backup or show a warning before executing potentially breaking version changes.
- **`version.json` Dependency**: The CI pipeline relies on this file to package release assets. Its integrity must be preserved.
- **No Personal Identifiers**: No special privileges should be granted to `Mangom72` or any specific user in the logic (except for standard GitHub URLs).

## 10. Next Steps / Tasks to be Done
- Keep the PR/main Windows SDK/legacy build gates and tagged release verification required when extending this work.
- Expand bridge command completion and TPS/MSPT metrics to Fabric/Forge if a stable server-side API is selected.
- If public publisher trust is later required, evaluate a managed open-source signing service without weakening the current ephemeral self-signing and hash-verification gates.
- Exercise 125%, 150%, and mixed-DPI multi-monitor layouts with representative long Korean and English strings.
- Continue the .NET 10 migration sequence in `docs/architecture/DOTNET_MODERNIZATION.md` without changing the shipped target until compatibility gates pass.

## 11. Conflicts Between History and Actual Code
- **File Name Changes**: Chat history initially mentions `Paper-26.2-Server.exe` or `Minecraft-Server-Launcher.exe`, but the final applied app name is `MineHarbor.exe`. All logic and docs must adhere to this latest name.
- **Admin Features**: `Mangom72Admin` modes and log-hiding features only exist in historical records and have been completely removed from the actual codebase.

## 12. Project Maintenance and Deployment Policies (Confirmed from Chat History)
- **Version Bump Criteria (`productVersion`)**: Follows Semantic Versioning. Bug fixes bump the patch (`x.x.1`), new features bump the minor (`x.1.x`), and breaking changes bump the major (`1.x.x`) version.
- **Internal Build Number (`buildNumber`)**: When updating `version.json`, increment the very last digit by 1 alongside the `productVersion` (e.g., `26.2.45.36` -> `26.2.45.37`).
- **Deployment Workflow**: Never commit or push directly to `main`. Push a `codex/` feature branch, require the PR CI to pass, merge through review, and only then create the matching `v1.x.x` tag or explicitly dispatch the release workflow. The release workflow rebuilds, tests, packages, verifies hashes, and publishes the final release.
