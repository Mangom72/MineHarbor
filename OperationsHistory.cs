using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static partial class Launcher
{
	private const int OperationsHistorySchemaVersion = 1;
	private const int OperationsHistoryMaximumEntries = 500;
	private const int OperationsHistoryMaximumBytes = 4194304;
	private static readonly object OperationsHistoryProcessLock = new object();

	private sealed class OperationsHistoryDocument
	{
		public int SchemaVersion = OperationsHistorySchemaVersion;
		public string ChainAnchor = string.Empty;
		public List<OperationsHistoryEntry> Entries = new List<OperationsHistoryEntry>();
	}

	private sealed class OperationsHistoryEntry
	{
		public string Id;
		public string CreatedUtc;
		public string Category;
		public string Severity;
		public string Source;
		public string MessageKo;
		public string MessageEn;
		public bool IsRead;
		public string PreviousHash;
		public string Hash;
	}

	private sealed class OperationsHistoryListReference
	{
		public string ServerDirectory;
		public string EntryId;
	}

	private static string GetOperationsHistoryPath(string serverDirectory)
	{
		string root = Path.GetFullPath(serverDirectory);
		string metadata = Path.Combine(root, ".mineharbor");
		string path = Path.GetFullPath(Path.Combine(metadata, "operations-history.json"));
		if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			throw new InvalidDataException("운영 기록 경로가 서버 폴더를 벗어났습니다.");
		if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
			throw new InvalidDataException("연결 또는 재분석 지점 서버 폴더에는 운영 기록을 저장할 수 없습니다.");
		if (Directory.Exists(metadata) && (File.GetAttributes(metadata) & FileAttributes.ReparsePoint) != 0)
			throw new InvalidDataException("연결 또는 재분석 지점에는 운영 기록을 저장할 수 없습니다.");
		if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
			throw new InvalidDataException("연결 또는 재분석 지점 파일에는 운영 기록을 저장할 수 없습니다.");
		return path;
	}

	private static OperationsHistoryDocument ReadOperationsHistory(string serverDirectory)
	{
		string path = GetOperationsHistoryPath(serverDirectory);
		return WithOperationsHistoryLock(path, delegate
		{
			return ReadOperationsHistoryUnlocked(path);
		});
	}

	private static OperationsHistoryDocument ReadOperationsHistoryUnlocked(string path)
	{
		if (!File.Exists(path)) return new OperationsHistoryDocument();
		FileInfo info = new FileInfo(path);
		if (info.Length <= 0 || info.Length > OperationsHistoryMaximumBytes)
			throw new InvalidDataException("운영 기록 파일 크기가 올바르지 않습니다.");
		OperationsHistoryDocument document;
		try
		{
			document = new JavaScriptSerializer { MaxJsonLength = OperationsHistoryMaximumBytes }.Deserialize<OperationsHistoryDocument>(File.ReadAllText(path, Encoding.UTF8));
		}
		catch (Exception exception)
		{
			throw new InvalidDataException("운영 기록 파일이 손상되었습니다. 원본 파일은 변경하지 않았습니다.", exception);
		}
		ValidateOperationsHistory(document);
		return document;
	}

	private static void RecordOperationEvent(
		string serverDirectory,
		string category,
		string severity,
		string messageKo,
		string messageEn,
		string source,
		bool initiallyRead)
	{
		string path = GetOperationsHistoryPath(serverDirectory);
		WithOperationsHistoryLock(path, delegate
		{
			OperationsHistoryDocument document = ReadOperationsHistoryUnlocked(path);
			OperationsHistoryEntry entry = new OperationsHistoryEntry();
			entry.Id = Guid.NewGuid().ToString("N");
			entry.CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
			entry.Category = NormalizeOperationCategory(category);
			entry.Severity = NormalizeOperationSeverity(severity);
			entry.Source = NormalizeOperationSource(source);
			entry.MessageKo = SanitizeOperationMessage(messageKo, serverDirectory);
			entry.MessageEn = SanitizeOperationMessage(messageEn, serverDirectory);
			entry.IsRead = initiallyRead;
			entry.PreviousHash = document.Entries.Count == 0 ? document.ChainAnchor : document.Entries[document.Entries.Count - 1].Hash;
			entry.Hash = CalculateOperationEntryHash(entry);
			document.Entries.Add(entry);
			while (document.Entries.Count > OperationsHistoryMaximumEntries)
			{
				document.ChainAnchor = document.Entries[0].Hash;
				document.Entries.RemoveAt(0);
			}
			WriteOperationsHistoryUnlocked(path, document);
			return true;
		});
	}

	private static void TryRecordOperationEvent(
		string serverDirectory,
		string category,
		string severity,
		string messageKo,
		string messageEn,
		string source,
		bool initiallyRead)
	{
		try { RecordOperationEvent(serverDirectory, category, severity, messageKo, messageEn, source, initiallyRead); }
		catch (Exception exception) { Console.WriteLine("[Operations] 운영 기록 저장 실패 (" + exception.GetType().Name + ")"); }
	}

	private static bool MarkOperationEventRead(string serverDirectory, string entryId, bool isRead)
	{
		if (string.IsNullOrWhiteSpace(entryId)) return false;
		string path = GetOperationsHistoryPath(serverDirectory);
		return WithOperationsHistoryLock(path, delegate
		{
			OperationsHistoryDocument document = ReadOperationsHistoryUnlocked(path);
			for (int i = 0; i < document.Entries.Count; i++)
			{
				if (!string.Equals(document.Entries[i].Id, entryId, StringComparison.OrdinalIgnoreCase)) continue;
				if (document.Entries[i].IsRead == isRead) return true;
				document.Entries[i].IsRead = isRead;
				WriteOperationsHistoryUnlocked(path, document);
				return true;
			}
			return false;
		});
	}

	private static int MarkAllOperationEventsRead(string serverDirectory)
	{
		string path = GetOperationsHistoryPath(serverDirectory);
		return WithOperationsHistoryLock(path, delegate
		{
			OperationsHistoryDocument document = ReadOperationsHistoryUnlocked(path);
			int changed = 0;
			for (int i = 0; i < document.Entries.Count; i++)
			{
				if (document.Entries[i].IsRead) continue;
				document.Entries[i].IsRead = true;
				changed++;
			}
			if (changed > 0) WriteOperationsHistoryUnlocked(path, document);
			return changed;
		});
	}

	private static void WriteOperationsHistoryUnlocked(string path, OperationsHistoryDocument document)
	{
		ValidateOperationsHistory(document);
		string json = new JavaScriptSerializer { MaxJsonLength = OperationsHistoryMaximumBytes }.Serialize(document);
		if (Encoding.UTF8.GetByteCount(json) > OperationsHistoryMaximumBytes)
			throw new InvalidDataException("운영 기록 파일이 허용 크기를 초과했습니다.");
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string temporary = path + ".준비중";
		File.WriteAllText(temporary, json, new UTF8Encoding(false));
		ReplaceFile(temporary, path);
	}

	private static void ValidateOperationsHistory(OperationsHistoryDocument document)
	{
		if (document == null || document.SchemaVersion != OperationsHistorySchemaVersion)
			throw new InvalidDataException("지원하지 않는 운영 기록 스키마입니다.");
		if (document.Entries == null) document.Entries = new List<OperationsHistoryEntry>();
		if (document.Entries.Count > OperationsHistoryMaximumEntries)
			throw new InvalidDataException("운영 기록 항목 수가 허용 범위를 초과했습니다.");
		if (!IsOperationHash(document.ChainAnchor, true))
			throw new InvalidDataException("운영 기록 연결 기준값이 올바르지 않습니다.");
		string previous = document.ChainAnchor ?? string.Empty;
		HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		DateTime parsed;
		for (int i = 0; i < document.Entries.Count; i++)
		{
			OperationsHistoryEntry entry = document.Entries[i];
			if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || entry.Id.Length > 80 || !identifiers.Add(entry.Id))
				throw new InvalidDataException("운영 기록 식별자가 없거나 중복되었습니다.");
			if (!DateTime.TryParse(entry.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
				throw new InvalidDataException("운영 기록 시각이 올바르지 않습니다.");
			if (!string.Equals(entry.Category, NormalizeOperationCategory(entry.Category), StringComparison.Ordinal)
				|| !string.Equals(entry.Severity, NormalizeOperationSeverity(entry.Severity), StringComparison.Ordinal)
				|| !string.Equals(entry.Source, NormalizeOperationSource(entry.Source), StringComparison.Ordinal))
				throw new InvalidDataException("운영 기록 분류가 올바르지 않습니다.");
			if (string.IsNullOrWhiteSpace(entry.MessageKo) || string.IsNullOrWhiteSpace(entry.MessageEn)
				|| entry.MessageKo.Length > 1000 || entry.MessageEn.Length > 1000)
				throw new InvalidDataException("운영 기록 문구가 올바르지 않습니다.");
			if (!string.Equals(entry.PreviousHash ?? string.Empty, previous, StringComparison.OrdinalIgnoreCase)
				|| !IsOperationHash(entry.Hash, false)
				|| !string.Equals(entry.Hash, CalculateOperationEntryHash(entry), StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("운영 기록 연속 해시가 일치하지 않습니다. 원본 파일은 변경하지 않았습니다.");
			previous = entry.Hash;
		}
	}

	private static string CalculateOperationEntryHash(OperationsHistoryEntry entry)
	{
		string canonical = string.Join("\n", new string[]
		{
			entry.Id ?? string.Empty,
			entry.CreatedUtc ?? string.Empty,
			entry.Category ?? string.Empty,
			entry.Severity ?? string.Empty,
			entry.Source ?? string.Empty,
			entry.MessageKo ?? string.Empty,
			entry.MessageEn ?? string.Empty,
			entry.PreviousHash ?? string.Empty
		});
		using (SHA256 hash = SHA256.Create())
		{
			return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", string.Empty).ToLowerInvariant();
		}
	}

	private static bool IsOperationHash(string value, bool allowEmpty)
	{
		if (string.IsNullOrEmpty(value)) return allowEmpty;
		if (value.Length != 64) return false;
		for (int i = 0; i < value.Length; i++)
		{
			char character = value[i];
			if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'))) return false;
		}
		return true;
	}

	private static string NormalizeOperationCategory(string value)
	{
		string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
		switch (normalized)
		{
			case "server":
			case "automation":
			case "backup":
			case "content":
			case "network":
			case "update":
			case "security":
				return normalized;
			default:
				return "system";
		}
	}

	private static string NormalizeOperationSeverity(string value)
	{
		string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
		return normalized == "error" || normalized == "warning" ? normalized : "info";
	}

	private static string NormalizeOperationSource(string value)
	{
		string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
		switch (normalized)
		{
			case "user":
			case "automation":
			case "launcher":
			case "recovery":
			case "background-agent":
				return normalized;
			default:
				return "system";
		}
	}

	private static string SanitizeOperationMessage(string value, string serverDirectory)
	{
		string text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('\0', ' ').Trim();
		if (!string.IsNullOrWhiteSpace(serverDirectory))
		{
			string fullPath = Path.GetFullPath(serverDirectory);
			text = ReplaceOperationText(text, fullPath, "[server]");
		}
		text = Regex.Replace(text, @"(?<![\d.])(?:\d{1,3}\.){3}\d{1,3}(?![\d.])", delegate(Match match)
		{
			string[] octets = match.Value.Split('.');
			for (int i = 0; i < octets.Length; i++)
			{
				int parsedOctet;
				if (!int.TryParse(octets[i], NumberStyles.None, CultureInfo.InvariantCulture, out parsedOctet) || parsedOctet > 255) return match.Value;
			}
			return "[IP]";
		});
		string lower = text.ToLowerInvariant();
		foreach (string secretMarker in new string[] { "discord.com/api/webhooks/", "discordapp.com/api/webhooks/", "token=", "password=", "secret=", "webhook=" })
		{
			int index = lower.IndexOf(secretMarker, StringComparison.Ordinal);
			if (index < 0) continue;
			text = text.Substring(0, index).TrimEnd() + " [민감 정보 가림 / sensitive value redacted]";
			break;
		}
		if (text.Length > 1000) text = text.Substring(0, 997) + "...";
		return string.IsNullOrWhiteSpace(text) ? "-" : text;
	}

	private static string ReplaceOperationText(string text, string oldValue, string newValue)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue)) return text;
		StringBuilder result = new StringBuilder(text.Length);
		int start = 0;
		while (start < text.Length)
		{
			int index = text.IndexOf(oldValue, start, StringComparison.OrdinalIgnoreCase);
			if (index < 0)
			{
				result.Append(text, start, text.Length - start);
				break;
			}
			result.Append(text, start, index - start);
			result.Append(newValue);
			start = index + oldValue.Length;
		}
		return result.ToString();
	}

	private static T WithOperationsHistoryLock<T>(string path, Func<T> action)
	{
		lock (OperationsHistoryProcessLock)
		{
			using (Mutex mutex = new Mutex(false, GetOperationsHistoryMutexName(path)))
			{
				bool entered = false;
				try
				{
					try { entered = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
					catch (AbandonedMutexException) { entered = true; }
					if (!entered) throw new IOException("다른 MineHarbor 프로세스가 운영 기록을 갱신하고 있습니다.");
					return action();
				}
				finally
				{
					if (entered) mutex.ReleaseMutex();
				}
			}
		}
	}

	private static string GetOperationsHistoryMutexName(string path)
	{
		using (SHA256 hash = SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()))).Replace("-", string.Empty).Substring(0, 24);
			return "Local\\MineHarbor.OperationsHistory." + suffix;
		}
	}

	private sealed class OperationsHistoryForm : Form
	{
		private readonly string serversRoot;
		private readonly ComboBox serverBox;
		private readonly ComboBox severityBox;
		private readonly CheckBox unreadOnlyBox;
		private readonly ListView historyList;
		private readonly Label summaryLabel;
		private readonly List<ManagedProfileRecord> profiles;

		public OperationsHistoryForm(string rootDirectory)
		{
			serversRoot = Path.GetFullPath(rootDirectory);
			profiles = ReadManagedProfiles(serversRoot);
			bool korean = IsManagedKorean();
			Text = korean ? "알림 및 운영 기록" : "Notifications and operations";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(900, 600);
			Size = new Size(1080, 720);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			KeyPreview = true;
			ApplyLauncherWindowIcon(this);

			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 };
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);

			Label heading = new Label
			{
				AutoSize = true,
				Font = new Font(ThemeFonts.Display, 18F, FontStyle.Bold),
				Text = korean ? "서버 운영 기록" : "Server operations",
				Margin = new Padding(0, 0, 0, 12)
			};
			root.Controls.Add(heading, 0, 0);

			FlowLayoutPanel filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 0, 0, 10) };
			filters.Controls.Add(new Label { AutoSize = true, Text = korean ? "서버" : "Server", Margin = new Padding(0, 9, 5, 0) });
			serverBox = new ModernComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = korean ? "서버 필터" : "Server filter" };
			serverBox.Items.Add(korean ? "모든 서버" : "All servers");
			for (int i = 0; i < profiles.Count; i++) serverBox.Items.Add(profiles[i].Name);
			serverBox.SelectedIndex = 0;
			filters.Controls.Add(serverBox);
			filters.Controls.Add(new Label { AutoSize = true, Text = korean ? "중요도" : "Severity", Margin = new Padding(14, 9, 5, 0) });
			severityBox = new ModernComboBox { Width = 135, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = korean ? "중요도 필터" : "Severity filter" };
			severityBox.Items.AddRange(korean ? new object[] { "전체", "정보", "경고", "오류" } : new object[] { "All", "Info", "Warning", "Error" });
			severityBox.SelectedIndex = 0;
			filters.Controls.Add(severityBox);
			unreadOnlyBox = new ModernCheckBox { AutoSize = true, Text = korean ? "읽지 않음만" : "Unread only", Margin = new Padding(16, 7, 0, 0) };
			filters.Controls.Add(unreadOnlyBox);
			root.Controls.Add(filters, 0, 1);

			historyList = new BufferedListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				HideSelection = false,
				MultiSelect = false,
				AccessibleName = korean ? "운영 기록 목록" : "Operations history list",
				AccessibleDescription = korean ? "시각, 읽음 상태, 중요도, 분류, 서버와 내용을 표시합니다." : "Shows time, read state, severity, category, server, and details."
			};
			historyList.Columns.Add(korean ? "시각" : "Time", 155);
			historyList.Columns.Add(korean ? "상태" : "State", 80);
			historyList.Columns.Add(korean ? "중요도" : "Severity", 90);
			historyList.Columns.Add(korean ? "분류" : "Category", 100);
			historyList.Columns.Add(korean ? "서버" : "Server", 150);
			historyList.Columns.Add(korean ? "내용" : "Details", 430);
			root.Controls.Add(historyList, 0, 2);

			summaryLabel = new Label { AutoSize = true, Margin = new Padding(0, 8, 0, 4), AccessibleName = korean ? "운영 기록 상태" : "Operations history status" };
			root.Controls.Add(summaryLabel, 0, 3);

			FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 6, 0, 0) };
			Button markRead = MultiServerDashboardForm.NewManagedButton(korean ? "선택 읽음" : "Mark read", 112, "secondary");
			Button markAll = MultiServerDashboardForm.NewManagedButton(korean ? "모두 읽음" : "Mark all read", 126, "secondary");
			Button notificationSettings = MultiServerDashboardForm.NewManagedButton(korean ? "알림 설정" : "Notifications", 118, "secondary");
			Button export = MultiServerDashboardForm.NewManagedButton(korean ? "내보내기" : "Export", 108, "secondary");
			Button refresh = MultiServerDashboardForm.NewManagedButton(korean ? "새로고침" : "Refresh", 108, "primary");
			markRead.Click += delegate { MarkSelectedRead(); };
			markAll.Click += delegate { MarkAllRead(); };
			notificationSettings.Click += delegate
			{
				using (WindowsNotificationSettingsForm form = new WindowsNotificationSettingsForm()) form.ShowDialog(this);
			};
			export.Click += delegate { ExportVisibleHistory(); };
			refresh.Click += delegate { ReloadHistory(); };
			actions.Controls.AddRange(new Control[] { markRead, markAll, notificationSettings, export, refresh });
			root.Controls.Add(actions, 0, 4);

			serverBox.SelectedIndexChanged += delegate { ReloadHistory(); };
			severityBox.SelectedIndexChanged += delegate { ReloadHistory(); };
			unreadOnlyBox.CheckedChanged += delegate { ReloadHistory(); };
			historyList.DoubleClick += delegate { MarkSelectedRead(); };
			Shown += delegate { ReloadHistory(); };
			ApplySimpleDialogTheme(this);
			ApplyCommonButtonToolTips(this);
		}

		private void ReloadHistory()
		{
			bool korean = IsManagedKorean();
			historyList.BeginUpdate();
			historyList.Items.Clear();
			int total = 0;
			int unread = 0;
			int corrupted = 0;
			try
			{
				for (int profileIndex = 0; profileIndex < profiles.Count; profileIndex++)
				{
					ManagedProfileRecord profile = profiles[profileIndex];
					if (serverBox.SelectedIndex > 0 && !string.Equals(Convert.ToString(serverBox.SelectedItem), profile.Name, StringComparison.OrdinalIgnoreCase)) continue;
					OperationsHistoryDocument document;
					try { document = ReadOperationsHistory(profile.Directory); }
					catch (Exception exception)
					{
						corrupted++;
						ListViewItem errorItem = new ListViewItem("-");
						errorItem.SubItems.Add("-");
						errorItem.SubItems.Add(korean ? "오류" : "Error");
						errorItem.SubItems.Add(korean ? "저장소" : "Storage");
						errorItem.SubItems.Add(profile.Name);
						errorItem.SubItems.Add((korean ? "운영 기록을 읽거나 검증하지 못했습니다. 원본은 변경하지 않았습니다. (" : "Could not read or verify operations history. The original was not changed. (") + exception.GetType().Name + ")");
						historyList.Items.Add(errorItem);
						continue;
					}
					for (int i = document.Entries.Count - 1; i >= 0; i--)
					{
						OperationsHistoryEntry entry = document.Entries[i];
						if (!MatchesSeverityFilter(entry.Severity) || (unreadOnlyBox.Checked && entry.IsRead)) continue;
						DateTime created = DateTime.Parse(entry.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();
						ListViewItem item = new ListViewItem(created.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture));
						item.SubItems.Add(entry.IsRead ? (korean ? "읽음" : "Read") : (korean ? "새 알림" : "Unread"));
						item.SubItems.Add(OperationSeverityText(entry.Severity));
						item.SubItems.Add(OperationCategoryText(entry.Category));
						item.SubItems.Add(profile.Name);
						item.SubItems.Add(korean ? entry.MessageKo : entry.MessageEn);
						item.Tag = new OperationsHistoryListReference { ServerDirectory = profile.Directory, EntryId = entry.Id };
						historyList.Items.Add(item);
						total++;
						if (!entry.IsRead) unread++;
					}
				}
			}
			finally { historyList.EndUpdate(); }
			summaryLabel.Text = corrupted > 0
				? (korean ? "표시 " + total + "개 · 읽지 않음 " + unread + "개 · 손상 또는 읽기 실패 " + corrupted + "개 서버" : total + " shown · " + unread + " unread · " + corrupted + " server stores could not be read")
				: (korean ? "표시 " + total + "개 · 읽지 않음 " + unread + "개 · 연속 해시 검증 완료" : total + " shown · " + unread + " unread · hash chain verified");
			summaryLabel.AccessibleDescription = summaryLabel.Text;
		}

		private bool MatchesSeverityFilter(string severity)
		{
			if (severityBox.SelectedIndex <= 0) return true;
			string[] values = { string.Empty, "info", "warning", "error" };
			return string.Equals(severity, values[severityBox.SelectedIndex], StringComparison.OrdinalIgnoreCase);
		}

		private void MarkSelectedRead()
		{
			if (historyList.SelectedItems.Count == 0) return;
			OperationsHistoryListReference reference = historyList.SelectedItems[0].Tag as OperationsHistoryListReference;
			if (reference == null) return;
			try
			{
				MarkOperationEventRead(reference.ServerDirectory, reference.EntryId, true);
				ReloadHistory();
			}
			catch (Exception exception) { ShowMineHarborDialog(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
		}

		private void MarkAllRead()
		{
			try
			{
				for (int i = 0; i < profiles.Count; i++)
				{
					if (serverBox.SelectedIndex > 0 && !string.Equals(Convert.ToString(serverBox.SelectedItem), profiles[i].Name, StringComparison.OrdinalIgnoreCase)) continue;
					MarkAllOperationEventsRead(profiles[i].Directory);
				}
				ReloadHistory();
			}
			catch (Exception exception) { ShowMineHarborDialog(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
		}

		private void ExportVisibleHistory()
		{
			bool korean = IsManagedKorean();
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Title = korean ? "운영 기록 내보내기" : "Export operations history";
				dialog.Filter = "CSV (*.csv)|*.csv";
				dialog.FileName = "MineHarbor-operations-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv";
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				StringBuilder csv = new StringBuilder();
				csv.AppendLine("\"Time\",\"State\",\"Severity\",\"Category\",\"Server\",\"Details\"");
				for (int i = 0; i < historyList.Items.Count; i++)
				{
					ListViewItem item = historyList.Items[i];
					for (int column = 0; column < item.SubItems.Count; column++)
					{
						if (column > 0) csv.Append(',');
						csv.Append('"').Append(item.SubItems[column].Text.Replace("\"", "\"\"")).Append('"');
					}
					csv.AppendLine();
				}
				File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
			}
		}

		protected override bool ProcessCmdKey(ref Message message, Keys keyData)
		{
			if (keyData == Keys.F5) { ReloadHistory(); return true; }
			if (keyData == (Keys.Control | Keys.E)) { ExportVisibleHistory(); return true; }
			return base.ProcessCmdKey(ref message, keyData);
		}
	}

	private static string OperationSeverityText(string value)
	{
		if (string.Equals(value, "error", StringComparison.OrdinalIgnoreCase)) return ManagedText("오류", "Error");
		if (string.Equals(value, "warning", StringComparison.OrdinalIgnoreCase)) return ManagedText("경고", "Warning");
		return ManagedText("정보", "Info");
	}

	private static string OperationCategoryText(string value)
	{
		if (string.Equals(value, "server", StringComparison.OrdinalIgnoreCase)) return ManagedText("서버", "Server");
		if (string.Equals(value, "automation", StringComparison.OrdinalIgnoreCase)) return ManagedText("자동화", "Automation");
		if (string.Equals(value, "backup", StringComparison.OrdinalIgnoreCase)) return ManagedText("백업", "Backup");
		if (string.Equals(value, "content", StringComparison.OrdinalIgnoreCase)) return ManagedText("콘텐츠", "Content");
		if (string.Equals(value, "network", StringComparison.OrdinalIgnoreCase)) return ManagedText("네트워크", "Network");
		if (string.Equals(value, "update", StringComparison.OrdinalIgnoreCase)) return ManagedText("업데이트", "Update");
		if (string.Equals(value, "security", StringComparison.OrdinalIgnoreCase)) return ManagedText("보안", "Security");
		return ManagedText("시스템", "System");
	}

	private sealed partial class LauncherForm
	{
		private Button mainOperationsButton;

		private void OpenMainOperationsHistory()
		{
			string root = GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory);
			ShowModelessToolWindow("operations-history", delegate { return new OperationsHistoryForm(root); }, false, null);
		}

		private void TryRecordActiveServerOperation(string category, string severity, string messageKo, string messageEn, string source)
		{
			try
			{
				string root;
				string directory;
				ReadActiveLauncherOptions(out root, out directory);
				TryRecordOperationEvent(directory, category, severity, messageKo, messageEn, source, false);
			}
			catch (InvalidOperationException) { }
			catch (Exception exception) { Console.WriteLine("[Operations] 활성 서버 기록 생략 (" + exception.GetType().Name + ")"); }
		}
	}
}
