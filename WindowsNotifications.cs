using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static partial class Launcher
{
	private const int WindowsNotificationSettingsSchemaVersion = 1;
	private const int WindowsNotificationSettingsMaximumBytes = 65536;
	private const int WindowsNotificationMaximumPending = 50;
	private static readonly object WindowsNotificationSettingsProcessLock = new object();
	private static string WindowsNotificationSettingsPathOverride = null;
	private static Action<string, string, ToolTipIcon> WindowsNotificationDisplayOverride = null;

	private sealed class WindowsNotificationSettings
	{
		public int SchemaVersion = WindowsNotificationSettingsSchemaVersion;
		public bool Enabled;
		public string MinimumSeverity = "warning";
		public bool ServerEvents = true;
		public bool AutomationEvents = true;
		public bool BackupEvents = true;
		public bool ContentEvents = true;
		public bool NetworkEvents = true;
		public bool UpdateAndSecurityEvents = true;
		public bool QuietHoursEnabled;
		public int QuietStartMinutes = 22 * 60;
		public int QuietEndMinutes = 7 * 60;
	}

	private sealed class WindowsNotificationEnvelope
	{
		public string ServerDirectory;
		public string ProfileName;
		public string EntryId;
		public string Category;
		public string Severity;
		public string MessageKo;
		public string MessageEn;
		public DateTime CreatedUtc;
	}

	private static string GetWindowsNotificationSettingsPath()
	{
		if (!string.IsNullOrWhiteSpace(WindowsNotificationSettingsPathOverride))
			return Path.GetFullPath(WindowsNotificationSettingsPathOverride);
		return Path.Combine(GetLauncherUserDataDirectory(), "windows-notifications.json");
	}

	private static WindowsNotificationSettings ReadWindowsNotificationSettings()
	{
		lock (WindowsNotificationSettingsProcessLock)
		{
			return WithWindowsNotificationSettingsLock(delegate
			{
				string path = GetWindowsNotificationSettingsPath();
				if (!File.Exists(path)) return new WindowsNotificationSettings();
				FileInfo info = new FileInfo(path);
				if (info.Length <= 0 || info.Length > WindowsNotificationSettingsMaximumBytes)
					throw Localized(new InvalidDataException("Windows 알림 설정 파일 크기가 올바르지 않습니다."), "The Windows notification settings file size is invalid.");
				WindowsNotificationSettings settings;
				try
				{
					settings = new JavaScriptSerializer().Deserialize<WindowsNotificationSettings>(File.ReadAllText(path, Encoding.UTF8));
				}
				catch (Exception exception)
				{
					throw Localized(new InvalidDataException("Windows 알림 설정 파일이 손상되었습니다. 원본 파일은 변경하지 않았습니다.", exception), "The Windows notification settings file is damaged. The original file was left unchanged.");
				}
				ValidateWindowsNotificationSettings(settings);
				return settings;
			});
		}
	}

	private static void WriteWindowsNotificationSettings(WindowsNotificationSettings settings)
	{
		ValidateWindowsNotificationSettings(settings);
		lock (WindowsNotificationSettingsProcessLock)
		{
			WithWindowsNotificationSettingsLock(delegate
			{
				string path = GetWindowsNotificationSettingsPath();
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				WriteJsonAtomic(path, settings);
				return 0;
			});
		}
	}

	private static void ValidateWindowsNotificationSettings(WindowsNotificationSettings settings)
	{
		if (settings == null || settings.SchemaVersion != WindowsNotificationSettingsSchemaVersion)
			throw Localized(new InvalidDataException("지원하지 않는 Windows 알림 설정 버전입니다."), "Unsupported Windows notification settings version.");
		if (!string.Equals(settings.MinimumSeverity, "info", StringComparison.Ordinal)
			&& !string.Equals(settings.MinimumSeverity, "warning", StringComparison.Ordinal)
			&& !string.Equals(settings.MinimumSeverity, "error", StringComparison.Ordinal))
			throw Localized(new InvalidDataException("Windows 알림 최소 중요도가 올바르지 않습니다."), "The minimum Windows notification severity is invalid.");
		if (settings.QuietStartMinutes < 0 || settings.QuietStartMinutes >= 24 * 60
			|| settings.QuietEndMinutes < 0 || settings.QuietEndMinutes >= 24 * 60)
			throw Localized(new InvalidDataException("Windows 알림 조용한 시간 범위가 올바르지 않습니다."), "The Windows notification quiet-hours range is invalid.");
	}

	private static T WithWindowsNotificationSettingsLock<T>(Func<T> action)
	{
		string path = GetWindowsNotificationSettingsPath();
		using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()))).Replace("-", string.Empty).Substring(0, 24);
			using (Mutex mutex = new Mutex(false, "Local\\MineHarbor.WindowsNotifications." + suffix))
			{
				bool entered = false;
				try
				{
					try { entered = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
					catch (AbandonedMutexException) { entered = true; }
					if (!entered) throw Localized(new IOException("다른 MineHarbor 프로세스가 Windows 알림 설정을 갱신하고 있습니다."), "Another MineHarbor process is updating the Windows notification settings.");
					return action();
				}
				finally
				{
					if (entered) mutex.ReleaseMutex();
				}
			}
		}
	}

	private static bool IsWithinWindowsNotificationQuietHours(WindowsNotificationSettings settings, DateTime localTime)
	{
		if (settings == null || !settings.QuietHoursEnabled) return false;
		int current = localTime.Hour * 60 + localTime.Minute;
		if (settings.QuietStartMinutes == settings.QuietEndMinutes) return true;
		if (settings.QuietStartMinutes < settings.QuietEndMinutes)
			return current >= settings.QuietStartMinutes && current < settings.QuietEndMinutes;
		return current >= settings.QuietStartMinutes || current < settings.QuietEndMinutes;
	}

	private static bool ShouldDeliverWindowsNotification(WindowsNotificationSettings settings, string category, string severity, DateTime localTime)
	{
		if (settings == null || !settings.Enabled || IsWithinWindowsNotificationQuietHours(settings, localTime)) return false;
		if (WindowsNotificationSeverityRank(severity) < WindowsNotificationSeverityRank(settings.MinimumSeverity)) return false;
		string normalizedCategory = NormalizeOperationCategory(category);
		if (normalizedCategory == "server") return settings.ServerEvents;
		if (normalizedCategory == "automation") return settings.AutomationEvents;
		if (normalizedCategory == "backup") return settings.BackupEvents;
		if (normalizedCategory == "content") return settings.ContentEvents;
		if (normalizedCategory == "network") return settings.NetworkEvents;
		if (normalizedCategory == "update" || normalizedCategory == "security" || normalizedCategory == "system")
			return settings.UpdateAndSecurityEvents;
		return false;
	}

	private static int WindowsNotificationSeverityRank(string severity)
	{
		if (string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase)) return 2;
		if (string.Equals(severity, "warning", StringComparison.OrdinalIgnoreCase)) return 1;
		return 0;
	}

	private static ToolTipIcon WindowsNotificationIcon(string severity)
	{
		if (string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase)) return ToolTipIcon.Error;
		if (string.Equals(severity, "warning", StringComparison.OrdinalIgnoreCase)) return ToolTipIcon.Warning;
		return ToolTipIcon.Info;
	}

	private static string TrimWindowsNotificationText(string value, int maximumLength)
	{
		string text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('\0', ' ').Trim();
		if (text.Length <= maximumLength) return text;
		// 알림 문구도 서로게이트 쌍(이모지 등)이 반으로 갈라지지 않게 자릅니다.
		int keep = Math.Max(1, maximumLength - 1);
		if (char.IsHighSurrogate(text[keep - 1]) && char.IsLowSurrogate(text[keep])) keep--;
		return text.Substring(0, keep).TrimEnd() + "…";
	}

	private static void DisplayWindowsNotification(NotifyIcon trayIcon, string title, string message, ToolTipIcon icon)
	{
		title = TrimWindowsNotificationText(title, 63);
		message = TrimWindowsNotificationText(message, 240);
		if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message)) return;
		if (WindowsNotificationDisplayOverride != null)
		{
			WindowsNotificationDisplayOverride(title, message, icon);
			return;
		}
		if (trayIcon == null || !trayIcon.Visible) return;
		trayIcon.ShowBalloonTip(10000, title, message, icon);
	}

	private sealed class WindowsNotificationMonitor
	{
		private readonly NotifyIcon trayIcon;
		private readonly Dictionary<string, string> lastEntryByServer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly List<WindowsNotificationEnvelope> pending = new List<WindowsNotificationEnvelope>();
		private readonly HashSet<string> reportedHistoryFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private DateTime nextDisplayUtc = DateTime.MinValue;
		private bool settingsFailureReported;

		public WindowsNotificationMonitor(NotifyIcon notificationTrayIcon)
		{
			trayIcon = notificationTrayIcon;
		}

		public void Poll()
		{
			PollAt(DateTime.UtcNow, DateTime.Now);
		}

		private void PollAt(DateTime nowUtc, DateTime localTime)
		{
			WindowsNotificationSettings settings;
			List<ManagedProfileRecord> profiles;
			try
			{
				settings = ReadWindowsNotificationSettings();
				profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory));
			}
			catch (Exception exception)
			{
				if (!settingsFailureReported)
					Console.Error.WriteLine("[Notifications] 알림 검사 생략 (" + exception.GetType().Name + ")");
				settingsFailureReported = true;
				return;
			}
			settingsFailureReported = false;

			HashSet<string> currentDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < profiles.Count; i++)
			{
				ManagedProfileRecord profile = profiles[i];
				string directory = Path.GetFullPath(profile.Directory);
				currentDirectories.Add(directory);
				CollectNewEntries(profile, settings, localTime);
			}
			List<string> removed = new List<string>();
			foreach (string directory in lastEntryByServer.Keys)
				if (!currentDirectories.Contains(directory)) removed.Add(directory);
			for (int i = 0; i < removed.Count; i++)
			{
				lastEntryByServer.Remove(removed[i]);
				reportedHistoryFailures.Remove(removed[i]);
			}
			removed.Clear();
			foreach (string directory in reportedHistoryFailures)
				if (!currentDirectories.Contains(directory)) removed.Add(directory);
			for (int i = 0; i < removed.Count; i++) reportedHistoryFailures.Remove(removed[i]);

			if (!settings.Enabled)
			{
				pending.Clear();
				return;
			}
			if (pending.Count == 0 || nowUtc < nextDisplayUtc) return;
			ShowPendingSummary(nowUtc);
		}

		private void CollectNewEntries(ManagedProfileRecord profile, WindowsNotificationSettings settings, DateTime localTime)
		{
			OperationsHistoryDocument document;
			string directory = Path.GetFullPath(profile.Directory);
			try { document = ReadOperationsHistory(profile.Directory); }
			catch (Exception exception)
			{
				if (reportedHistoryFailures.Add(directory))
					Console.Error.WriteLine("[Notifications] 운영 기록 감시 생략 (" + exception.GetType().Name + ")");
				return;
			}
			reportedHistoryFailures.Remove(directory);
			string latestId = document.Entries.Count == 0 ? string.Empty : document.Entries[document.Entries.Count - 1].Id;
			string previousId;
			if (!lastEntryByServer.TryGetValue(directory, out previousId))
			{
				// 에이전트 시작 전의 오래된 기록을 갑자기 다시 알리지 않고 현재 위치만 기준점으로 잡습니다.
				lastEntryByServer[directory] = latestId;
				return;
			}

			int startIndex = 0;
			if (!string.IsNullOrEmpty(previousId))
			{
				startIndex = -1;
				for (int i = document.Entries.Count - 1; i >= 0; i--)
				{
					if (!string.Equals(document.Entries[i].Id, previousId, StringComparison.OrdinalIgnoreCase)) continue;
					startIndex = i + 1;
					break;
				}
				// 보존 한도로 기준 항목이 사라졌다면 오래된 기록을 재생하지 않습니다.
				if (startIndex < 0)
				{
					lastEntryByServer[directory] = latestId;
					return;
				}
			}

			for (int i = startIndex; i < document.Entries.Count; i++)
			{
				OperationsHistoryEntry entry = document.Entries[i];
				if (!ShouldDeliverWindowsNotification(settings, entry.Category, entry.Severity, localTime)) continue;
				DateTime createdUtc;
				if (!DateTime.TryParse(entry.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out createdUtc)) continue;
				pending.Add(new WindowsNotificationEnvelope
				{
					ServerDirectory = directory,
					ProfileName = profile.Name,
					EntryId = entry.Id,
					Category = entry.Category,
					Severity = entry.Severity,
					MessageKo = SanitizeOperationMessage(entry.MessageKo, directory),
					MessageEn = SanitizeOperationMessage(entry.MessageEn, directory),
					CreatedUtc = createdUtc.ToUniversalTime()
				});
				while (pending.Count > WindowsNotificationMaximumPending) pending.RemoveAt(0);
			}
			lastEntryByServer[directory] = latestId;
		}

		private void ShowPendingSummary(DateTime nowUtc)
		{
			int bestIndex = 0;
			for (int i = 1; i < pending.Count; i++)
			{
				int left = WindowsNotificationSeverityRank(pending[i].Severity);
				int right = WindowsNotificationSeverityRank(pending[bestIndex].Severity);
				if (left > right || (left == right && pending[i].CreatedUtc > pending[bestIndex].CreatedUtc)) bestIndex = i;
			}
			WindowsNotificationEnvelope selected = pending[bestIndex];
			int additional = pending.Count - 1;
			bool korean = IsManagedKorean();
			string title = "MineHarbor · " + TrimWindowsNotificationText(selected.ProfileName, 42);
			string message = korean ? selected.MessageKo : selected.MessageEn;
			if (additional > 0)
				message += korean ? " · 그 외 새 알림 " + additional.ToString(CultureInfo.CurrentCulture) + "개" : " · " + additional.ToString(CultureInfo.InvariantCulture) + " more new notification" + (additional == 1 ? string.Empty : "s");
			DisplayWindowsNotification(trayIcon, title, message, WindowsNotificationIcon(selected.Severity));
			pending.Clear();
			nextDisplayUtc = nowUtc.AddSeconds(8);
		}
	}

	private sealed class WindowsNotificationSettingsForm : Form
	{
		private readonly ModernCheckBox enabledBox;
		private readonly ModernComboBox severityBox;
		private readonly ModernCheckBox serverBox;
		private readonly ModernCheckBox automationBox;
		private readonly ModernCheckBox backupBox;
		private readonly ModernCheckBox contentBox;
		private readonly ModernCheckBox networkBox;
		private readonly ModernCheckBox updateSecurityBox;
		private readonly ModernCheckBox quietBox;
		private readonly ModernComboBox quietStartBox;
		private readonly ModernComboBox quietEndBox;
		private readonly string loadError;

		public WindowsNotificationSettingsForm()
		{
			bool korean = IsManagedKorean();
			Text = korean ? "Windows 알림 설정" : "Windows notification settings";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(700, 620);
			Size = new Size(760, 680);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			ApplyLauncherWindowIcon(this);

			WindowsNotificationSettings settings;
			string settingsLoadError = null;
			try { settings = ReadWindowsNotificationSettings(); }
			catch (Exception exception)
			{
				settings = new WindowsNotificationSettings();
				settingsLoadError = exception.Message;
			}
			loadError = settingsLoadError;

			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);

			root.Controls.Add(new Label
			{
				AutoSize = true,
				Font = new Font(ThemeFonts.Display, 18F, FontStyle.Bold),
				Text = korean ? "중요한 서버 상태를 놓치지 마세요" : "Stay informed about important server events",
				Margin = new Padding(0, 0, 0, 8)
			}, 0, 0);
			root.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(680, 0),
				Text = korean
					? "백그라운드 에이전트가 실행 중일 때 새 운영 기록을 Windows 작업 표시줄 알림으로 보여 줍니다. 명령, IP 주소, 사용자 경로와 비밀값은 표시하지 않습니다."
					: "While the background agent is running, new operations can appear as Windows taskbar notifications. Commands, IP addresses, user paths, and secret values are not shown.",
				Margin = new Padding(0, 0, 0, 14)
			}, 0, 1);

			enabledBox = new ModernCheckBox { AutoSize = true, Text = korean ? "Windows 알림 사용" : "Enable Windows notifications", Checked = settings.Enabled, Margin = new Padding(0, 4, 0, 10) };
			ConfigureAccessibleField(enabledBox, enabledBox.Text, korean ? "기본값은 꺼짐이며 사용자가 켠 경우에만 알림을 표시합니다." : "Disabled by default and shown only after explicit opt-in.");
			root.Controls.Add(enabledBox, 0, 2);

			TableLayoutPanel severityRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Margin = new Padding(0, 0, 0, 12) };
			severityRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			severityRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
			severityRow.Controls.Add(new Label { AutoSize = true, Text = korean ? "최소 중요도" : "Minimum severity", Margin = new Padding(0, 9, 16, 0) }, 0, 0);
			severityBox = new ModernComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
			severityBox.Items.AddRange(korean
				? new object[] { "정보·성공 포함", "경고 이상", "오류만" }
				: new object[] { "Include info and success", "Warnings and errors", "Errors only" });
			severityBox.SelectedIndex = string.Equals(settings.MinimumSeverity, "info", StringComparison.Ordinal) ? 0 : string.Equals(settings.MinimumSeverity, "error", StringComparison.Ordinal) ? 2 : 1;
			ConfigureAccessibleField(severityBox, korean ? "알림 최소 중요도" : "Notification minimum severity", korean ? "표시할 알림의 최소 중요도를 선택합니다." : "Choose the minimum severity to display.");
			severityRow.Controls.Add(severityBox, 1, 0);
			root.Controls.Add(severityRow, 0, 3);

			ModernGroupBox categories = new ModernGroupBox { Text = korean ? "알림 종류" : "Notification categories", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12) };
			FlowLayoutPanel categoryFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
			serverBox = NewNotificationCheckBox(korean ? "서버 시작·종료·충돌" : "Server lifecycle and crashes", settings.ServerEvents);
			automationBox = NewNotificationCheckBox(korean ? "예약 작업" : "Scheduled jobs", settings.AutomationEvents);
			backupBox = NewNotificationCheckBox(korean ? "백업" : "Backups", settings.BackupEvents);
			contentBox = NewNotificationCheckBox(korean ? "콘텐츠" : "Content", settings.ContentEvents);
			networkBox = NewNotificationCheckBox(korean ? "네트워크" : "Network", settings.NetworkEvents);
			updateSecurityBox = NewNotificationCheckBox(korean ? "업데이트·보안·시스템" : "Updates, security, and system", settings.UpdateAndSecurityEvents);
			categoryFlow.Controls.AddRange(new Control[] { serverBox, automationBox, backupBox, contentBox, networkBox, updateSecurityBox });
			categories.Controls.Add(categoryFlow);
			root.Controls.Add(categories, 0, 4);

			ModernGroupBox quietGroup = new ModernGroupBox { Text = korean ? "조용한 시간" : "Quiet hours", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12) };
			TableLayoutPanel quietLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, RowCount = 2 };
			quietLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			quietLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
			quietLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			quietLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
			quietBox = new ModernCheckBox { AutoSize = true, Text = korean ? "이 시간에는 표시하지 않음" : "Do not show during these hours", Checked = settings.QuietHoursEnabled, Margin = new Padding(0, 5, 18, 6) };
			quietLayout.Controls.Add(quietBox, 0, 0);
			quietLayout.SetColumnSpan(quietBox, 4);
			quietLayout.Controls.Add(new Label { AutoSize = true, Text = korean ? "시작" : "From", Margin = new Padding(0, 9, 8, 0) }, 0, 1);
			quietStartBox = NewNotificationTimeBox(settings.QuietStartMinutes);
			quietLayout.Controls.Add(quietStartBox, 1, 1);
			quietLayout.Controls.Add(new Label { AutoSize = true, Text = korean ? "종료" : "Until", Margin = new Padding(18, 9, 8, 0) }, 2, 1);
			quietEndBox = NewNotificationTimeBox(settings.QuietEndMinutes);
			quietLayout.Controls.Add(quietEndBox, 3, 1);
			quietGroup.Controls.Add(quietLayout);
			root.Controls.Add(quietGroup, 0, 5);

			Label status = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(680, 0),
				Text = !string.IsNullOrEmpty(loadError)
					? (korean ? "설정 파일을 검증하지 못해 원본을 보존했습니다. 파일을 확인한 뒤 다시 열어 주세요." : "The settings file could not be verified and was preserved. Review it, then reopen this window.")
					: IsBackgroundAgentRunning()
						? (korean ? "백그라운드 에이전트가 연결되어 있습니다." : "The background agent is connected.")
						: (korean ? "알림을 표시하려면 백그라운드 운영도 켜야 합니다." : "Background operations must also be enabled to show notifications."),
				Margin = new Padding(0, 0, 0, 10)
			};
			status.AccessibleName = status.Text;
			root.Controls.Add(status, 0, 6);

			FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			Button test = MultiServerDashboardForm.NewManagedButton(korean ? "저장 후 테스트" : "Save and test", 142, "secondary");
			Button save = MultiServerDashboardForm.NewManagedButton(korean ? "저장" : "Save", 100, "primary");
			Button cancel = MultiServerDashboardForm.NewManagedButton(korean ? "취소" : "Cancel", 100, "secondary");
			test.Enabled = string.IsNullOrEmpty(loadError);
			save.Enabled = string.IsNullOrEmpty(loadError);
			test.Click += delegate { if (SaveSettings(false)) SendTestNotification(); };
			save.Click += delegate { SaveSettings(true); };
			cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			buttons.Controls.AddRange(new Control[] { test, save, cancel });
			root.Controls.Add(buttons, 0, 7);

			quietBox.CheckedChanged += delegate { UpdateEnabledState(); };
			enabledBox.CheckedChanged += delegate { UpdateEnabledState(); };
			UpdateEnabledState();
			ApplySimpleDialogTheme(this);
			ApplyCommonButtonToolTips(this);
		}

		private static ModernCheckBox NewNotificationCheckBox(string text, bool value)
		{
			ModernCheckBox box = new ModernCheckBox { AutoSize = true, Text = text, Checked = value, Margin = new Padding(0, 5, 22, 5) };
			box.AccessibleName = text;
			return box;
		}

		private static ModernComboBox NewNotificationTimeBox(int selectedMinutes)
		{
			ModernComboBox box = new ModernComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
			for (int minutes = 0; minutes < 24 * 60; minutes += 30)
				box.Items.Add((minutes / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutes % 60).ToString("00", CultureInfo.InvariantCulture));
			box.SelectedIndex = Math.Max(0, Math.Min(box.Items.Count - 1, selectedMinutes / 30));
			return box;
		}

		private void UpdateEnabledState()
		{
			bool enabled = enabledBox.Checked;
			severityBox.Enabled = enabled;
			serverBox.Enabled = enabled;
			automationBox.Enabled = enabled;
			backupBox.Enabled = enabled;
			contentBox.Enabled = enabled;
			networkBox.Enabled = enabled;
			updateSecurityBox.Enabled = enabled;
			quietBox.Enabled = enabled;
			quietStartBox.Enabled = enabled && quietBox.Checked;
			quietEndBox.Enabled = enabled && quietBox.Checked;
		}

		private WindowsNotificationSettings BuildSettings()
		{
			return new WindowsNotificationSettings
			{
				Enabled = enabledBox.Checked,
				MinimumSeverity = severityBox.SelectedIndex == 0 ? "info" : severityBox.SelectedIndex == 2 ? "error" : "warning",
				ServerEvents = serverBox.Checked,
				AutomationEvents = automationBox.Checked,
				BackupEvents = backupBox.Checked,
				ContentEvents = contentBox.Checked,
				NetworkEvents = networkBox.Checked,
				UpdateAndSecurityEvents = updateSecurityBox.Checked,
				QuietHoursEnabled = quietBox.Checked,
				QuietStartMinutes = Math.Max(0, quietStartBox.SelectedIndex) * 30,
				QuietEndMinutes = Math.Max(0, quietEndBox.SelectedIndex) * 30
			};
		}

		private bool SaveSettings(bool closeAfterSave)
		{
			if (!string.IsNullOrEmpty(loadError)) return false;
			try
			{
				WriteWindowsNotificationSettings(BuildSettings());
				if (closeAfterSave)
				{
					DialogResult = DialogResult.OK;
					Close();
				}
				return true;
			}
			catch (Exception exception)
			{
				ShowMineHarborDialog(this, (IsManagedKorean() ? "알림 설정을 저장하지 못했습니다: " : "Could not save notification settings: ") + DescribeException(exception), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private void SendTestNotification()
		{
			bool korean = IsManagedKorean();
			BackgroundAgentSettings background;
			try { background = ReadBackgroundAgentSettings(); }
			catch { background = new BackgroundAgentSettings(); }
			if (!background.Enabled || !EnsureBackgroundAgentRunning())
			{
				ShowMineHarborDialog(this, korean ? "테스트 알림을 표시하려면 백그라운드 운영을 먼저 켜 주세요." : "Enable background operations before showing a test notification.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			BackgroundAgentResponse response = SendBackgroundAgentRequest("test-notification", null, null, 1500);
			if (response == null || !response.Success)
				ShowMineHarborDialog(this, korean ? "테스트 알림을 표시하지 못했습니다." : "Could not show the test notification.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}
}
