using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static partial class Launcher
{
	private const int AutomationSchemaVersion = 2;
	private const int AutomationFileMaximumBytes = 1048576;
	private static readonly object AutomationFileLock = new object();

	private sealed class ServerAutomationConfiguration
	{
		public int SchemaVersion = AutomationSchemaVersion;
		public bool BackupBeforeStart;
		public bool BackupAfterStop;
		public int RetentionCount = 10;
		public int RetentionDays = 30;
		public long RetentionMaximumBytes = 21474836480L;
		public List<ServerAutomationJob> Jobs = new List<ServerAutomationJob>();
	}

	private sealed class ServerAutomationJob
	{
		public string Id;
		public string Name;
		public string Action;
		public string Command;
		public bool Enabled = true;
		public string ScheduleKind = "interval";
		public int IntervalMinutes = 60;
		public string DailyLocalTime = "04:00";
		public string Weekdays = "Monday";
		public string OnceLocalDateTime = string.Empty;
		public string MissedRunPolicy = "run-once";
		public int MaximumDelayMinutes = 1440;
		public int WarningSeconds = 60;
		public string NextRunUtc;
		public string LastRunUtc;
		public string LastResult;
		public bool Running;
		public string LeaseUtc;
		public int LeaseProcessId;
		public long LeaseProcessStartTicks;
	}

	private sealed class AutomationJobClaim
	{
		public string ServerDirectory;
		public ServerAutomationJob Job;
	}

	private static string GetAutomationConfigurationPath(string serverDirectory)
	{
		return Path.Combine(Path.Combine(Path.GetFullPath(serverDirectory), ".mineharbor"), "automation.json");
	}

	private static ServerAutomationConfiguration ReadServerAutomationConfiguration(string serverDirectory)
	{
		lock (AutomationFileLock)
		{
			return ReadServerAutomationConfigurationUnlocked(serverDirectory);
		}
	}

	private static ServerAutomationConfiguration ReadServerAutomationConfigurationUnlocked(string serverDirectory)
	{
		string path = GetAutomationConfigurationPath(serverDirectory);
		if (!File.Exists(path)) { ServerAutomationConfiguration defaults = new ServerAutomationConfiguration(); defaults.RetentionCount = ReadBackupRetentionCount(serverDirectory); return defaults; }
		FileInfo info = new FileInfo(path);
		if (info.Length <= 0 || info.Length > AutomationFileMaximumBytes)
		{
			throw Localized(new InvalidDataException("자동화 설정 파일 크기가 올바르지 않습니다."), "The automation settings file size is invalid.");
		}
		ServerAutomationConfiguration configuration;
		try
		{
			configuration = new JavaScriptSerializer().Deserialize<ServerAutomationConfiguration>(File.ReadAllText(path));
		}
		catch (Exception exception)
		{
			throw Localized(new InvalidDataException("자동화 설정 파일이 손상되었습니다. 원본 파일은 변경하지 않았습니다.", exception), "The automation settings file is damaged. The original file was left unchanged.");
		}
		MigrateServerAutomationConfiguration(configuration);
		ValidateServerAutomationConfiguration(configuration);
		return configuration;
	}

	private static void MigrateServerAutomationConfiguration(ServerAutomationConfiguration configuration)
	{
		if (configuration == null) throw Localized(new InvalidDataException("자동화 설정 파일이 비어 있습니다."), "The automation settings file is empty.");
		if (configuration.SchemaVersion > AutomationSchemaVersion || configuration.SchemaVersion < 1)
			throw Localized(new InvalidDataException("지원하지 않는 자동화 설정 버전입니다."), "Unsupported automation settings version.");
		if (configuration.Jobs == null) configuration.Jobs = new List<ServerAutomationJob>();
		if (configuration.SchemaVersion == 1)
		{
			configuration.SchemaVersion = AutomationSchemaVersion;
			for (int i = 0; i < configuration.Jobs.Count; i++)
			{
				ServerAutomationJob job = configuration.Jobs[i];
				if (job == null) continue;
				if (string.IsNullOrWhiteSpace(job.Weekdays)) job.Weekdays = "Monday";
				if (string.IsNullOrWhiteSpace(job.MissedRunPolicy)) job.MissedRunPolicy = "run-once";
				if (job.MaximumDelayMinutes <= 0) job.MaximumDelayMinutes = 1440;
			}
		}
	}

	private static void WriteServerAutomationConfiguration(string serverDirectory, ServerAutomationConfiguration configuration)
	{
		ValidateServerAutomationConfiguration(configuration);
		lock (AutomationFileLock)
		{
			WithAutomationCrossProcessLock(serverDirectory, delegate
			{
				string path = GetAutomationConfigurationPath(serverDirectory);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				WriteJsonAtomic(path, configuration);
				WriteBackupRetentionCount(serverDirectory, configuration.RetentionCount);
				return 0;
			});
		}
	}

	private static T WithAutomationCrossProcessLock<T>(string serverDirectory, Func<T> action)
	{
		string path = GetAutomationConfigurationPath(serverDirectory);
		using (SHA256 hash = SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()))).Replace("-", string.Empty).Substring(0, 24);
			using (Mutex mutex = new Mutex(false, "Local\\MineHarbor.Automation." + suffix))
			{
				bool entered = false;
				try
				{
					try { entered = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
					catch (AbandonedMutexException) { entered = true; }
					if (!entered) throw Localized(new IOException("다른 MineHarbor 프로세스가 예약 설정을 갱신하고 있습니다."), "Another MineHarbor process is updating the schedule settings.");
					return action();
				}
				finally { if (entered) mutex.ReleaseMutex(); }
			}
		}
	}

	private static void ValidateServerAutomationConfiguration(ServerAutomationConfiguration configuration)
	{
		if (configuration == null || configuration.SchemaVersion != AutomationSchemaVersion)
			throw Localized(new InvalidDataException("지원하지 않는 자동화 설정 버전입니다."), "Unsupported automation settings version.");
		configuration.RetentionCount = Math.Max(1, Math.Min(200, configuration.RetentionCount));
		configuration.RetentionDays = Math.Max(1, Math.Min(3650, configuration.RetentionDays));
		configuration.RetentionMaximumBytes = Math.Max(104857600L, Math.Min(10995116277760L, configuration.RetentionMaximumBytes));
		if (configuration.Jobs == null) configuration.Jobs = new List<ServerAutomationJob>();
		if (configuration.Jobs.Count > 200) throw Localized(new InvalidDataException("예약 작업은 서버당 200개를 넘을 수 없습니다."), "A server cannot have more than 200 scheduled jobs.");
		HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < configuration.Jobs.Count; i++)
		{
			ServerAutomationJob job = configuration.Jobs[i];
			if (job == null || string.IsNullOrWhiteSpace(job.Id) || job.Id.Length > 80 || !identifiers.Add(job.Id))
				throw Localized(new InvalidDataException("예약 작업 식별자가 없거나 중복되었습니다."), "A scheduled job identifier is missing or duplicated.");
			if (string.IsNullOrWhiteSpace(job.Name) || job.Name.Length > 120)
				throw Localized(new InvalidDataException("예약 작업 이름은 1~120자로 입력해야 합니다."), "A scheduled job name must be 1 to 120 characters.");
			if (!IsSupportedAutomationAction(job.Action)) throw Localized(new InvalidDataException("지원하지 않는 예약 작업 종류입니다."), "Unsupported scheduled job type.");
			if (string.Equals(job.Action, "command", StringComparison.OrdinalIgnoreCase)) ValidateScheduledCommand(job.Command);
			if (job.WarningSeconds < 0 || job.WarningSeconds > 3600) throw Localized(new InvalidDataException("공지 시간은 0~3600초여야 합니다."), "The warning time must be between 0 and 3600 seconds.");
			if (string.Equals(job.ScheduleKind, "interval", StringComparison.OrdinalIgnoreCase))
			{
				if (job.IntervalMinutes < 1 || job.IntervalMinutes > 525600) throw Localized(new InvalidDataException("반복 간격은 1분~365일이어야 합니다."), "The repeat interval must be between 1 minute and 365 days.");
			}
			else if (string.Equals(job.ScheduleKind, "daily", StringComparison.OrdinalIgnoreCase))
			{
				ValidateAutomationLocalTime(job.DailyLocalTime);
			}
			else if (string.Equals(job.ScheduleKind, "weekly", StringComparison.OrdinalIgnoreCase))
			{
				ValidateAutomationLocalTime(job.DailyLocalTime);
				ParseAutomationWeekdays(job.Weekdays);
			}
			else if (string.Equals(job.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase))
			{
				DateTime ignored;
				if (!DateTime.TryParseExact(job.OnceLocalDateTime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out ignored))
					throw Localized(new InvalidDataException("일회성 실행 시각은 yyyy-MM-dd HH:mm 형식이어야 합니다."), "A one-time run time must use the yyyy-MM-dd HH:mm format.");
			}
			else throw Localized(new InvalidDataException("지원하지 않는 예약 방식입니다."), "Unsupported schedule kind.");
			if (!string.Equals(job.MissedRunPolicy, "run-once", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(job.MissedRunPolicy, "skip", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(job.MissedRunPolicy, "notify-only", StringComparison.OrdinalIgnoreCase))
				throw Localized(new InvalidDataException("놓친 작업 처리 방식이 올바르지 않습니다."), "The missed-run policy is invalid.");
			if (job.MaximumDelayMinutes < 1 || job.MaximumDelayMinutes > 525600)
				throw Localized(new InvalidDataException("최대 지연 시간은 1분~365일이어야 합니다."), "The maximum delay must be between 1 minute and 365 days.");
			DateTime parsed;
			if (!string.IsNullOrEmpty(job.NextRunUtc) && !DateTime.TryParse(job.NextRunUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
				throw Localized(new InvalidDataException("다음 실행 시각이 올바르지 않습니다."), "The next run time is invalid.");
		}
	}

	private static void ValidateAutomationLocalTime(string value)
	{
		DateTime ignored;
		if (!DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out ignored))
			throw Localized(new InvalidDataException("실행 시각은 HH:mm 형식이어야 합니다."), "A run time must use the HH:mm format.");
	}

	private static HashSet<DayOfWeek> ParseAutomationWeekdays(string value)
	{
		HashSet<DayOfWeek> days = new HashSet<DayOfWeek>();
		DayOfWeek[] allowed = { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
		string[] values = (value ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < values.Length; i++)
		{
			string candidate = values[i].Trim();
			bool found = false;
			for (int dayIndex = 0; dayIndex < allowed.Length; dayIndex++)
			{
				if (!string.Equals(candidate, allowed[dayIndex].ToString(), StringComparison.OrdinalIgnoreCase)) continue;
				days.Add(allowed[dayIndex]);
				found = true;
				break;
			}
			if (!found) throw Localized(new InvalidDataException("실행 요일 값이 올바르지 않습니다."), "A run weekday value is invalid.");
		}
		if (days.Count == 0) throw Localized(new InvalidDataException("매주 실행할 요일을 하나 이상 선택해야 합니다."), "Select at least one weekday for a weekly schedule.");
		return days;
	}

	private static bool IsSupportedAutomationAction(string action)
	{
		return string.Equals(action, "backup", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(action, "start", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(action, "stop", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(action, "command", StringComparison.OrdinalIgnoreCase);
	}

	private static void ValidateScheduledCommand(string command)
	{
		if (string.IsNullOrWhiteSpace(command) || command.Length > 2048 || command.IndexOf('\r') >= 0 || command.IndexOf('\n') >= 0 || command.IndexOf('\0') >= 0)
			throw Localized(new InvalidDataException("예약 명령은 줄바꿈 없이 1~2048자로 입력해야 합니다."), "A scheduled command must be 1 to 2048 characters with no line breaks.");
	}

	private static DateTime CalculateNextAutomationRunUtc(ServerAutomationJob job, DateTime afterUtc)
	{
		if (string.Equals(job.ScheduleKind, "interval", StringComparison.OrdinalIgnoreCase))
			return afterUtc.AddMinutes(Math.Max(1, job.IntervalMinutes));
		DateTime localAfter = afterUtc.ToLocalTime();
		if (string.Equals(job.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase))
		{
			DateTime once = DateTime.ParseExact(job.OnceLocalDateTime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None);
			return DateTime.SpecifyKind(once, DateTimeKind.Local).ToUniversalTime();
		}
		DateTime time = DateTime.ParseExact(job.DailyLocalTime, "HH:mm", CultureInfo.InvariantCulture);
		if (string.Equals(job.ScheduleKind, "weekly", StringComparison.OrdinalIgnoreCase))
		{
			HashSet<DayOfWeek> days = ParseAutomationWeekdays(job.Weekdays);
			for (int offset = 0; offset <= 7; offset++)
			{
				DateTime date = localAfter.Date.AddDays(offset);
				if (!days.Contains(date.DayOfWeek)) continue;
				DateTime weekly = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0, DateTimeKind.Local);
				if (weekly > localAfter) return weekly.ToUniversalTime();
			}
			throw Localized(new InvalidDataException("다음 주간 실행 시각을 계산하지 못했습니다."), "Could not calculate the next weekly run time.");
		}
		DateTime candidate = new DateTime(localAfter.Year, localAfter.Month, localAfter.Day, time.Hour, time.Minute, 0, DateTimeKind.Local);
		if (candidate <= localAfter) candidate = candidate.AddDays(1);
		return candidate.ToUniversalTime();
	}

	private static List<AutomationJobClaim> ClaimDueAutomationJobs(string serverDirectory, DateTime nowUtc)
	{
		lock (AutomationFileLock)
		{
			return WithAutomationCrossProcessLock(serverDirectory, delegate
			{
			ServerAutomationConfiguration configuration = ReadServerAutomationConfigurationUnlocked(serverDirectory);
			List<AutomationJobClaim> claims = new List<AutomationJobClaim>();
			List<ServerAutomationJob> missedNotifications = new List<ServerAutomationJob>();
			bool changed = false;
			for (int i = 0; i < configuration.Jobs.Count; i++)
			{
				ServerAutomationJob job = configuration.Jobs[i];
				DateTime lease;
				bool legacyLeaseExpired = !DateTime.TryParse(job.LeaseUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out lease) || nowUtc - lease.ToUniversalTime() > TimeSpan.FromMinutes(30);
				bool ownerGone = job.LeaseProcessId > 0 && job.LeaseProcessStartTicks > 0 && !IsAutomationLeaseOwnerAlive(job.LeaseProcessId, job.LeaseProcessStartTicks);
				if (job.Running && (ownerGone || ((job.LeaseProcessId <= 0 || job.LeaseProcessStartTicks <= 0) && legacyLeaseExpired)))
				{
					job.Running = false;
					job.LeaseUtc = null;
					job.LeaseProcessId = 0;
					job.LeaseProcessStartTicks = 0L;
					job.LastResult = "이전 실행 임대가 만료되어 복구됨 / Recovered expired execution lease";
					changed = true;
				}
				if (!job.Enabled || job.Running) continue;
				DateTime next;
				if (string.IsNullOrEmpty(job.NextRunUtc) || !DateTime.TryParse(job.NextRunUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out next))
				{
					job.NextRunUtc = CalculateNextAutomationRunUtc(job, nowUtc).ToString("o", CultureInfo.InvariantCulture);
					changed = true;
					continue;
				}
				if (next.ToUniversalTime() > nowUtc) continue;
				TimeSpan delay = nowUtc - next.ToUniversalTime();
				bool tooLate = delay > TimeSpan.FromMinutes(job.MaximumDelayMinutes);
				bool skipMissed = tooLate && (string.Equals(job.MissedRunPolicy, "skip", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(job.MissedRunPolicy, "notify-only", StringComparison.OrdinalIgnoreCase));
				if (skipMissed)
				{
					job.LastResult = string.Equals(job.MissedRunPolicy, "notify-only", StringComparison.OrdinalIgnoreCase)
						? "놓친 작업은 실행하지 않고 알림만 기록함 / Missed run was not executed; notification recorded"
						: "최대 지연 시간을 초과해 건너뜀 / Skipped after maximum delay";
					if (string.Equals(job.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase)) job.Enabled = false;
					else job.NextRunUtc = CalculateNextAutomationRunUtc(job, nowUtc).ToString("o", CultureInfo.InvariantCulture);
					if (string.Equals(job.MissedRunPolicy, "notify-only", StringComparison.OrdinalIgnoreCase)) missedNotifications.Add(CloneAutomationJob(job));
					changed = true;
					continue;
				}
				job.Running = true;
				job.LeaseUtc = nowUtc.ToString("o", CultureInfo.InvariantCulture);
				using (System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess())
				{
					job.LeaseProcessId = current.Id;
					job.LeaseProcessStartTicks = current.StartTime.Ticks;
				}
				if (string.Equals(job.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase)) job.Enabled = false;
				else job.NextRunUtc = CalculateNextAutomationRunUtc(job, nowUtc).ToString("o", CultureInfo.InvariantCulture);
				claims.Add(new AutomationJobClaim { ServerDirectory = Path.GetFullPath(serverDirectory), Job = CloneAutomationJob(job) });
				changed = true;
			}
			if (changed)
			{
				string path = GetAutomationConfigurationPath(serverDirectory);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				WriteJsonAtomic(path, configuration);
			}
			for (int i = 0; i < missedNotifications.Count; i++)
			{
				TryRecordOperationEvent(
					serverDirectory,
					"automation",
					"warning",
					"예약 작업 \"" + missedNotifications[i].Name + "\"의 실행 시각을 놓쳐 알림만 기록했습니다.",
					"The scheduled time for \"" + missedNotifications[i].Name + "\" was missed; notification only.",
					"automation",
					false);
			}
			return claims;
			});
		}
	}

	private static void CompleteAutomationJob(AutomationJobClaim claim, DateTime completedUtc, string result)
	{
		ServerAutomationJob completed = null;
		lock (AutomationFileLock)
		{
			WithAutomationCrossProcessLock(claim.ServerDirectory, delegate
			{
			ServerAutomationConfiguration configuration = ReadServerAutomationConfigurationUnlocked(claim.ServerDirectory);
			ServerAutomationJob match = configuration.Jobs.Find(delegate(ServerAutomationJob item) { return string.Equals(item.Id, claim.Job.Id, StringComparison.OrdinalIgnoreCase); });
			if (match == null) return 0;
			match.Running = false;
			match.LeaseUtc = null;
			match.LeaseProcessId = 0;
			match.LeaseProcessStartTicks = 0L;
			match.LastRunUtc = completedUtc.ToString("o", CultureInfo.InvariantCulture);
			match.LastResult = string.IsNullOrWhiteSpace(result) ? "완료 / Completed" : result;
			string path = GetAutomationConfigurationPath(claim.ServerDirectory);
			WriteJsonAtomic(path, configuration);
			completed = CloneAutomationJob(match);
			return 0;
			});
		}
		if (completed != null) RecordAutomationCompletionHistory(claim.ServerDirectory, completed, result);
	}

	private static void RecordAutomationCompletionHistory(string serverDirectory, ServerAutomationJob job, string result)
	{
		string value = result ?? string.Empty;
		bool failed = value.StartsWith("실패:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Failed:", StringComparison.OrdinalIgnoreCase);
		bool canceled = value.IndexOf("취소", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
		string severity = failed ? "error" : canceled ? "warning" : "info";
		string korean = failed
			? "예약 작업 \"" + job.Name + "\"에 실패했습니다. 일정 화면에서 최근 결과를 확인해 주세요."
			: canceled
				? "예약 작업 \"" + job.Name + "\"이 취소되었습니다."
				: "예약 작업 \"" + job.Name + "\"을 완료했습니다.";
		string english = failed
			? "Scheduled job \"" + job.Name + "\" failed. Review its latest result in Schedules."
			: canceled
				? "Scheduled job \"" + job.Name + "\" was cancelled."
				: "Scheduled job \"" + job.Name + "\" completed.";
		TryRecordOperationEvent(serverDirectory, "automation", severity, korean, english, "automation", false);
	}

	private static bool IsAutomationLeaseOwnerAlive(int processId, long startTicks)
	{
		try
		{
			using (System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(processId)) return !process.HasExited && process.StartTime.Ticks == startTicks;
		}
		catch { return false; }
	}

	private static ServerAutomationJob CloneAutomationJob(ServerAutomationJob source)
	{
		return new JavaScriptSerializer().Deserialize<ServerAutomationJob>(new JavaScriptSerializer().Serialize(source));
	}

	private static void PruneServerBackupsWithPolicy(string backupDirectory, int retentionCount, int retentionDays, long retentionMaximumBytes, DateTime nowUtc)
	{
		if (!Directory.Exists(backupDirectory)) return;
		retentionCount = Math.Max(1, Math.Min(200, retentionCount));
		retentionDays = Math.Max(1, Math.Min(3650, retentionDays));
		retentionMaximumBytes = Math.Max(104857600L, retentionMaximumBytes);
		FileInfo[] files = new DirectoryInfo(backupDirectory).GetFiles("server-*.zip");
		Array.Sort(files, delegate(FileInfo left, FileInfo right) { return right.LastWriteTimeUtc.CompareTo(left.LastWriteTimeUtc); });
		long keptBytes = 0L;
		for (int i = 0; i < files.Length; i++)
		{
			bool keepNewest = i == 0;
			bool exceedsCount = i >= retentionCount;
			bool exceedsAge = nowUtc - files[i].LastWriteTimeUtc > TimeSpan.FromDays(retentionDays);
			bool exceedsSize = keptBytes > retentionMaximumBytes - files[i].Length;
			if (!keepNewest && (exceedsCount || exceedsAge || exceedsSize)) files[i].Delete();
			else keptBytes = checked(keptBytes + files[i].Length);
		}
	}

	private sealed class AutomationManagerForm : Form
	{
		private readonly string serverDirectory;
		private readonly ListView jobList;
		private readonly CheckBox beforeStartBox;
		private readonly CheckBox afterStopBox;
		private readonly NumericUpDown countBox;
		private readonly NumericUpDown daysBox;
		private readonly NumericUpDown sizeBox;
		private ServerAutomationConfiguration configuration;

		public AutomationManagerForm(string directory)
		{
			serverDirectory = Path.GetFullPath(directory);
			bool korean = IsManagedKorean();
			Text = korean ? "자동 백업 및 일정" : "Backups and schedules";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(880, 600);
			Size = new Size(1040, 680);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font("Pretendard", 10.5F);
			KeyPreview = true;
			ApplyLauncherWindowIcon(this);

			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 4, ColumnCount = 1 };
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);
			Label heading = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = korean ? "서버별 자동화" : "Per-server automation", Margin = new Padding(0, 0, 0, 12) };
			root.Controls.Add(heading, 0, 0);
			jobList = new BufferedListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false };
			jobList.Columns.Add(korean ? "작업" : "Job", 180);
			jobList.Columns.Add(korean ? "종류" : "Action", 100);
			jobList.Columns.Add(korean ? "일정" : "Schedule", 150);
			jobList.Columns.Add(korean ? "다음 실행" : "Next run", 175);
			jobList.Columns.Add(korean ? "최근 결과" : "Last result", 300);
			root.Controls.Add(jobList, 0, 1);

			FlowLayoutPanel policy = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 10, 0, 4) };
			beforeStartBox = new ModernCheckBox { AutoSize = true, Text = korean ? "시작 전 백업" : "Backup before start", Margin = new Padding(0, 8, 18, 0) };
			afterStopBox = new ModernCheckBox { AutoSize = true, Text = korean ? "종료 후 백업" : "Backup after stop", Margin = new Padding(0, 8, 18, 0) };
			countBox = AddAutomationNumber(policy, korean ? "보존 개수" : "Keep count", 1, 200, 10);
			daysBox = AddAutomationNumber(policy, korean ? "보존 일수" : "Keep days", 1, 3650, 30);
			sizeBox = AddAutomationNumber(policy, korean ? "최대 용량(GB)" : "Maximum size (GB)", 1, 10240, 20);
			policy.Controls.Add(beforeStartBox);
			policy.Controls.Add(afterStopBox);
			root.Controls.Add(policy, 0, 2);

			FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 8, 0, 0) };
			Button add = MultiServerDashboardForm.NewManagedButton(korean ? "추가" : "Add", 90, "primary");
			Button edit = MultiServerDashboardForm.NewManagedButton(korean ? "편집" : "Edit", 90, "secondary");
			Button toggle = MultiServerDashboardForm.NewManagedButton(korean ? "켜기/끄기" : "Enable/disable", 122, "secondary");
			Button run = MultiServerDashboardForm.NewManagedButton(korean ? "지금 실행" : "Run now", 108, "secondary");
			Button remove = MultiServerDashboardForm.NewManagedButton(korean ? "제거" : "Remove", 90, "danger");
			Button save = MultiServerDashboardForm.NewManagedButton(korean ? "설정 저장" : "Save settings", 116, "primary");
			Button refresh = MultiServerDashboardForm.NewManagedButton(korean ? "새로고침" : "Refresh", 104, "secondary");
			add.Click += delegate { EditJob(null); };
			edit.Click += delegate { EditJob(GetSelectedJob()); };
			toggle.Click += delegate { ToggleSelectedJob(); };
			run.Click += delegate { RunSelectedNow(); };
			remove.Click += delegate { RemoveSelectedJob(); };
			save.Click += delegate { SaveConfiguration(); };
			refresh.Click += delegate { Reload(); };
			actions.Controls.AddRange(new Control[] { add, edit, toggle, run, remove, refresh, save });
			root.Controls.Add(actions, 0, 3);
			Shown += delegate { Reload(); };
			jobList.DoubleClick += delegate { EditJob(GetSelectedJob()); };
			ApplySimpleDialogTheme(this);
			ConfigureAccessibleField(jobList, korean ? "예약 작업 목록" : "Scheduled jobs", korean ? "다음 실행 시각과 최근 실행 결과를 표시합니다." : "Shows next run time and latest execution result.");
			ApplyCommonButtonToolTips(this);
		}

		private static NumericUpDown AddAutomationNumber(FlowLayoutPanel panel, string label, decimal minimum, decimal maximum, decimal value)
		{
			panel.Controls.Add(new Label { AutoSize = true, Text = label, Margin = new Padding(8, 10, 4, 0) });
			NumericUpDown box = new NumericUpDown { Minimum = minimum, Maximum = maximum, Value = value, Width = 78, Margin = new Padding(0, 5, 8, 0), AccessibleName = label };
			panel.Controls.Add(box);
			return box;
		}

		private void Reload()
		{
			try
			{
				configuration = ReadServerAutomationConfiguration(serverDirectory);
				beforeStartBox.Checked = configuration.BackupBeforeStart;
				afterStopBox.Checked = configuration.BackupAfterStop;
				countBox.Value = configuration.RetentionCount;
				daysBox.Value = configuration.RetentionDays;
				sizeBox.Value = Math.Max(sizeBox.Minimum, Math.Min(sizeBox.Maximum, configuration.RetentionMaximumBytes / 1073741824L));
				RenderJobs();
			}
			catch (Exception exception) { ShowAutomationError(exception); }
		}

		private void RenderJobs()
		{
			jobList.Items.Clear();
			if (configuration == null) return;
			for (int i = 0; i < configuration.Jobs.Count; i++)
			{
				ServerAutomationJob job = configuration.Jobs[i];
				ListViewItem item = new ListViewItem((job.Enabled ? "● " : "○ ") + job.Name) { Tag = job.Id };
				item.SubItems.Add(AutomationActionText(job.Action));
				item.SubItems.Add(FormatAutomationSchedule(job));
				item.SubItems.Add(FormatAutomationTime(job.NextRunUtc));
				item.SubItems.Add(string.IsNullOrWhiteSpace(job.LastResult) ? ManagedText("실행 기록 없음", "Never run") : job.LastResult);
				jobList.Items.Add(item);
			}
		}

		private ServerAutomationJob GetSelectedJob()
		{
			if (configuration == null || jobList.SelectedItems.Count == 0) return null;
			string id = Convert.ToString(jobList.SelectedItems[0].Tag);
			return configuration.Jobs.Find(delegate(ServerAutomationJob item) { return string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase); });
		}

		private void EditJob(ServerAutomationJob existing)
		{
			using (AutomationJobForm dialog = new AutomationJobForm(existing, configuration == null ? null : configuration.Jobs))
			{
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				if (existing == null) configuration.Jobs.Add(dialog.Job);
				else
				{
					int index = configuration.Jobs.IndexOf(existing);
					dialog.Job.LastRunUtc = existing.LastRunUtc;
					dialog.Job.LastResult = existing.LastResult;
					dialog.Job.Running = existing.Running;
					dialog.Job.LeaseUtc = existing.LeaseUtc;
					dialog.Job.LeaseProcessId = existing.LeaseProcessId;
					dialog.Job.LeaseProcessStartTicks = existing.LeaseProcessStartTicks;
					if (string.Equals(dialog.Job.ScheduleKind, existing.ScheduleKind, StringComparison.OrdinalIgnoreCase)
						&& dialog.Job.IntervalMinutes == existing.IntervalMinutes
						&& string.Equals(dialog.Job.DailyLocalTime, existing.DailyLocalTime, StringComparison.Ordinal)
						&& string.Equals(dialog.Job.Weekdays, existing.Weekdays, StringComparison.Ordinal)
						&& string.Equals(dialog.Job.OnceLocalDateTime, existing.OnceLocalDateTime, StringComparison.Ordinal)) dialog.Job.NextRunUtc = existing.NextRunUtc;
					configuration.Jobs[index] = dialog.Job;
				}
				SaveConfiguration();
			}
		}

		private void ToggleSelectedJob() { ServerAutomationJob job = GetSelectedJob(); if (job == null) return; job.Enabled = !job.Enabled; SaveConfiguration(); }
		private void RunSelectedNow() { ServerAutomationJob job = GetSelectedJob(); if (job == null) return; job.Enabled = true; job.NextRunUtc = DateTime.UtcNow.AddSeconds(-1).ToString("o", CultureInfo.InvariantCulture); SaveConfiguration(); }
		private void RemoveSelectedJob() { ServerAutomationJob job = GetSelectedJob(); if (job == null || ShowMineHarborDialog(this, ManagedText("선택한 예약 작업을 제거할까요?", "Remove the selected scheduled job?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; configuration.Jobs.Remove(job); SaveConfiguration(); }

		private void SaveConfiguration()
		{
			try
			{
				configuration.BackupBeforeStart = beforeStartBox.Checked;
				configuration.BackupAfterStop = afterStopBox.Checked;
				configuration.RetentionCount = (int)countBox.Value;
				configuration.RetentionDays = (int)daysBox.Value;
				configuration.RetentionMaximumBytes = checked((long)sizeBox.Value * 1073741824L);
				for (int i = 0; i < configuration.Jobs.Count; i++) if (string.IsNullOrEmpty(configuration.Jobs[i].NextRunUtc)) configuration.Jobs[i].NextRunUtc = CalculateNextAutomationRunUtc(configuration.Jobs[i], DateTime.UtcNow).ToString("o", CultureInfo.InvariantCulture);
				WriteServerAutomationConfiguration(serverDirectory, configuration);
				RenderJobs();
			}
			catch (Exception exception) { ShowAutomationError(exception); }
		}

		private void ShowAutomationError(Exception exception) { ShowMineHarborDialog(this, ManagedText("자동화 설정을 처리하지 못했습니다: ", "Could not process automation settings: ") + DescribeException(exception), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
	}

	private sealed class AutomationJobForm : Form
	{
		private readonly TextBox nameBox;
		private readonly ComboBox actionBox;
		private readonly ComboBox scheduleBox;
		private readonly NumericUpDown intervalBox;
		private readonly TextBox timeBox;
		private readonly CheckBox[] weekdayBoxes;
		private readonly TextBox onceBox;
		private readonly ComboBox missedPolicyBox;
		private readonly NumericUpDown maximumDelayBox;
		private readonly NumericUpDown warningBox;
		private readonly TextBox commandBox;
		private readonly InlineSuggestionController commandSuggestions;
		private readonly CheckBox enabledBox;
		private readonly Label previewLabel;
		private readonly string originalId;
		private readonly IList<ServerAutomationJob> existingJobs;
		public ServerAutomationJob Job { get; private set; }

		public AutomationJobForm(ServerAutomationJob existing)
			: this(existing, null)
		{
		}

		public AutomationJobForm(ServerAutomationJob existing, IList<ServerAutomationJob> allJobs)
		{
			existingJobs = allJobs;
			bool korean = IsManagedKorean();
			Text = korean ? "예약 작업" : "Scheduled job";
			StartPosition = FormStartPosition.CenterParent;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			ClientSize = new Size(680, 650);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font("Pretendard", 10.5F);
			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 14 };
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
			Controls.Add(root);
			nameBox = AddAutomationField(root, 0, korean ? "이름" : "Name") as TextBox;
			actionBox = new ModernComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
			actionBox.Items.AddRange(korean ? new object[] { "백업", "시작", "종료", "재시작", "명령" } : new object[] { "Backup", "Start", "Stop", "Restart", "Command" });
			AddAutomationControl(root, 1, korean ? "작업" : "Action", actionBox);
			scheduleBox = new ModernComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
			scheduleBox.Items.AddRange(korean ? new object[] { "반복 간격", "매일", "요일 선택", "특정 날짜 한 번" } : new object[] { "Interval", "Daily", "Selected weekdays", "Specific date once" });
			AddAutomationControl(root, 2, korean ? "일정 방식" : "Schedule", scheduleBox);
			intervalBox = new NumericUpDown { Dock = DockStyle.Left, Width = 150, Minimum = 1, Maximum = 525600, Value = 60 };
			AddAutomationControl(root, 3, korean ? "반복 간격(분)" : "Interval (minutes)", intervalBox);
			timeBox = AddAutomationField(root, 4, korean ? "실행 시각(HH:mm)" : "Run time (HH:mm)") as TextBox;

			FlowLayoutPanel weekdaysPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = false };
			weekdayBoxes = new CheckBox[7];
			string[] dayNames = korean ? new string[] { "월", "화", "수", "목", "금", "토", "일" } : new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
			for (int i = 0; i < weekdayBoxes.Length; i++)
			{
				weekdayBoxes[i] = new ModernCheckBox { AutoSize = true, Text = dayNames[i], Margin = new Padding(0, 6, 12, 0) };
				weekdaysPanel.Controls.Add(weekdayBoxes[i]);
			}
			AddAutomationControl(root, 5, korean ? "실행 요일" : "Weekdays", weekdaysPanel);

			onceBox = AddAutomationField(root, 6, korean ? "일회성 시각" : "One-time date") as TextBox;
			onceBox.AccessibleDescription = korean ? "yyyy-MM-dd HH:mm 형식" : "Format: yyyy-MM-dd HH:mm";
			missedPolicyBox = new ModernComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
			missedPolicyBox.Items.AddRange(korean ? new object[] { "다음 실행 때 한 번 실행", "건너뛰기", "실행하지 않고 알림" } : new object[] { "Run once when available", "Skip", "Notify without running" });
			AddAutomationControl(root, 7, korean ? "놓친 작업" : "Missed run", missedPolicyBox);
			maximumDelayBox = new NumericUpDown { Dock = DockStyle.Left, Width = 150, Minimum = 1, Maximum = 525600, Value = 1440 };
			AddAutomationControl(root, 8, korean ? "최대 지연(분)" : "Maximum delay (min)", maximumDelayBox);
			warningBox = new NumericUpDown { Dock = DockStyle.Left, Width = 150, Minimum = 0, Maximum = 3600, Value = 60 };
			AddAutomationControl(root, 9, korean ? "사전 공지(초)" : "Warning (seconds)", warningBox);
			commandBox = AddAutomationField(root, 10, korean ? "명령" : "Command") as TextBox;
			commandSuggestions = new InlineSuggestionController(
				this,
				commandBox,
				true,
				delegate(string input) { return GetManagedCommandAutoCompleteCandidates(input, new string[0]); },
				korean ? "예약 명령 자동완성" : "Scheduled command suggestions",
				korean ? "위아래 방향키로 이동하고 Tab 또는 Enter로 선택합니다." : "Use Up and Down, then Tab or Enter to select.");
			enabledBox = new ModernCheckBox { AutoSize = true, Checked = true, Text = korean ? "이 작업 사용" : "Enable this job" };
			root.Controls.Add(enabledBox, 1, 11);
			previewLabel = new Label
			{
				Dock = DockStyle.Fill,
				AutoEllipsis = true,
				TextAlign = ContentAlignment.MiddleLeft,
				Tag = "muted",
				AccessibleName = korean ? "예약 미리보기" : "Schedule preview"
			};
			root.SetColumnSpan(previewLabel, 2);
			root.Controls.Add(previewLabel, 0, 12);
			FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
			Button save = MultiServerDashboardForm.NewManagedButton(korean ? "저장" : "Save", 96, "primary");
			Button cancel = MultiServerDashboardForm.NewManagedButton(korean ? "취소" : "Cancel", 96, "secondary");
			save.Click += delegate { SaveJob(); };
			cancel.DialogResult = DialogResult.Cancel;
			actions.Controls.Add(save);
			actions.Controls.Add(cancel);
			root.Controls.Add(actions, 1, 13);
			AcceptButton = save;
			CancelButton = cancel;
			originalId = existing == null ? Guid.NewGuid().ToString("N") : existing.Id;
			if (existing != null)
			{
				nameBox.Text = existing.Name;
				actionBox.SelectedIndex = AutomationActionIndex(existing.Action);
				scheduleBox.SelectedIndex = AutomationScheduleIndex(existing.ScheduleKind);
				intervalBox.Value = Math.Max(intervalBox.Minimum, Math.Min(intervalBox.Maximum, existing.IntervalMinutes));
				timeBox.Text = existing.DailyLocalTime;
				SetSelectedWeekdays(existing.Weekdays);
				onceBox.Text = existing.OnceLocalDateTime;
				missedPolicyBox.SelectedIndex = AutomationMissedPolicyIndex(existing.MissedRunPolicy);
				maximumDelayBox.Value = Math.Max(maximumDelayBox.Minimum, Math.Min(maximumDelayBox.Maximum, existing.MaximumDelayMinutes));
				warningBox.Value = existing.WarningSeconds;
				commandBox.Text = existing.Command;
				enabledBox.Checked = existing.Enabled;
			}
			else
			{
				actionBox.SelectedIndex = 0;
				scheduleBox.SelectedIndex = 0;
				timeBox.Text = "04:00";
				weekdayBoxes[0].Checked = true;
				onceBox.Text = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
				missedPolicyBox.SelectedIndex = 0;
			}
			actionBox.SelectedIndexChanged += delegate { UpdateJobFieldState(); };
			scheduleBox.SelectedIndexChanged += delegate { UpdateJobFieldState(); };
			missedPolicyBox.SelectedIndexChanged += delegate { UpdateJobFieldState(); };
			intervalBox.ValueChanged += delegate { UpdateJobPreview(); };
			timeBox.TextChanged += delegate { UpdateJobPreview(); };
			onceBox.TextChanged += delegate { UpdateJobPreview(); };
			nameBox.TextChanged += delegate { UpdateJobPreview(); };
			for (int i = 0; i < weekdayBoxes.Length; i++) weekdayBoxes[i].CheckedChanged += delegate { UpdateJobPreview(); };
			UpdateJobFieldState();
			ApplySimpleDialogTheme(this);
			commandSuggestions.ApplyPalette(ThemePalette.Create(launcherForm != null && launcherForm.UsesDarkTheme));
			ApplyCommonButtonToolTips(this);
			FormClosed += delegate { commandSuggestions.Dispose(); };
		}

		private static Control AddAutomationField(TableLayoutPanel root, int row, string label) { TextBox box = new TextBox { Dock = DockStyle.Fill }; AddAutomationControl(root, row, label, box); return box; }
		private static void AddAutomationControl(TableLayoutPanel root, int row, string label, Control control) { control.AccessibleName = label; root.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, row); root.Controls.Add(control, 1, row); }
		private void UpdateJobFieldState()
		{
			intervalBox.Enabled = scheduleBox.SelectedIndex == 0;
			timeBox.Enabled = scheduleBox.SelectedIndex == 1 || scheduleBox.SelectedIndex == 2;
			for (int i = 0; i < weekdayBoxes.Length; i++) weekdayBoxes[i].Enabled = scheduleBox.SelectedIndex == 2;
			onceBox.Enabled = scheduleBox.SelectedIndex == 3;
			maximumDelayBox.Enabled = missedPolicyBox.SelectedIndex == 1 || missedPolicyBox.SelectedIndex == 2;
			warningBox.Enabled = actionBox.SelectedIndex == 2 || actionBox.SelectedIndex == 3;
			commandBox.Enabled = actionBox.SelectedIndex == 4;
			UpdateJobPreview();
		}
		private void SaveJob()
		{
			try
			{
				ServerAutomationJob value = BuildJobFromFields();
				ServerAutomationConfiguration validation = new ServerAutomationConfiguration(); validation.Jobs.Add(value); ValidateServerAutomationConfiguration(validation);
				if (string.Equals(value.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase)
					&& CalculateNextAutomationRunUtc(value, DateTime.UtcNow) < DateTime.UtcNow.AddMinutes(-1))
					throw new InvalidDataException(ManagedText("이미 지난 일회성 시각은 저장할 수 없습니다. 지금 실행을 사용하거나 미래 시각을 선택해 주세요.", "A past one-time date cannot be saved. Use Run now or choose a future date."));
				Job = value; DialogResult = DialogResult.OK; Close();
			}
			catch (Exception exception) { ShowMineHarborDialog(this, DescribeException(exception), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
		}

		private ServerAutomationJob BuildJobFromFields()
		{
			string[] actions = { "backup", "start", "stop", "restart", "command" };
			string[] schedules = { "interval", "daily", "weekly", "once" };
			string[] missed = { "run-once", "skip", "notify-only" };
			return new ServerAutomationJob
			{
				Id = originalId,
				Name = nameBox.Text.Trim(),
				Action = actionBox.SelectedIndex >= 0 ? actions[actionBox.SelectedIndex] : string.Empty,
				ScheduleKind = scheduleBox.SelectedIndex >= 0 ? schedules[scheduleBox.SelectedIndex] : string.Empty,
				IntervalMinutes = (int)intervalBox.Value,
				DailyLocalTime = timeBox.Text.Trim(),
				Weekdays = GetSelectedWeekdays(),
				OnceLocalDateTime = onceBox.Text.Trim(),
				MissedRunPolicy = missedPolicyBox.SelectedIndex >= 0 ? missed[missedPolicyBox.SelectedIndex] : string.Empty,
				MaximumDelayMinutes = (int)maximumDelayBox.Value,
				WarningSeconds = (int)warningBox.Value,
				Command = commandBox.Text.Trim(),
				Enabled = enabledBox.Checked
			};
		}

		private void UpdateJobPreview()
		{
			if (previewLabel == null) return;
			try
			{
				ServerAutomationJob value = BuildJobFromFields();
				ServerAutomationConfiguration validation = new ServerAutomationConfiguration();
				validation.Jobs.Add(value);
				ValidateServerAutomationConfiguration(validation);
				DateTime next = CalculateNextAutomationRunUtc(value, DateTime.UtcNow);
				int conflicts = CountAutomationConflicts(value, next);
				string risk = actionBox.SelectedIndex == 0 ? ManagedText("낮음", "Low") : actionBox.SelectedIndex == 4 ? ManagedText("주의", "Caution") : ManagedText("높음", "High");
				string offline = actionBox.SelectedIndex == 4
					? ManagedText("서버가 꺼져 있으면 실패", "fails while the server is off")
					: actionBox.SelectedIndex == 2
						? ManagedText("서버가 꺼져 있으면 건너뜀", "skips while the server is off")
						: ManagedText("현재 상태에 맞게 처리", "handles the current server state");
				previewLabel.Text = ManagedText("다음 ", "Next ") + next.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
					+ ManagedText(" · 위험도 ", " · risk ") + risk
					+ " · " + offline
					+ (conflicts > 0 ? ManagedText(" · 5분 안에 겹치는 작업 ", " · jobs within 5 minutes: ") + conflicts : string.Empty);
			}
			catch (Exception exception)
			{
				previewLabel.Text = ManagedText("미리보기: ", "Preview: ") + DescribeException(exception);
			}
			previewLabel.AccessibleDescription = previewLabel.Text;
		}

		private int CountAutomationConflicts(ServerAutomationJob value, DateTime next)
		{
			if (existingJobs == null) return 0;
			int count = 0;
			for (int i = 0; i < existingJobs.Count; i++)
			{
				ServerAutomationJob other = existingJobs[i];
				if (other == null || !other.Enabled || string.Equals(other.Id, value.Id, StringComparison.OrdinalIgnoreCase)) continue;
				try
				{
					DateTime otherNext;
					if (!DateTime.TryParse(other.NextRunUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out otherNext))
						otherNext = CalculateNextAutomationRunUtc(other, DateTime.UtcNow);
					if (Math.Abs((otherNext.ToUniversalTime() - next.ToUniversalTime()).TotalMinutes) <= 5D) count++;
				}
				catch { }
			}
			return count;
		}

		private string GetSelectedWeekdays()
		{
			DayOfWeek[] values = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
			List<string> selected = new List<string>();
			for (int i = 0; i < weekdayBoxes.Length; i++) if (weekdayBoxes[i].Checked) selected.Add(values[i].ToString());
			return string.Join(",", selected.ToArray());
		}

		private void SetSelectedWeekdays(string value)
		{
			HashSet<DayOfWeek> selected = ParseAutomationWeekdays(string.IsNullOrWhiteSpace(value) ? "Monday" : value);
			DayOfWeek[] values = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
			for (int i = 0; i < weekdayBoxes.Length; i++) weekdayBoxes[i].Checked = selected.Contains(values[i]);
		}

		private static int AutomationScheduleIndex(string value)
		{
			string[] values = { "interval", "daily", "weekly", "once" };
			for (int i = 0; i < values.Length; i++) if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase)) return i;
			return 0;
		}

		private static int AutomationMissedPolicyIndex(string value)
		{
			string[] values = { "run-once", "skip", "notify-only" };
			for (int i = 0; i < values.Length; i++) if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase)) return i;
			return 0;
		}

		private static int AutomationActionIndex(string action)
		{
			string[] actions = { "backup", "start", "stop", "restart", "command" };
			for (int i = 0; i < actions.Length; i++) if (string.Equals(actions[i], action, StringComparison.OrdinalIgnoreCase)) return i;
			return 0;
		}
	}

	private static string AutomationActionText(string action)
	{
		if (string.Equals(action, "backup", StringComparison.OrdinalIgnoreCase)) return ManagedText("백업", "Backup");
		if (string.Equals(action, "start", StringComparison.OrdinalIgnoreCase)) return ManagedText("시작", "Start");
		if (string.Equals(action, "stop", StringComparison.OrdinalIgnoreCase)) return ManagedText("종료", "Stop");
		if (string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase)) return ManagedText("재시작", "Restart");
		return ManagedText("명령", "Command");
	}

	private static string FormatAutomationSchedule(ServerAutomationJob job)
	{
		if (string.Equals(job.ScheduleKind, "daily", StringComparison.OrdinalIgnoreCase)) return ManagedText("매일 ", "Daily ") + job.DailyLocalTime;
		if (string.Equals(job.ScheduleKind, "weekly", StringComparison.OrdinalIgnoreCase)) return FormatAutomationWeekdays(job.Weekdays) + " " + job.DailyLocalTime;
		if (string.Equals(job.ScheduleKind, "once", StringComparison.OrdinalIgnoreCase)) return ManagedText("한 번 ", "Once ") + job.OnceLocalDateTime;
		return ManagedText("매 ", "Every ") + job.IntervalMinutes + ManagedText("분", " min");
	}

	private static string FormatAutomationWeekdays(string value)
	{
		HashSet<DayOfWeek> days = ParseAutomationWeekdays(value);
		DayOfWeek[] order = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
		string[] korean = { "월", "화", "수", "목", "금", "토", "일" };
		string[] english = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		List<string> selected = new List<string>();
		for (int i = 0; i < order.Length; i++) if (days.Contains(order[i])) selected.Add(IsManagedKorean() ? korean[i] : english[i]);
		return string.Join("·", selected.ToArray());
	}

	private static string FormatAutomationTime(string value)
	{
		DateTime parsed;
		return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed) ? parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture) : ManagedText("미정", "Not scheduled");
	}
}
