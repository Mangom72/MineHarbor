using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static partial class Launcher
{
	private sealed class DiscordRemoteSettingsForm : Form
	{
		private readonly ModernCheckBox enabledBox;
		private readonly ModernTextBox tokenBox;
		private readonly ModernCheckBox removeTokenBox;
		private readonly ModernTextBox applicationIdBox;
		private readonly ModernTextBox guildIdBox;
		private readonly ModernTextBox channelIdBox;
		private readonly ModernTextBox allowedUsersBox;
		private readonly ModernTextBox allowedRolesBox;
		private readonly CheckedListBox profilesBox;
		private readonly Label statusLabel;
		private readonly string loadError;
		private readonly string existingProtectedToken;
		private bool refreshing;

		public DiscordRemoteSettingsForm()
		{
			bool korean = IsManagedKorean();
			Text = korean ? "Discord 원격 제어 (베타)" : "Discord remote control (Beta)";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(860, 650);
			Size = new Size(940, 720);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			ApplyLauncherWindowIcon(this);

			DiscordRemoteSettings settings;
			string settingsLoadError = null;
			try { settings = ReadDiscordRemoteSettings(); }
			catch (Exception exception)
			{
				settings = new DiscordRemoteSettings();
				settingsLoadError = exception.Message;
			}
			loadError = settingsLoadError;
			existingProtectedToken = settings.ProtectedBotToken ?? string.Empty;

			TableLayoutPanel root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(24),
				ColumnCount = 1,
				RowCount = 6
			};
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);

			root.Controls.Add(new Label
			{
				AutoSize = true,
				Font = new Font(ThemeFonts.Display, 18F, FontStyle.Bold),
				Text = korean ? "Discord에서 허용된 서버만 안전하게 관리합니다" : "Safely manage approved servers from Discord",
				Margin = new Padding(0, 0, 0, 8)
			}, 0, 0);
			root.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(860, 0),
				Text = korean
					? "공개 포트를 열지 않는 길드 전용 /mineharbor 명령입니다. 허용 사용자·역할·채널·서버를 모두 검사하며, 안전 종료와 재시작은 60초 확인 버튼을 거칩니다. 임의 콘솔 명령은 제공하지 않습니다."
					: "This guild-only /mineharbor command opens no public listener. It checks approved users, roles, channel, and servers; safe stop and restart require a 60-second confirmation. Arbitrary console commands are not exposed.",
				Margin = new Padding(0, 0, 0, 12)
			}, 0, 1);

			enabledBox = new ModernCheckBox
			{
				AutoSize = true,
				Text = korean ? "Discord 원격 제어 사용 (기본값 꺼짐)" : "Enable Discord remote control (off by default)",
				Checked = settings.Enabled,
				Margin = new Padding(0, 2, 0, 12)
			};
			ConfigureAccessibleField(enabledBox, enabledBox.Text, korean
				? "백그라운드 에이전트와 Discord 봇을 사용자가 명시적으로 연결합니다."
				: "Explicitly connects the background agent to a Discord bot.");
			root.Controls.Add(enabledBox, 0, 2);

			TableLayoutPanel columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
			columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
			columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
			root.Controls.Add(columns, 0, 3);

			ModernGroupBox connectionGroup = new ModernGroupBox
			{
				Text = korean ? "Discord 앱 연결" : "Discord app connection",
				Dock = DockStyle.Fill,
				Padding = new Padding(16),
				Margin = new Padding(0, 0, 10, 0)
			};
			TableLayoutPanel connection = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 12 };
			for (int row = 0; row < 12; row++)
			{
				if (row % 2 == 0) connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				else if (row >= 9) connection.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
				else connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
			}
			connectionGroup.Controls.Add(connection);
			columns.Controls.Add(connectionGroup, 0, 0);

			tokenBox = NewDiscordTextBox(korean ? "새 봇 토큰 — 비워 두면 기존 토큰 유지" : "New bot token — leave blank to keep existing", false);
			tokenBox.UseSystemPasswordChar = true;
			applicationIdBox = NewDiscordTextBox(korean ? "Discord 애플리케이션 ID" : "Discord application ID", false);
			guildIdBox = NewDiscordTextBox(korean ? "Discord 서버 ID" : "Discord server ID", false);
			channelIdBox = NewDiscordTextBox(korean ? "명령을 허용할 채널 ID" : "Allowed command channel ID", false);
			allowedUsersBox = NewDiscordTextBox(korean ? "허용 사용자 ID — 쉼표 또는 줄바꿈" : "Allowed user IDs — commas or new lines", true);
			allowedRolesBox = NewDiscordTextBox(korean ? "허용 역할 ID — 선택 사항" : "Allowed role IDs — optional", true);
			applicationIdBox.Text = settings.ApplicationId;
			guildIdBox.Text = settings.GuildId;
			channelIdBox.Text = settings.ChannelId;
			allowedUsersBox.Text = string.Join(Environment.NewLine, settings.AllowedUserIds.ToArray());
			allowedRolesBox.Text = string.Join(Environment.NewLine, settings.AllowedRoleIds.ToArray());

			AddDiscordField(connection, 0, korean ? "봇 토큰" : "Bot token", tokenBox, korean
				? "토큰은 현재 Windows 사용자 범위 DPAPI로 암호화해 저장합니다."
				: "The token is stored with current-user Windows DPAPI encryption.");
			AddDiscordField(connection, 2, korean ? "애플리케이션 ID" : "Application ID", applicationIdBox, korean ? "Discord Developer Portal의 Application ID입니다." : "The Application ID from the Discord Developer Portal.");
			AddDiscordField(connection, 4, korean ? "서버 ID" : "Server ID", guildIdBox, korean ? "명령을 등록할 Discord 서버입니다." : "The Discord server where the command is registered.");
			AddDiscordField(connection, 6, korean ? "채널 ID" : "Channel ID", channelIdBox, korean ? "이 채널에서만 명령을 허용합니다." : "Commands are accepted only in this channel.");
			AddDiscordField(connection, 8, korean ? "허용 사용자" : "Allowed users", allowedUsersBox, korean ? "사용자 ID를 쉼표 또는 줄바꿈으로 구분합니다." : "Separate user IDs with commas or new lines.");
			AddDiscordField(connection, 10, korean ? "허용 역할 (선택)" : "Allowed roles (optional)", allowedRolesBox, korean ? "역할 ID를 쉼표 또는 줄바꿈으로 구분합니다." : "Separate role IDs with commas or new lines.");

			ModernGroupBox authorizationGroup = new ModernGroupBox
			{
				Text = korean ? "허용 서버와 보안 경계" : "Approved servers and security boundary",
				Dock = DockStyle.Fill,
				Padding = new Padding(16),
				Margin = new Padding(10, 0, 0, 0)
			};
			TableLayoutPanel authorization = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorization.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorizationGroup.Controls.Add(authorization);
			columns.Controls.Add(authorizationGroup, 1, 0);
			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				Text = korean ? "Discord에서 관리할 서버 프로필" : "Server profiles manageable from Discord",
				Margin = new Padding(0, 0, 0, 6)
			}, 0, 0);
			profilesBox = new CheckedListBox
			{
				Dock = DockStyle.Fill,
				CheckOnClick = true,
				BorderStyle = BorderStyle.None,
				IntegralHeight = false,
				AccessibleName = korean ? "Discord 허용 서버 프로필" : "Discord-approved server profiles",
				AccessibleDescription = korean ? "Discord 원격 명령에서 접근할 수 있는 서버만 선택합니다." : "Select only servers accessible to Discord remote commands."
			};
			List<ManagedProfileRecord> profiles;
			try { profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory)); }
			catch { profiles = new List<ManagedProfileRecord>(); }
			for (int index = 0; index < profiles.Count; index++)
			{
				int item = profilesBox.Items.Add(profiles[index].Name);
				if (settings.AllowedProfiles.Contains(profiles[index].Name, StringComparer.OrdinalIgnoreCase))
					profilesBox.SetItemChecked(item, true);
			}
			RoundedPanel profileSurface = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(10), CornerRadius = 9, Tag = "input-surface", Margin = new Padding(0, 0, 0, 10) };
			profileSurface.Controls.Add(profilesBox);
			authorization.Controls.Add(profileSurface, 0, 1);

			removeTokenBox = new ModernCheckBox
			{
				AutoSize = true,
				Text = korean ? "저장된 봇 토큰 제거" : "Remove the saved bot token",
				Enabled = !string.IsNullOrEmpty(existingProtectedToken),
				Margin = new Padding(0, 2, 0, 8)
			};
			ConfigureAccessibleField(removeTokenBox, removeTokenBox.Text, korean
				? "저장할 때 암호화된 토큰을 삭제하며 원격 제어도 꺼야 합니다."
				: "Deletes the encrypted token on save; remote control must also be disabled.");
			authorization.Controls.Add(removeTokenBox, 0, 2);
			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(350, 0),
				Text = korean
					? "지원: 상태·플레이어·최근 오류 조회, 시작, 확인형 안전 종료·재시작, 백업\n차단: 임의 콘솔, 셸·파일 실행, 허용되지 않은 서버, 외부 소유 프로세스"
					: "Allowed: status, players, recent errors, start, confirmed safe stop/restart, backup\nBlocked: arbitrary console, shell/file execution, unapproved servers, externally owned processes",
				Margin = new Padding(0, 2, 0, 10)
			}, 0, 3);
			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(350, 0),
				Text = korean
					? "봇을 Discord 서버에 bot 및 applications.commands 범위로 설치하세요. 특권 Gateway Intent는 필요하지 않습니다."
					: "Install the bot to the Discord server with bot and applications.commands scopes. Privileged Gateway Intents are not required.",
				Margin = new Padding(0)
			}, 0, 4);

			statusLabel = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(860, 0),
				Text = !string.IsNullOrEmpty(loadError)
					? (korean ? "설정을 검증하지 못해 원본을 보존했습니다. 파일을 확인한 뒤 다시 열어 주세요." : "The settings file could not be verified and was preserved. Review it, then reopen this window.")
					: (korean ? "연결 상태를 확인하는 중…" : "Checking connection status…"),
				Margin = new Padding(0, 12, 0, 8)
			};
			statusLabel.AccessibleName = statusLabel.Text;
			root.Controls.Add(statusLabel, 0, 4);

			FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			Button refresh = MultiServerDashboardForm.NewManagedButton(korean ? "상태 새로고침" : "Refresh status", 136, "secondary");
			Button save = MultiServerDashboardForm.NewManagedButton(korean ? "저장 및 연결" : "Save and connect", 136, "primary");
			Button cancel = MultiServerDashboardForm.NewManagedButton(korean ? "취소" : "Cancel", 100, "secondary");
			refresh.Click += delegate { RefreshStatusAsync(); };
			save.Click += delegate { SaveSettings(); };
			cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			refresh.Enabled = string.IsNullOrEmpty(loadError);
			save.Enabled = string.IsNullOrEmpty(loadError);
			buttons.Controls.AddRange(new Control[] { refresh, save, cancel });
			root.Controls.Add(buttons, 0, 5);

			enabledBox.CheckedChanged += delegate { UpdateEnabledState(); };
			removeTokenBox.CheckedChanged += delegate { UpdateEnabledState(); };
			Shown += delegate { RefreshStatusAsync(); };
			UpdateEnabledState();
			ApplySimpleDialogTheme(this);
			ApplyCommonButtonToolTips(this);
		}

		private static ModernTextBox NewDiscordTextBox(string cueText, bool multiline)
		{
			return new ModernTextBox
			{
				CueText = cueText,
				Multiline = multiline,
				AcceptsReturn = multiline,
				ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
				Dock = DockStyle.Fill
			};
		}

		private static void AddDiscordField(TableLayoutPanel layout, int row, string label, ModernTextBox box, string description)
		{
			Label title = new Label { AutoSize = true, Text = label, Margin = new Padding(0, row == 0 ? 0 : 5, 0, 4) };
			layout.Controls.Add(title, 0, row);
			RoundedPanel surface = CreateModernTextBoxSurface(box, 9);
			surface.Dock = DockStyle.Fill;
			surface.Margin = new Padding(0, 0, 0, 3);
			layout.Controls.Add(surface, 0, row + 1);
			ConfigureAccessibleField(box, label, description);
		}

		private void UpdateEnabledState()
		{
			bool enabled = enabledBox.Checked;
			tokenBox.Enabled = enabled && !removeTokenBox.Checked;
			applicationIdBox.Enabled = enabled;
			guildIdBox.Enabled = enabled;
			channelIdBox.Enabled = enabled;
			allowedUsersBox.Enabled = enabled;
			allowedRolesBox.Enabled = enabled;
			profilesBox.Enabled = enabled;
			if (removeTokenBox.Checked) enabledBox.Checked = false;
		}

		private DiscordRemoteSettings BuildSettings()
		{
			string protectedToken = existingProtectedToken;
			if (removeTokenBox.Checked) protectedToken = string.Empty;
			else if (!string.IsNullOrWhiteSpace(tokenBox.Text)) protectedToken = ProtectDiscordBotToken(tokenBox.Text.Trim());
			List<string> selectedProfiles = new List<string>();
			foreach (object item in profilesBox.CheckedItems) selectedProfiles.Add(Convert.ToString(item));
			return new DiscordRemoteSettings
			{
				Enabled = enabledBox.Checked,
				ProtectedBotToken = protectedToken,
				ApplicationId = applicationIdBox.Text.Trim(),
				GuildId = guildIdBox.Text.Trim(),
				ChannelId = channelIdBox.Text.Trim(),
				AllowedUserIds = ParseDiscordIdText(allowedUsersBox.Text),
				AllowedRoleIds = ParseDiscordIdText(allowedRolesBox.Text),
				AllowedProfiles = selectedProfiles
			};
		}

		private void SaveSettings()
		{
			if (!string.IsNullOrEmpty(loadError)) return;
			bool korean = IsManagedKorean();
			DiscordRemoteSettings previous = null;
			bool settingsWritten = false;
			try
			{
				previous = ReadDiscordRemoteSettings();
				DiscordRemoteSettings settings = BuildSettings();
				if (settings.Enabled)
				{
					BackgroundAgentSettings background = ReadBackgroundAgentSettings();
					if (!background.Enabled)
						throw new InvalidOperationException(korean
							? "먼저 백그라운드 운영을 켜 주세요. Discord 연결은 백그라운드 에이전트에서만 실행됩니다."
							: "Enable background operations first. The Discord connection runs only in the background agent.");
				}
				WriteDiscordRemoteSettings(settings);
				settingsWritten = true;
				if (settings.Enabled)
				{
					if (!EnsureBackgroundAgentRunning())
						throw new InvalidOperationException(korean ? "백그라운드 에이전트를 시작하지 못했습니다." : "Could not start the background agent.");
					BackgroundAgentResponse reload = SendBackgroundAgentRequest("reload-discord", null, null, 1500);
					if (reload == null || !reload.Success)
						throw new InvalidOperationException(korean ? "에이전트에 Discord 설정을 전달하지 못했습니다." : "Could not reload Discord settings in the agent.");
				}
				else if (IsBackgroundAgentRunning())
				SendBackgroundAgentRequest("reload-discord", null, null, 1500);
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception exception)
			{
				if (settingsWritten && previous != null)
				{
					try
					{
						WriteDiscordRemoteSettings(previous);
						if (IsBackgroundAgentRunning()) SendBackgroundAgentRequest("reload-discord", null, null, 1500);
					}
					catch { }
				}
				ShowMineHarborDialog(this,
					(korean ? "Discord 원격 제어 설정을 저장하지 못했습니다: " : "Could not save Discord remote-control settings: ") + exception.Message,
					Text,
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private async void RefreshStatusAsync()
		{
			if (refreshing || IsDisposed || !string.IsNullOrEmpty(loadError)) return;
			refreshing = true;
			try
			{
				bool korean = IsManagedKorean();
				BackgroundAgentResponse response = await Task.Run(delegate
				{
					return SendBackgroundAgentRequest("discord-status", null, null, 1200);
				});
				if (IsDisposed) return;
				statusLabel.Text = response == null
					? (korean ? "백그라운드 에이전트가 실행 중이 아닙니다." : "The background agent is not running.")
					: response.Message;
				statusLabel.AccessibleName = statusLabel.Text;
			}
			finally { refreshing = false; }
		}
	}

	private static List<string> ParseDiscordIdText(string text)
	{
		string[] values = (text ?? string.Empty).Split(new char[] { ',', ';', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		return NormalizeDiscordIdList(values, "입력한");
	}
}
