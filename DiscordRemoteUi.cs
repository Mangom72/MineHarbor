using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static partial class Launcher
{
	private static bool IsDiscordRemoteRegistrationConfigured(DiscordRemoteSettings settings)
	{
		return settings != null
			&& !string.IsNullOrWhiteSpace(settings.ProtectedBotToken)
			&& IsDiscordSnowflake(settings.ApplicationId)
			&& IsDiscordSnowflake(settings.GuildId)
			&& IsDiscordSnowflake(settings.ChannelId)
			&& ((settings.AllowedUserIds != null && settings.AllowedUserIds.Count > 0)
				|| (settings.AllowedRoleIds != null && settings.AllowedRoleIds.Count > 0))
			&& settings.AllowedProfiles != null
			&& settings.AllowedProfiles.Count > 0;
	}

	private static bool ShouldShowDiscordRegistrationGuide()
	{
		try { return !IsDiscordRemoteRegistrationConfigured(ReadDiscordRemoteSettings()); }
		catch { return false; }
	}

	private static void OpenDiscordRemoteSettings(IWin32Window owner, bool modal)
	{
		if (ShouldShowDiscordRegistrationGuide())
		{
			using (DiscordRemoteRegistrationGuideForm guide = new DiscordRemoteRegistrationGuideForm())
			{
				DialogResult result = owner == null ? guide.ShowDialog() : guide.ShowDialog(owner);
				if (result != DialogResult.OK) return;
			}
		}
		if (modal)
		{
			using (DiscordRemoteSettingsForm form = new DiscordRemoteSettingsForm())
			{
				if (owner == null) form.ShowDialog();
				else form.ShowDialog(owner);
			}
			return;
		}
		DiscordRemoteSettingsForm settingsForm = new DiscordRemoteSettingsForm();
		settingsForm.FormClosed += delegate { settingsForm.Dispose(); };
		settingsForm.Show();
	}

	private sealed class DiscordRemoteRegistrationGuideForm : Form
	{
		private const string DiscordDeveloperPortalUrl = "https://discord.com/developers/applications";
		private readonly Panel stepsViewport;
		private readonly TableLayoutPanel stepsPanel;
		private readonly RoundedPanel securityPanel;
		private readonly Label securityLabel;
		private readonly Button portalButton;
		private readonly Button startButton;
		private readonly Button laterButton;
		private readonly List<Label> stepNumbers = new List<Label>();
		private readonly List<Label> stepDescriptions = new List<Label>();

		public DiscordRemoteRegistrationGuideForm()
		{
			bool korean = IsManagedKorean();
			Text = korean ? "Discord 원격 제어 시작 가이드" : "Discord remote-control setup guide";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(680, 520);
			Size = new Size(760, 650);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			MaximizeBox = false;
			MinimizeBox = false;
			AccessibleName = Text;
			AccessibleDescription = korean
				? "Discord 봇 등록에 필요한 네 단계를 설명합니다."
				: "Explains the four steps required to register a Discord bot.";
			ApplyLauncherWindowIcon(this);

			TableLayoutPanel root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(28, 24, 28, 24),
				ColumnCount = 1,
				RowCount = 5
			};
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);

			root.Controls.Add(new Label
			{
				AutoSize = true,
				Font = new Font(ThemeFonts.Display, 20F, FontStyle.Bold),
				Text = korean ? "Discord 연결을 먼저 준비해 주세요" : "Set up your Discord connection first",
				Margin = new Padding(0, 0, 0, 8)
			}, 0, 0);
			root.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(680, 0),
				Text = korean
					? "아직 등록된 Discord 앱이 없습니다. 아래 순서대로 한 번만 설정하면 허용된 채널에서 서버를 안전하게 관리할 수 있습니다."
					: "No Discord app is registered yet. Complete these steps once to manage approved servers safely from an approved channel.",
				Tag = "muted",
				Margin = new Padding(0, 0, 0, 14)
			}, 0, 1);

			stepsViewport = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				Margin = Padding.Empty,
				AccessibleName = korean ? "Discord 등록 단계" : "Discord registration steps"
			};
			stepsPanel = new TableLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				ColumnCount = 1,
				RowCount = 4,
				Margin = Padding.Empty
			};
			for (int index = 0; index < 4; index++) stepsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			stepsPanel.Controls.Add(CreateDiscordGuideStep(1,
				korean ? "Discord 앱과 봇 만들기" : "Create a Discord app and bot",
				korean ? "Developer Portal에서 새 애플리케이션을 만들고 Bot 페이지에서 봇을 추가합니다." : "Create an application in the Developer Portal and add its bot from the Bot page."), 0, 0);
			stepsPanel.Controls.Add(CreateDiscordGuideStep(2,
				korean ? "내 Discord 서버에 설치하기" : "Install it to your Discord server",
				korean ? "Guild Install에 bot과 applications.commands 범위를 선택해 관리할 서버에 설치합니다." : "Use Guild Install with the bot and applications.commands scopes for the server you manage."), 0, 1);
			stepsPanel.Controls.Add(CreateDiscordGuideStep(3,
				korean ? "필요한 ID 복사하기" : "Copy the required IDs",
				korean ? "개발자 모드를 켠 뒤 애플리케이션·서버·채널과 허용할 사용자 또는 역할 ID를 복사합니다." : "Enable Developer Mode, then copy the application, guild, channel, and approved user or role IDs."), 0, 2);
			stepsPanel.Controls.Add(CreateDiscordGuideStep(4,
				korean ? "MineHarbor에서 연결하기" : "Connect it in MineHarbor",
				korean ? "백그라운드 운영을 켜고 토큰·ID·허용 서버를 입력한 뒤 저장 및 연결을 누릅니다." : "Enable Background operations, enter the token, IDs, and approved servers, then choose Save and connect."), 0, 3);
			stepsViewport.Controls.Add(stepsPanel);
			root.Controls.Add(stepsViewport, 0, 2);

			securityPanel = new RoundedPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				CornerRadius = 12,
				Tag = "input-surface",
				Padding = new Padding(16, 10, 16, 10),
				Margin = new Padding(0, 12, 0, 12),
				AccessibleName = korean ? "Discord 보안 안내" : "Discord security note"
			};
			securityLabel = new Label
			{
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft,
				Text = korean
					? "토큰은 현재 Windows 사용자만 복호화할 수 있게 저장됩니다. 임의 콘솔·셸·파일 명령은 Discord에 노출되지 않습니다."
					: "The token is stored so only the current Windows user can decrypt it. Discord never exposes arbitrary console, shell, or file commands.",
				Tag = "muted"
			};
			securityPanel.Controls.Add(securityLabel);
			root.Controls.Add(securityPanel, 0, 3);

			FlowLayoutPanel buttons = new FlowLayoutPanel
			{
				AutoSize = true,
				Dock = DockStyle.Right,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Margin = Padding.Empty
			};
			portalButton = MultiServerDashboardForm.NewManagedButton(korean ? "Developer Portal 열기" : "Open Developer Portal", 178, "secondary");
			startButton = MultiServerDashboardForm.NewManagedButton(korean ? "설정 시작" : "Start setup", 126, "primary");
			laterButton = MultiServerDashboardForm.NewManagedButton(korean ? "나중에" : "Not now", 104, "secondary");
			ConfigureAccessibleField(portalButton, portalButton.Text, korean ? "기본 브라우저에서 Discord Developer Portal을 엽니다." : "Opens the Discord Developer Portal in the default browser.");
			ConfigureAccessibleField(startButton, startButton.Text, korean ? "가이드를 닫고 Discord 설정 입력 화면으로 이동합니다." : "Closes the guide and opens Discord settings.");
			ConfigureAccessibleField(laterButton, laterButton.Text, korean ? "설정을 변경하지 않고 가이드를 닫습니다." : "Closes the guide without changing settings.");
			portalButton.Click += delegate { OpenDiscordDeveloperPortal(); };
			startButton.Click += delegate { DialogResult = DialogResult.OK; Close(); };
			laterButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			buttons.Controls.AddRange(new Control[] { portalButton, startButton, laterButton });
			root.Controls.Add(buttons, 0, 4);
			AcceptButton = startButton;
			CancelButton = laterButton;

			ApplySimpleDialogTheme(this);
			ThemePalette palette = ThemePalette.Create(launcherForm != null && launcherForm.UsesDarkTheme);
			for (int index = 0; index < stepNumbers.Count; index++) stepNumbers[index].ForeColor = palette.Accent;
			ApplyCommonButtonToolTips(this);
			EnsureButtonContentFits(portalButton);
			EnsureButtonContentFits(startButton);
			EnsureButtonContentFits(laterButton);
			stepsViewport.ClientSizeChanged += delegate { UpdateDiscordGuideTextLayout(); };
			securityPanel.ClientSizeChanged += delegate { UpdateDiscordGuideTextLayout(); };
			Load += delegate
			{
				UpdateDiscordGuideTextLayout();
				FitDiscordGuideToContent(root);
				UpdateDiscordGuideTextLayout();
			};
			UpdateDiscordGuideTextLayout();
		}

		private RoundedPanel CreateDiscordGuideStep(int number, string title, string description)
		{
			RoundedPanel card = new RoundedPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				CornerRadius = 12,
				Tag = "surface",
				Padding = new Padding(14, 8, 14, 8),
				Margin = new Padding(0, 4, 0, 4),
				AccessibleName = number.ToString() + ". " + title,
				AccessibleDescription = description
			};
			TableLayoutPanel content = new TableLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty
			};
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			Label numberLabel = new Label
			{
				Dock = DockStyle.Fill,
				Text = number.ToString("00"),
				TextAlign = ContentAlignment.MiddleCenter,
				Font = new Font(ThemeFonts.Display, 13F, FontStyle.Bold),
				AccessibleName = number.ToString()
			};
			stepNumbers.Add(numberLabel);
			TableLayoutPanel copy = new TableLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				ColumnCount = 1,
				RowCount = 2,
				Margin = Padding.Empty
			};
			copy.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			copy.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			copy.Controls.Add(new Label { AutoSize = true, Text = title, Font = new Font(ThemeFonts.Body, 10.5F, FontStyle.Bold), Margin = new Padding(0, 1, 0, 2) }, 0, 0);
			Label descriptionLabel = new Label
			{
				AutoSize = true,
				Text = description,
				Tag = "muted",
				Margin = Padding.Empty
			};
			stepDescriptions.Add(descriptionLabel);
			copy.Controls.Add(descriptionLabel, 0, 1);
			content.Controls.Add(numberLabel, 0, 0);
			content.Controls.Add(copy, 1, 0);
			card.Controls.Add(content);
			return card;
		}

		private void UpdateDiscordGuideTextLayout()
		{
			if (stepsViewport == null || stepsPanel == null || securityPanel == null || securityLabel == null) return;
			int viewportWidth = stepsViewport.ClientSize.Width;
			if (viewportWidth > 0)
			{
				// 세로 스크롤이 생겨도 가로 스크롤이 따라오지 않도록 항상 스크롤 막대 폭을 비워 둡니다.
				int panelWidth = Math.Max(320, viewportWidth - SystemInformation.VerticalScrollBarWidth);
				if (stepsPanel.Width != panelWidth) stepsPanel.Width = panelWidth;
				int descriptionWidth = Math.Max(180, panelWidth - 52 - 28);
				for (int index = 0; index < stepDescriptions.Count; index++)
					stepDescriptions[index].MaximumSize = new Size(descriptionWidth, 0);
			}
			int securityWidth = securityPanel.ClientSize.Width - securityPanel.Padding.Horizontal;
			if (securityWidth > 0) securityLabel.MaximumSize = new Size(Math.Max(220, securityWidth), 0);
			stepsPanel.PerformLayout();
			securityPanel.PerformLayout();
		}

		private void FitDiscordGuideToContent(TableLayoutPanel root)
		{
			if (root == null) return;
			root.PerformLayout();
			int nonClientHeight = Height - ClientSize.Height;
			int contentWidth = Math.Max(320, root.ClientSize.Width - root.Padding.Horizontal);
			int preferredClientHeight = root.Padding.Vertical;
			for (int row = 0; row < root.RowCount; row++)
			{
				Control rowControl = row == 2 ? (Control)stepsPanel : root.GetControlFromPosition(0, row);
				if (rowControl == null) continue;
				preferredClientHeight += rowControl.GetPreferredSize(new Size(contentWidth, 0)).Height;
				preferredClientHeight += (row == 2 ? stepsViewport.Margin : rowControl.Margin).Vertical;
			}
			int preferredHeight = preferredClientHeight + nonClientHeight;
			int maximumHeight = Math.Max(MinimumSize.Height, Screen.FromControl(this).WorkingArea.Height - 24);
			Height = Math.Max(MinimumSize.Height, Math.Min(maximumHeight, preferredHeight));
		}

		private void OpenDiscordDeveloperPortal()
		{
			bool korean = IsManagedKorean();
			try
			{
				Process.Start(new ProcessStartInfo { FileName = DiscordDeveloperPortalUrl, UseShellExecute = true });
			}
			catch
			{
				ShowMineHarborDialog(this,
					korean ? "브라우저에서 Discord Developer Portal을 열지 못했습니다." : "Could not open the Discord Developer Portal in your browser.",
					Text,
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}
	}

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
		private readonly Label validationLabel;
		private readonly Label statusLabel;
		private readonly Button guideButton;
		private readonly Button saveButton;
		private readonly string loadError;
		private readonly string existingProtectedToken;
		private bool refreshing;
		private bool syncingToggles;

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
				RowCount = 7
			};
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
				Text = korean ? "Discord 원격 제어 사용" : "Enable Discord remote control",
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
			TableLayoutPanel connection = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 10 };
			connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
			connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
			connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
			connection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
			connection.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
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
			allowedUsersBox.MinimumSize = new Size(0, 72);
			allowedRolesBox.MinimumSize = new Size(0, 72);

			removeTokenBox = new ModernCheckBox
			{
				AutoSize = true,
				Text = korean ? "저장된 토큰 제거" : "Remove saved token",
				Enabled = !string.IsNullOrEmpty(existingProtectedToken),
				Margin = new Padding(0, 3, 0, 7)
			};
			ConfigureAccessibleField(removeTokenBox, removeTokenBox.Text, korean
				? "저장할 때 암호화된 토큰을 삭제하며 원격 제어도 꺼야 합니다."
				: "Deletes the encrypted token on save; remote control must also be disabled.");

			AddDiscordField(connection, 0, korean ? "봇 토큰" : "Bot token", tokenBox, korean
				? "토큰은 현재 Windows 사용자 범위 DPAPI로 암호화해 저장합니다."
				: "The token is stored with current-user Windows DPAPI encryption.");
			connection.Controls.Add(removeTokenBox, 0, 2);
			AddDiscordField(connection, 3, korean ? "애플리케이션 ID" : "Application ID", applicationIdBox, korean ? "Discord Developer Portal의 Application ID입니다." : "The Application ID from the Discord Developer Portal.");
			AddDiscordField(connection, 5, korean ? "서버 ID" : "Server ID", guildIdBox, korean ? "명령을 등록할 Discord 서버입니다." : "The Discord server where the command is registered.");
			AddDiscordField(connection, 7, korean ? "채널 ID" : "Channel ID", channelIdBox, korean ? "이 채널에서만 명령을 허용합니다." : "Commands are accepted only in this channel.");

			ModernGroupBox authorizationGroup = new ModernGroupBox
			{
				Text = korean ? "허용 서버와 보안 경계" : "Approved servers and security boundary",
				Dock = DockStyle.Fill,
				Padding = new Padding(16),
				Margin = new Padding(10, 0, 0, 0)
			};
			TableLayoutPanel authorization = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorization.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			authorization.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			authorization.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
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

			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				Font = new Font(ThemeFonts.BodySemibold, 10F, FontStyle.Bold),
				Text = korean ? "접근 허용 · 사용자 또는 역할 중 1개 이상 필수" : "Access allowlist · at least one user or role is required",
				Margin = new Padding(0, 0, 0, 6)
			}, 0, 2);

			TableLayoutPanel allowlists = new TableLayoutPanel
			{
				Name = "discordAllowlists",
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = new Padding(0, 0, 0, 7)
			};
			allowlists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			allowlists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			allowlists.Controls.Add(CreateDiscordAllowListColumn(
				korean ? "허용 사용자" : "Allowed users",
				allowedUsersBox,
				korean ? "사용자 ID를 쉼표 또는 줄바꿈으로 구분합니다." : "Separate user IDs with commas or new lines.",
				new Padding(0, 0, 5, 0)), 0, 0);
			allowlists.Controls.Add(CreateDiscordAllowListColumn(
				korean ? "허용 역할 (선택)" : "Allowed roles (optional)",
				allowedRolesBox,
				korean ? "역할 ID를 쉼표 또는 줄바꿈으로 구분합니다." : "Separate role IDs with commas or new lines.",
				new Padding(5, 0, 0, 0)), 1, 0);
			authorization.Controls.Add(allowlists, 0, 3);

			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(350, 0),
				Text = korean
					? "내 ID 복사: Discord 사용자 설정 → 고급 → 개발자 모드 → 내 프로필 우클릭 → 사용자 ID 복사"
					: "Copy your ID: Discord User Settings → Advanced → Developer Mode → right-click your profile → Copy User ID",
				Margin = new Padding(0, 1, 0, 7),
				Tag = "muted"
			}, 0, 4);
			authorization.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(350, 0),
				Text = korean
					? "선택한 프로필과 허용 목록만 /mineharbor를 사용할 수 있습니다. 임의 콘솔·셸·파일 작업은 차단됩니다."
					: "Only approved profiles and allowlisted members can use /mineharbor. Arbitrary console, shell, and file operations remain blocked.",
				Margin = new Padding(0)
			}, 0, 5);

			validationLabel = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(860, 0),
				Margin = new Padding(0, 10, 0, 0),
				Tag = "warning"
			};
			root.Controls.Add(validationLabel, 0, 4);

			statusLabel = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(860, 0),
				Text = !string.IsNullOrEmpty(loadError)
					? (korean ? "설정을 검증하지 못해 원본을 보존했습니다. 파일을 확인한 뒤 다시 열어 주세요." : "The settings file could not be verified and was preserved. Review it, then reopen this window.")
					: (korean ? "연결 상태를 확인하는 중…" : "Checking connection status…"),
				Margin = new Padding(0, 4, 0, 8)
			};
			statusLabel.AccessibleName = statusLabel.Text;
			root.Controls.Add(statusLabel, 0, 5);

			FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			guideButton = MultiServerDashboardForm.NewManagedButton(korean ? "설정 가이드" : "Setup guide", 124, "secondary");
			Button refresh = MultiServerDashboardForm.NewManagedButton(korean ? "상태 새로고침" : "Refresh status", 136, "secondary");
			saveButton = MultiServerDashboardForm.NewManagedButton(korean ? "저장 및 연결" : "Save and connect", 136, "primary");
			Button cancel = MultiServerDashboardForm.NewManagedButton(korean ? "취소" : "Cancel", 100, "secondary");
			guideButton.Click += delegate { ShowRegistrationGuide(); };
			refresh.Click += delegate { RefreshStatusAsync(); };
			saveButton.Click += delegate { SaveSettings(); };
			cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			refresh.Enabled = string.IsNullOrEmpty(loadError);
			saveButton.Enabled = string.IsNullOrEmpty(loadError);
			ConfigureAccessibleField(guideButton, guideButton.Text, korean ? "Discord 앱 등록 순서를 다시 표시합니다." : "Shows the Discord app registration steps again.");
			ConfigureAccessibleField(saveButton, saveButton.Text, korean ? "입력한 설정을 검증한 뒤 백그라운드 에이전트에 연결합니다." : "Validates the configuration and connects it to the background agent.");
			buttons.Controls.AddRange(new Control[] { guideButton, refresh, saveButton, cancel });
			root.Controls.Add(buttons, 0, 6);

			// 두 항목은 동시에 켤 수 없으므로 사용자가 마지막에 누른 쪽을 남기고 반대쪽을 끕니다.
			// 예전처럼 방금 켠 항목이 말없이 되돌아가지 않도록 합니다.
			enabledBox.CheckedChanged += delegate
			{
				if (!syncingToggles && enabledBox.Checked && removeTokenBox.Checked)
				{
					syncingToggles = true;
					try { removeTokenBox.Checked = false; }
					finally { syncingToggles = false; }
				}
				UpdateEnabledState();
			};
			removeTokenBox.CheckedChanged += delegate
			{
				if (!syncingToggles && removeTokenBox.Checked && enabledBox.Checked)
				{
					syncingToggles = true;
					try { enabledBox.Checked = false; }
					finally { syncingToggles = false; }
				}
				UpdateEnabledState();
			};
			tokenBox.TextChanged += delegate { UpdateValidationState(); };
			applicationIdBox.TextChanged += delegate { UpdateValidationState(); };
			guildIdBox.TextChanged += delegate { UpdateValidationState(); };
			channelIdBox.TextChanged += delegate { UpdateValidationState(); };
			allowedUsersBox.TextChanged += delegate { UpdateValidationState(); };
			allowedRolesBox.TextChanged += delegate { UpdateValidationState(); };
			profilesBox.ItemCheck += delegate
			{
				if (!IsDisposed && IsHandleCreated)
					BeginInvoke((MethodInvoker)delegate
					{
						if (!IsDisposed && !Disposing) UpdateValidationState();
					});
			};
			Shown += delegate { RefreshStatusAsync(); };
			UpdateEnabledState();
			ApplySimpleDialogTheme(this);
			UpdateValidationState();
			ApplyCommonButtonToolTips(this);
		}

		protected override void OnFormClosed(FormClosedEventArgs eventArgs)
		{
			// 저장이 끝나거나 취소한 뒤 평문 봇 토큰이 입력 컨트롤에 남지 않도록 지웁니다.
			if (tokenBox != null && !tokenBox.IsDisposed) tokenBox.Clear();
			base.OnFormClosed(eventArgs);
		}

		private void ShowRegistrationGuide()
		{
			using (DiscordRemoteRegistrationGuideForm guide = new DiscordRemoteRegistrationGuideForm())
			{
				if (guide.ShowDialog(this) != DialogResult.OK) return;
			}
			enabledBox.Checked = true;
			if (string.IsNullOrEmpty(existingProtectedToken) && string.IsNullOrWhiteSpace(tokenBox.Text)) tokenBox.Focus();
			else if (string.IsNullOrWhiteSpace(applicationIdBox.Text)) applicationIdBox.Focus();
			else guildIdBox.Focus();
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

		private static TableLayoutPanel CreateDiscordAllowListColumn(string label, ModernTextBox box, string description, Padding margin)
		{
			TableLayoutPanel column = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				Margin = margin
			};
			column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			column.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			column.Controls.Add(new Label { AutoSize = true, Text = label, Margin = new Padding(0, 0, 0, 4) }, 0, 0);
			RoundedPanel surface = CreateModernTextBoxSurface(box, 9);
			surface.Dock = DockStyle.Fill;
			surface.Margin = Padding.Empty;
			column.Controls.Add(surface, 0, 1);
			ConfigureAccessibleField(box, label, description);
			return column;
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
			UpdateValidationState();
		}

		private bool TryValidateInput(out string message, out Control target)
		{
			bool korean = IsManagedKorean();
			message = string.Empty;
			target = null;
			if (!enabledBox.Checked) return true;

			if (removeTokenBox.Checked || (string.IsNullOrEmpty(existingProtectedToken) && string.IsNullOrWhiteSpace(tokenBox.Text)))
			{
				message = korean ? "봇 토큰을 입력해 주세요." : "Enter a bot token.";
				target = tokenBox;
				return false;
			}
			if (!string.IsNullOrWhiteSpace(tokenBox.Text))
			{
				try { ValidateDiscordBotToken(tokenBox.Text.Trim()); }
				catch
				{
					message = korean ? "봇 토큰 형식을 확인해 주세요." : "Check the bot-token format.";
					target = tokenBox;
					return false;
				}
			}
			if (!IsDiscordSnowflake(applicationIdBox.Text.Trim()))
			{
				message = korean ? "애플리케이션 ID를 확인해 주세요." : "Check the application ID.";
				target = applicationIdBox;
				return false;
			}
			if (!IsDiscordSnowflake(guildIdBox.Text.Trim()))
			{
				message = korean ? "서버 ID를 확인해 주세요." : "Check the server ID.";
				target = guildIdBox;
				return false;
			}
			if (!IsDiscordSnowflake(channelIdBox.Text.Trim()))
			{
				message = korean ? "채널 ID를 확인해 주세요." : "Check the channel ID.";
				target = channelIdBox;
				return false;
			}

			List<string> users;
			List<string> roles;
			try { users = ParseDiscordIdText(allowedUsersBox.Text); }
			catch
			{
				message = korean ? "허용 사용자 ID는 숫자 ID만 입력해 주세요." : "Allowed user IDs must contain numeric Discord IDs only.";
				target = allowedUsersBox;
				return false;
			}
			try { roles = ParseDiscordIdText(allowedRolesBox.Text); }
			catch
			{
				message = korean ? "허용 역할 ID는 숫자 ID만 입력해 주세요." : "Allowed role IDs must contain numeric Discord IDs only.";
				target = allowedRolesBox;
				return false;
			}
			if (users.Count == 0 && roles.Count == 0)
			{
				message = korean
					? "허용 사용자 또는 역할 ID를 하나 이상 입력해 주세요."
					: "Enter at least one allowed user or role ID.";
				target = allowedUsersBox;
				return false;
			}
			if (profilesBox.CheckedItems.Count == 0)
			{
				message = korean ? "Discord에서 관리할 서버 프로필을 선택해 주세요." : "Select a server profile to manage from Discord.";
				target = profilesBox;
				return false;
			}
			return true;
		}

		private void SetValidationMessage(string message, string role)
		{
			validationLabel.Text = message ?? string.Empty;
			validationLabel.Tag = role;
			validationLabel.AccessibleName = validationLabel.Text;
			ThemePalette palette = ThemePalette.Create(launcherForm != null && launcherForm.UsesDarkTheme);
			if (string.Equals(role, "success", StringComparison.Ordinal)) validationLabel.ForeColor = palette.Success;
			else if (string.Equals(role, "danger-text", StringComparison.Ordinal)) validationLabel.ForeColor = palette.Danger;
			else if (string.Equals(role, "warning", StringComparison.Ordinal)) validationLabel.ForeColor = palette.Warning;
			else validationLabel.ForeColor = palette.Muted;
		}

		private void UpdateValidationState()
		{
			if (validationLabel == null || saveButton == null) return;
			bool korean = IsManagedKorean();
			if (!string.IsNullOrEmpty(loadError))
			{
				SetValidationMessage(
					korean ? "설정 파일을 먼저 확인해야 합니다." : "Review the settings file before continuing.",
					"danger-text");
				saveButton.Enabled = false;
				return;
			}
			if (removeTokenBox.Checked)
			{
				SetValidationMessage(
					korean
						? "저장하면 암호화된 토큰을 지우고 원격 제어를 끕니다. 다시 사용하려면 새 봇 토큰을 입력해 주세요."
						: "Saving deletes the encrypted token and turns remote control off. Enter a new bot token to use it again.",
					"warning");
				saveButton.Enabled = true;
				return;
			}
			if (!enabledBox.Checked)
			{
				SetValidationMessage(
					korean ? "원격 제어가 꺼져 있습니다. 켜면 필요한 항목을 여기서 바로 확인할 수 있습니다." : "Remote control is off. Turn it on to review the required fields here.",
					"muted");
				saveButton.Enabled = true;
				return;
			}

			string message;
			Control target;
			if (TryValidateInput(out message, out target))
			{
				SetValidationMessage(
					korean ? "연결 준비가 완료되었습니다. 저장 및 연결을 눌러 주세요." : "Ready to connect. Select Save and connect.",
					"success");
			}
			else
			{
				SetValidationMessage(
					(korean ? "연결 전 확인 · " : "Before connecting · ") + message,
					"warning");
			}
			saveButton.Enabled = true;
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
				string validationMessage;
				Control validationTarget;
				if (!TryValidateInput(out validationMessage, out validationTarget))
				{
					SetValidationMessage(validationMessage, "danger-text");
					if (validationTarget != null && validationTarget.CanFocus) validationTarget.Focus();
					return;
				}
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
