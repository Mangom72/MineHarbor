using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

internal static partial class Launcher
{
	private const int BackgroundAgentSchemaVersion = 1;
	private const int BackgroundAgentSettingsMaximumBytes = 65536;
	private const string BackgroundAgentMutexName = "Local\\MineHarbor.BackgroundAgent";
	private const string BackgroundAgentRunValueName = "MineHarbor Background Agent";
	private static string BackgroundAgentSettingsPathOverride = null;
	private static string BackgroundAgentPipeNameOverride = null;
	private static Action<bool, string> BackgroundAgentStartupRegistrationOverride = null;
	private static bool BackgroundAgentDisableTrayForTests = false;
	private static readonly object BackgroundAgentSettingsLock = new object();

	private sealed class BackgroundAgentSettings
	{
		public int SchemaVersion = BackgroundAgentSchemaVersion;
		public bool Enabled;
		public bool StartWithWindows;
		public bool RestartAfterCrash;
		public bool Paused;
	}

	private sealed class BackgroundAgentRequest
	{
		public string Command;
		public string Profile;
		public string Value;
	}

	private sealed class BackgroundAgentProfileState
	{
		public string Name;
		public string Status;
		public bool Running;
		public int ProcessId;
		public string StartedUtc;
	}

	private sealed class BackgroundAgentResponse
	{
		public bool Success;
		public string Message;
		public bool Paused;
		public string UpdatedUtc;
		public List<BackgroundAgentProfileState> Profiles = new List<BackgroundAgentProfileState>();
		public List<string> Lines = new List<string>();
	}

	private sealed class BackgroundAgentSession
	{
		public readonly object SyncRoot = new object();
		public ManagedProfileRecord Profile;
		public Process Process;
		public bool StopRequested;
		public bool ScheduledRestart;
		public DateTime StartedUtc;
		public string Status;
		public readonly List<string> Lines = new List<string>();
		public readonly Queue<DateTime> CrashTimes = new Queue<DateTime>();

		public void AddLine(string line)
		{
			lock (SyncRoot)
			{
				Lines.Add(line ?? string.Empty);
				if (Lines.Count > 4000) Lines.RemoveRange(0, 500);
			}
		}

		public string[] SnapshotLines()
		{
			lock (SyncRoot) return Lines.ToArray();
		}
	}

	private static string GetBackgroundAgentSettingsPath()
	{
		if (!string.IsNullOrWhiteSpace(BackgroundAgentSettingsPathOverride))
			return Path.GetFullPath(BackgroundAgentSettingsPathOverride);
		return Path.Combine(GetLauncherUserDataDirectory(), "background-agent.json");
	}

	private static BackgroundAgentSettings ReadBackgroundAgentSettings()
	{
		lock (BackgroundAgentSettingsLock)
		{
			return WithBackgroundAgentSettingsLock(ReadBackgroundAgentSettingsUnlocked);
		}
	}

	private static BackgroundAgentSettings ReadBackgroundAgentSettingsUnlocked()
	{
		string path = GetBackgroundAgentSettingsPath();
		if (!File.Exists(path)) return new BackgroundAgentSettings();
		FileInfo info = new FileInfo(path);
		if (info.Length <= 0 || info.Length > BackgroundAgentSettingsMaximumBytes)
			throw new InvalidDataException("백그라운드 에이전트 설정 파일 크기가 올바르지 않습니다.");
		BackgroundAgentSettings settings;
		try
		{
			settings = new JavaScriptSerializer().Deserialize<BackgroundAgentSettings>(File.ReadAllText(path));
		}
		catch (Exception exception)
		{
			throw new InvalidDataException("백그라운드 에이전트 설정 파일이 손상되었습니다. 원본 파일은 변경하지 않았습니다.", exception);
		}
		if (settings == null || settings.SchemaVersion != BackgroundAgentSchemaVersion)
			throw new InvalidDataException("지원하지 않는 백그라운드 에이전트 설정 버전입니다.");
		return settings;
	}

	private static void WriteBackgroundAgentSettings(BackgroundAgentSettings settings)
	{
		if (settings == null || settings.SchemaVersion != BackgroundAgentSchemaVersion)
			throw new InvalidDataException("지원하지 않는 백그라운드 에이전트 설정 버전입니다.");
		lock (BackgroundAgentSettingsLock)
		{
			WithBackgroundAgentSettingsLock(delegate
			{
				string path = GetBackgroundAgentSettingsPath();
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				WriteJsonAtomic(path, settings);
				return 0;
			});
		}
	}

	private static T WithBackgroundAgentSettingsLock<T>(Func<T> action)
	{
		string path = GetBackgroundAgentSettingsPath();
		using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()))).Replace("-", string.Empty).Substring(0, 24);
			using (Mutex mutex = new Mutex(false, "Local\\MineHarbor.BackgroundSettings." + suffix))
			{
				bool entered = false;
				try
				{
					try { entered = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
					catch (AbandonedMutexException) { entered = true; }
					if (!entered) throw new IOException("다른 MineHarbor 프로세스가 백그라운드 설정을 갱신하고 있습니다.");
					return action();
				}
				finally { if (entered) mutex.ReleaseMutex(); }
			}
		}
	}

	private static string GetBackgroundAgentPipeName()
	{
		if (!string.IsNullOrWhiteSpace(BackgroundAgentPipeNameOverride)) return BackgroundAgentPipeNameOverride;
		string sid;
		using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
			sid = identity.User == null ? Environment.UserName : identity.User.Value;
		using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(sid))).Replace("-", string.Empty).Substring(0, 20);
			return "MineHarbor.BackgroundAgent." + suffix;
		}
	}

	private static bool TryRunBackgroundAgentMode(string[] args, out int exitCode)
	{
		exitCode = 0;
		if (args == null || args.Length == 0 || !string.Equals(args[0], "--background-agent", StringComparison.OrdinalIgnoreCase))
			return false;
		BackgroundAgentSettings settings;
		try
		{
			settings = ReadBackgroundAgentSettings();
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine("[BackgroundAgent] " + exception.Message);
			exitCode = 2;
			return true;
		}
		if (!settings.Enabled)
		{
			Console.Error.WriteLine("[BackgroundAgent] 사용자가 백그라운드 운영에 동의하지 않아 시작하지 않았습니다.");
			exitCode = 3;
			return true;
		}
		bool createdNew;
		using (Mutex mutex = new Mutex(true, BackgroundAgentMutexName, out createdNew))
		{
			if (!createdNew) return true;
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				using (BackgroundAgentContext context = new BackgroundAgentContext(settings))
				{
					Application.Run(context);
				}
				return true;
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine("[BackgroundAgent] " + exception);
				exitCode = 1;
				return true;
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}
	}

	private static bool IsBackgroundAgentRunning()
	{
		BackgroundAgentResponse response = SendBackgroundAgentRequest("ping", null, null, 120);
		return response != null && response.Success;
	}

	private static bool EnsureBackgroundAgentRunning()
	{
		if (IsBackgroundAgentRunning()) return true;
		BackgroundAgentSettings settings;
		try { settings = ReadBackgroundAgentSettings(); }
		catch { return false; }
		if (!settings.Enabled) return false;
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = AssemblyLocation(),
			Arguments = "--background-agent",
			WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
			UseShellExecute = true,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};
		Process.Start(startInfo);
		for (int attempt = 0; attempt < 12; attempt++)
		{
			Thread.Sleep(100);
			if (IsBackgroundAgentRunning()) return true;
		}
		return false;
	}

	private static void StartConfiguredBackgroundAgent()
	{
		try
		{
			BackgroundAgentSettings settings = ReadBackgroundAgentSettings();
			if (settings.Enabled) EnsureBackgroundAgentRunning();
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine("[BackgroundAgent] 자동 연결 실패: " + exception.Message);
		}
	}

	private static void StopBackgroundAgentForLauncherUpdate()
	{
		if (!IsBackgroundAgentRunning()) return;
		SendBackgroundAgentRequest("shutdown", null, null, 1000);
		for (int attempt = 0; attempt < 350; attempt++)
		{
			if (!IsBackgroundAgentRunning()) return;
			Thread.Sleep(100);
		}
		throw new IOException("백그라운드 에이전트를 안전하게 종료하지 못해 런처 업데이트를 중단했습니다.");
	}

	private static BackgroundAgentResponse SendBackgroundAgentRequest(string command, string profile, string value, int timeoutMilliseconds)
	{
		if (string.IsNullOrWhiteSpace(command)) return null;
		try
		{
			using (NamedPipeClientStream client = new NamedPipeClientStream(".", GetBackgroundAgentPipeName(), PipeDirection.InOut, PipeOptions.None))
			{
				client.Connect(Math.Max(20, Math.Min(5000, timeoutMilliseconds)));
				client.ReadMode = PipeTransmissionMode.Byte;
				using (StreamReader reader = new StreamReader(client, Encoding.UTF8, false, 4096, true))
				using (StreamWriter writer = new StreamWriter(client, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
				{
					string request = new JavaScriptSerializer().Serialize(new BackgroundAgentRequest { Command = command, Profile = profile, Value = value });
					if (request.Length > 16384) throw new InvalidDataException("백그라운드 요청이 너무 큽니다.");
					int timeout = Math.Max(100, timeoutMilliseconds);
					Task writeTask = writer.WriteLineAsync(request);
					if (!writeTask.Wait(timeout)) return null;
					Task<string> readTask = reader.ReadLineAsync();
					if (!readTask.Wait(timeout)) return null;
					string line = readTask.Result;
					if (string.IsNullOrEmpty(line) || line.Length > 262144) return null;
					return new JavaScriptSerializer().Deserialize<BackgroundAgentResponse>(line);
				}
			}
		}
		catch
		{
			return null;
		}
	}

	private static void SetBackgroundAgentStartupRegistration(bool enabled, string executablePath)
	{
		if (BackgroundAgentStartupRegistrationOverride != null)
		{
			BackgroundAgentStartupRegistrationOverride(enabled, executablePath);
			return;
		}
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
		{
			if (key == null) throw new InvalidOperationException("Windows 자동 시작 설정을 열지 못했습니다.");
			if (enabled) key.SetValue(BackgroundAgentRunValueName, QuoteCommandLineArgument(Path.GetFullPath(executablePath)) + " --background-agent", RegistryValueKind.String);
			else key.DeleteValue(BackgroundAgentRunValueName, false);
		}
	}

	private sealed class BackgroundAgentContext : ApplicationContext
	{
		private readonly object sessionsLock = new object();
		private readonly Dictionary<string, BackgroundAgentSession> sessions = new Dictionary<string, BackgroundAgentSession>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, SemaphoreSlim> executionLocks = new Dictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
		private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
		private readonly NotifyIcon trayIcon;
		private readonly System.Windows.Forms.Timer pollTimer;
		private readonly Control dispatcher;
		private BackgroundAgentSettings settings;
		private bool polling;
		private bool disposed;

		public BackgroundAgentContext(BackgroundAgentSettings initialSettings)
		{
			settings = initialSettings;
			dispatcher = new Control();
			dispatcher.CreateControl();
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Opening += delegate { RebuildTrayMenu(menu); };
			trayIcon = new NotifyIcon
			{
				Visible = !BackgroundAgentDisableTrayForTests,
				Text = "MineHarbor " + ManagedText("백그라운드 운영", "background operations"),
				ContextMenuStrip = menu
			};
			try { trayIcon.Icon = Icon.ExtractAssociatedIcon(AssemblyLocation()); } catch { }
			trayIcon.DoubleClick += delegate { OpenMainWindow(); };
			pollTimer = new System.Windows.Forms.Timer { Interval = 5000 };
			pollTimer.Tick += delegate { PollSchedulesAsync(); };
			pollTimer.Start();
			SystemEvents.PowerModeChanged += HandlePowerModeChanged;
			StartPipeServer();
			PollSchedulesAsync();
		}

		private void HandlePowerModeChanged(object sender, PowerModeChangedEventArgs eventArgs)
		{
			if (eventArgs.Mode != PowerModes.Resume) return;
			PollSchedulesAsync();
			RecordForAllProfiles("background-agent", "info", "절전 모드 복귀 후 놓친 예약 작업을 확인했습니다.", "Checked missed schedules after resume.");
		}

		private void StartPipeServer()
		{
			Task.Run(async delegate
			{
				while (!cancellation.IsCancellationRequested)
				{
					NamedPipeServerStream pipe = null;
					try
					{
						pipe = CreateSecuredAgentPipe();
						await pipe.WaitForConnectionAsync(cancellation.Token).ConfigureAwait(false);
						await HandlePipeClientAsync(pipe).ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
					catch (ObjectDisposedException) { }
					catch (IOException) { }
					catch (Exception exception)
					{
						Console.Error.WriteLine("[BackgroundAgent] IPC 오류: " + exception.Message);
						Thread.Sleep(250);
					}
					finally
					{
						if (pipe != null) pipe.Dispose();
					}
				}
			});
		}

		private static NamedPipeServerStream CreateSecuredAgentPipe()
		{
			PipeSecurity security = new PipeSecurity();
			SecurityIdentifier currentUser;
			using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) currentUser = identity.User;
			if (currentUser == null) throw new InvalidOperationException("현재 Windows 사용자 SID를 확인하지 못했습니다.");
			security.SetAccessRuleProtection(true, false);
			security.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));
			return new NamedPipeServerStream(GetBackgroundAgentPipeName(), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 65536, 65536, security);
		}

		private async Task HandlePipeClientAsync(NamedPipeServerStream pipe)
		{
			using (StreamReader reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true))
			using (StreamWriter writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
			{
				Task<string> readTask = reader.ReadLineAsync();
				Task completed = await Task.WhenAny(readTask, Task.Delay(3000, cancellation.Token)).ConfigureAwait(false);
				if (completed != readTask) throw new IOException("IPC 요청 수신 시간이 초과되었습니다.");
				string line = await readTask.ConfigureAwait(false);
				BackgroundAgentResponse response;
				if (string.IsNullOrEmpty(line) || line.Length > 16384)
				{
					response = Failure("요청 크기가 올바르지 않습니다.", "The request size is invalid.");
				}
				else
				{
					try
					{
						BackgroundAgentRequest request = new JavaScriptSerializer().Deserialize<BackgroundAgentRequest>(line);
						response = HandleRequest(request);
					}
					catch (Exception exception)
					{
						response = new BackgroundAgentResponse { Success = false, Message = exception.Message };
					}
				}
				await writer.WriteLineAsync(new JavaScriptSerializer().Serialize(response)).ConfigureAwait(false);
			}
		}

		private BackgroundAgentResponse HandleRequest(BackgroundAgentRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Command)) return Failure("요청 명령이 없습니다.", "The request has no command.");
			string command = request.Command.Trim().ToLowerInvariant();
			if (command == "ping" || command == "status") return CreateStatusResponse();
			if (command == "logs") return CreateLogsResponse(request.Profile);
			if (command == "pause")
			{
				settings.Paused = true;
				WriteBackgroundAgentSettings(settings);
				return Success("백그라운드 운영을 일시 중지했습니다.", "Background operations paused.");
			}
			if (command == "resume")
			{
				settings.Paused = false;
				WriteBackgroundAgentSettings(settings);
				PollSchedulesAsync();
				return Success("백그라운드 운영을 다시 시작했습니다.", "Background operations resumed.");
			}
			if (command == "shutdown")
			{
				Task.Run(async delegate
				{
					bool stopped = await StopAllSessionsAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
					if (stopped) RequestExit();
					else RecordForAllProfiles("background-agent", "error", "서버가 제한 시간 안에 안전 종료되지 않아 에이전트 종료를 취소했습니다.", "Agent shutdown was cancelled because servers did not stop safely in time.");
				});
				return Success("백그라운드 에이전트를 종료합니다.", "The background agent is shutting down.");
			}
			if (command == "start") return StartProfile(request.Profile, false);
			if (command == "stop") return StopProfile(request.Profile, false);
			if (command == "restart") return StopProfile(request.Profile, true);
			if (command == "command") return SendProfileCommand(request.Profile, request.Value);
			if (command == "backup")
			{
				ObserveImmediateBackupAsync(request.Profile);
				return Success("백업을 시작했습니다.", "Backup started.");
			}
			return Failure("지원하지 않는 백그라운드 명령입니다.", "Unsupported background command.");
		}

		private void PollSchedulesAsync()
		{
			if (polling || cancellation.IsCancellationRequested) return;
			polling = true;
			Task.Run(async delegate
			{
				try
				{
					settings = ReadBackgroundAgentSettings();
					if (!settings.Enabled) { RequestExit(); return; }
					if (settings.Paused) return;
					string root = GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory);
					List<ManagedProfileRecord> profiles = ReadManagedProfiles(root);
					for (int i = 0; i < profiles.Count; i++)
					{
						// 다른 MineHarbor 창이나 사용자가 직접 실행한 서버의 예약은 소유권을 빼앗지 않습니다.
						if (GetRunningSession(profiles[i].Name) == null && IsLocalTcpPortListening(profiles[i].Port)) continue;
						List<AutomationJobClaim> claims = ClaimDueAutomationJobs(profiles[i].Directory, DateTime.UtcNow);
						for (int claimIndex = 0; claimIndex < claims.Count; claimIndex++)
							await ExecuteAutomationClaimAsync(profiles[i], claims[claimIndex]).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException) { }
				catch (Exception exception)
				{
					Console.Error.WriteLine("[BackgroundAgent] 예약 검사 실패: " + exception.Message);
				}
				finally { polling = false; }
			});
		}

		private async Task ExecuteAutomationClaimAsync(ManagedProfileRecord profile, AutomationJobClaim claim)
		{
			SemaphoreSlim gate;
			lock (executionLocks)
			{
				if (!executionLocks.TryGetValue(profile.Directory, out gate))
				{
					gate = new SemaphoreSlim(1, 1);
					executionLocks[profile.Directory] = gate;
				}
			}
			string result = ManagedText("취소됨", "Cancelled");
			await gate.WaitAsync(cancellation.Token).ConfigureAwait(false);
			try
			{
				ServerAutomationConfiguration configuration = ReadServerAutomationConfiguration(profile.Directory);
				if (string.Equals(claim.Job.Action, "backup", StringComparison.OrdinalIgnoreCase))
				{
					BackgroundAgentSession session = GetRunningSession(profile.Name);
					bool pausedSaves = session != null;
					try
					{
						if (pausedSaves)
						{
							SendSessionCommand(session, "save-off");
							SendSessionCommand(session, "save-all flush");
							await Task.Delay(1000, cancellation.Token).ConfigureAwait(false);
						}
						else if (IsLocalTcpPortListening(profile.Port))
						{
							throw new InvalidOperationException("에이전트가 소유하지 않은 실행 중 서버는 안전하게 백업할 수 없습니다.");
						}
						string path = await CreateAgentBackupAsync(profile.Directory, configuration, "scheduled", cancellation.Token).ConfigureAwait(false);
						result = ManagedText("백업 완료: ", "Backup completed: ") + Path.GetFileName(path);
					}
					finally { if (pausedSaves && IsSessionRunning(session)) SendSessionCommand(session, "save-on"); }
				}
				else if (string.Equals(claim.Job.Action, "start", StringComparison.OrdinalIgnoreCase))
				{
					if (configuration.BackupBeforeStart)
						await CreateAgentBackupAsync(profile.Directory, configuration, "before-start", cancellation.Token).ConfigureAwait(false);
					BackgroundAgentResponse response = StartProfile(profile.Name, false);
					result = response.Message;
				}
				else if (string.Equals(claim.Job.Action, "stop", StringComparison.OrdinalIgnoreCase))
				{
					BackgroundAgentSession session = GetRunningSession(profile.Name);
					if (session == null) result = ManagedText("이미 중지됨", "Already stopped");
					else
					{
						await WarnSessionAsync(session, claim.Job.WarningSeconds, ManagedText("서버가 곧 종료됩니다.", "Server will stop soon."), cancellation.Token).ConfigureAwait(false);
						result = StopProfile(profile.Name, false).Message;
					}
				}
				else if (string.Equals(claim.Job.Action, "restart", StringComparison.OrdinalIgnoreCase))
				{
					BackgroundAgentSession session = GetRunningSession(profile.Name);
					if (session == null)
					{
						if (configuration.BackupBeforeStart)
							await CreateAgentBackupAsync(profile.Directory, configuration, "before-start", cancellation.Token).ConfigureAwait(false);
						result = StartProfile(profile.Name, false).Message;
					}
					else
					{
						await WarnSessionAsync(session, claim.Job.WarningSeconds, ManagedText("서버가 곧 재시작됩니다.", "Server will restart soon."), cancellation.Token).ConfigureAwait(false);
						result = StopProfile(profile.Name, true).Message;
					}
				}
				else
				{
					ValidateScheduledCommand(claim.Job.Command);
					BackgroundAgentResponse response = SendProfileCommand(profile.Name, claim.Job.Command);
					if (!response.Success) throw new InvalidOperationException(response.Message);
					result = response.Message;
				}
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception exception) { result = ManagedText("실패: ", "Failed: ") + exception.Message; }
			finally
			{
				gate.Release();
				CompleteAutomationJob(claim, DateTime.UtcNow, result);
			}
		}

		private static async Task WarnSessionAsync(BackgroundAgentSession session, int seconds, string message, CancellationToken token)
		{
			if (seconds <= 0) return;
			SendSessionCommand(session, "say " + message + " " + seconds.ToString(CultureInfo.InvariantCulture) + "s");
			await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
		}

		private static Task<string> CreateAgentBackupAsync(string serverDirectory, ServerAutomationConfiguration configuration, string reason, CancellationToken token)
		{
			return Task.Run(delegate
			{
				token.ThrowIfCancellationRequested();
				string path = CreateComprehensiveServerBackup(serverDirectory, configuration.RetentionCount, reason);
				PruneServerBackupsWithPolicy(GetServerBackupDirectory(serverDirectory), configuration.RetentionCount, configuration.RetentionDays, configuration.RetentionMaximumBytes, DateTime.UtcNow);
				return path;
			}, token);
		}

		private void ObserveImmediateBackupAsync(string profileName)
		{
			Task.Run(async delegate
			{
				ManagedProfileRecord profile = FindProfile(profileName);
				if (profile == null) return;
				BackgroundAgentSession session = null;
				try
				{
					ServerAutomationConfiguration configuration = ReadServerAutomationConfiguration(profile.Directory);
					session = GetRunningSession(profile.Name);
					if (session != null)
					{
						SendSessionCommand(session, "save-off");
						SendSessionCommand(session, "save-all flush");
						await Task.Delay(1000, cancellation.Token).ConfigureAwait(false);
					}
					else if (IsLocalTcpPortListening(profile.Port)) throw new InvalidOperationException("에이전트가 소유하지 않은 실행 중 서버는 안전하게 백업할 수 없습니다.");
					await CreateAgentBackupAsync(profile.Directory, configuration, "manual-agent", cancellation.Token).ConfigureAwait(false);
					TryRecordOperationEvent(profile.Directory, "backup", "info", "백그라운드 에이전트 백업을 완료했습니다.", "Background agent backup completed.", "background-agent", false);
				}
				catch (Exception exception)
				{
					TryRecordOperationEvent(profile.Directory, "backup", "error", "백그라운드 에이전트 백업에 실패했습니다: " + exception.Message, "Background agent backup failed: " + exception.Message, "background-agent", false);
				}
				finally
				{
					if (session != null && IsSessionRunning(session))
					{
						try { SendSessionCommand(session, "save-on"); } catch { }
					}
				}
			});
		}

		private BackgroundAgentResponse StartProfile(string profileName, bool automaticRestart)
		{
			ManagedProfileRecord profile = FindProfile(profileName);
			if (profile == null) return Failure("서버 프로필을 찾지 못했습니다.", "Server profile not found.");
			if (GetRunningSession(profile.Name) != null) return Success("이미 실행 중입니다.", "Already running.");
			if (!File.Exists(Path.Combine(profile.Directory, ".launcher-properties-configured")))
				return Failure("서버 프로필 설정을 먼저 완료해 주세요.", "Complete the server profile setup first.");
			if (!EulaIsAccepted(Path.Combine(profile.Directory, "eula.txt")))
				return Failure("GUI에서 Minecraft EULA에 먼저 동의해 주세요.", "Accept the Minecraft EULA in the GUI first.");
			if (IsLocalTcpPortListening(profile.Port))
				return Failure("포트를 다른 프로세스가 사용 중입니다.", "The port is used by another process.");
			int usedMemory = 0;
			lock (sessionsLock)
			{
				foreach (BackgroundAgentSession runningSession in sessions.Values)
					if (IsSessionRunning(runningSession) && runningSession.Profile != null) usedMemory += Math.Max(0, runningSession.Profile.MemoryGb);
			}
			if (checked(usedMemory + profile.MemoryGb) > GetSafeMemoryMaximumGb())
				return Failure("동시에 실행할 서버의 메모리 합계가 안전 상한을 넘습니다.", "Combined server memory exceeds the safe limit.");

			BackgroundAgentSession session;
			lock (sessionsLock)
			{
				if (!sessions.TryGetValue(profile.Name, out session)) session = new BackgroundAgentSession();
				session.Profile = profile;
				session.Status = automaticRestart ? ManagedText("재시작 중", "Restarting") : ManagedText("시작 중", "Starting");
				session.StartedUtc = DateTime.UtcNow;
				session.StopRequested = false;
				session.ScheduledRestart = false;
				sessions[profile.Name] = session;
			}
			try
			{
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = AssemblyLocation(),
					WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};
				using (Process current = Process.GetCurrentProcess())
				{
					startInfo.Arguments = "--managed-profile " + QuoteCommandLineArgument(profile.Name) + " --parent-pid " + current.Id + " --parent-start " + current.StartTime.Ticks;
				}
				Process process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
				process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs) { if (eventArgs.Data != null) session.AddLine(eventArgs.Data); };
				process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs) { if (eventArgs.Data != null) session.AddLine(eventArgs.Data); };
				process.Exited += delegate { HandleSessionExit(session); };
				if (!process.Start()) throw new InvalidOperationException("관리 서버 프로세스를 시작하지 못했습니다.");
				session.Process = process;
				session.Status = ManagedText("실행 중", "Running");
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				TryRecordOperationEvent(profile.Directory, "server", "info", "백그라운드 에이전트가 서버를 시작했습니다.", "The background agent started the server.", "background-agent", false);
				return Success("서버 시작을 요청했습니다.", "Server start requested.");
			}
			catch (Exception exception)
			{
				lock (sessionsLock) sessions.Remove(profile.Name);
				TryRecordOperationEvent(profile.Directory, "server", "error", "백그라운드 에이전트가 서버를 시작하지 못했습니다.", "The background agent could not start the server.", "background-agent", false);
				return new BackgroundAgentResponse { Success = false, Message = exception.Message };
			}
		}

		private BackgroundAgentResponse StopProfile(string profileName, bool restart)
		{
			BackgroundAgentSession session = GetRunningSession(profileName);
			if (session == null) return Success("이미 중지되어 있습니다.", "Already stopped.");
			lock (session.SyncRoot)
			{
				session.StopRequested = true;
				session.ScheduledRestart = restart;
				session.Status = restart ? ManagedText("재시작 중", "Restarting") : ManagedText("안전 종료 중", "Stopping safely");
			}
			try
			{
				SendSessionCommand(session, "stop");
				return Success(restart ? "재시작을 요청했습니다." : "안전 종료를 요청했습니다.", restart ? "Restart requested." : "Safe stop requested.");
			}
			catch (Exception exception) { return new BackgroundAgentResponse { Success = false, Message = exception.Message }; }
		}

		private BackgroundAgentResponse SendProfileCommand(string profileName, string command)
		{
			try
			{
				ValidateScheduledCommand(command);
				BackgroundAgentSession session = GetRunningSession(profileName);
				if (session == null) return Failure("서버가 실행 중이 아닙니다.", "The server is not running.");
				SendSessionCommand(session, command);
				return Success("명령을 전송했습니다.", "Command sent.");
			}
			catch (Exception exception) { return new BackgroundAgentResponse { Success = false, Message = exception.Message }; }
		}

		private static void SendSessionCommand(BackgroundAgentSession session, string command)
		{
			if (!IsSessionRunning(session)) throw new InvalidOperationException("서버가 실행 중이 아닙니다.");
			lock (session.SyncRoot)
			{
				session.Process.StandardInput.WriteLine(command);
				session.Process.StandardInput.Flush();
			}
		}

		private void HandleSessionExit(BackgroundAgentSession session)
		{
			int exitCode = -1;
			try { exitCode = session.Process.ExitCode; } catch { }
			bool restart;
			lock (session.SyncRoot)
			{
				restart = session.ScheduledRestart || (!session.StopRequested && exitCode != 0 && settings.RestartAfterCrash);
				session.ScheduledRestart = false;
				session.Status = session.StopRequested || exitCode == 0 ? ManagedText("꺼짐", "Stopped") : ManagedText("충돌", "Crashed");
				if (restart)
				{
					DateTime now = DateTime.UtcNow;
					while (session.CrashTimes.Count > 0 && now - session.CrashTimes.Peek() > TimeSpan.FromMinutes(10)) session.CrashTimes.Dequeue();
					session.CrashTimes.Enqueue(now);
					if (session.CrashTimes.Count >= 3)
					{
						restart = false;
						session.Status = ManagedText("반복 충돌로 중단", "Stopped after crash loop");
					}
				}
			}
			TryRecordOperationEvent(session.Profile.Directory, "server", restart ? "warning" : exitCode == 0 || session.StopRequested ? "info" : "error",
				restart ? "백그라운드 에이전트가 서버 재시작을 준비합니다." : "백그라운드 에이전트가 관리하던 서버가 종료되었습니다.",
				restart ? "The background agent is preparing a server restart." : "A server managed by the background agent stopped.",
				"background-agent", false);
			if (!restart && session.StopRequested)
			{
				Task.Run(async delegate
				{
					try
					{
						ServerAutomationConfiguration configuration = ReadServerAutomationConfiguration(session.Profile.Directory);
						if (configuration.BackupAfterStop)
							await CreateAgentBackupAsync(session.Profile.Directory, configuration, "after-stop", cancellation.Token).ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
					catch (Exception exception)
					{
						TryRecordOperationEvent(session.Profile.Directory, "backup", "error", "종료 후 백업에 실패했습니다: " + exception.Message, "Post-stop backup failed: " + exception.Message, "background-agent", false);
					}
				});
			}
			if (restart)
			{
				Task.Run(async delegate
				{
					try
					{
						await Task.Delay(5000, cancellation.Token).ConfigureAwait(false);
						StartProfile(session.Profile.Name, true);
					}
					catch (OperationCanceledException) { }
				});
			}
		}

		private BackgroundAgentSession GetRunningSession(string profileName)
		{
			if (string.IsNullOrWhiteSpace(profileName)) return null;
			lock (sessionsLock)
			{
				BackgroundAgentSession session;
				if (!sessions.TryGetValue(profileName, out session) || !IsSessionRunning(session)) return null;
				return session;
			}
		}

		private static bool IsSessionRunning(BackgroundAgentSession session)
		{
			if (session == null || session.Process == null) return false;
			try { return !session.Process.HasExited; } catch { return false; }
		}

		private ManagedProfileRecord FindProfile(string profileName)
		{
			if (!IsValidProfileName(profileName)) return null;
			List<ManagedProfileRecord> profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory));
			return profiles.Find(delegate(ManagedProfileRecord item) { return string.Equals(item.Name, profileName, StringComparison.OrdinalIgnoreCase); });
		}

		private BackgroundAgentResponse CreateStatusResponse()
		{
			BackgroundAgentResponse response = Success("백그라운드 에이전트가 실행 중입니다.", "The background agent is running.");
			response.Paused = settings.Paused;
			response.UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
			List<ManagedProfileRecord> profiles;
			try { profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory)); }
			catch { profiles = new List<ManagedProfileRecord>(); }
			for (int i = 0; i < profiles.Count; i++)
			{
				BackgroundAgentSession session = GetRunningSession(profiles[i].Name);
				bool external = session == null && IsLocalTcpPortListening(profiles[i].Port);
				response.Profiles.Add(new BackgroundAgentProfileState
				{
					Name = profiles[i].Name,
					Running = session != null || external,
					ProcessId = session == null || session.Process == null ? 0 : session.Process.Id,
					Status = session != null ? session.Status : external ? ManagedText("다른 프로세스에서 실행 중", "Running in another process") : ManagedText("꺼짐", "Stopped"),
					StartedUtc = session == null ? string.Empty : session.StartedUtc.ToString("o", CultureInfo.InvariantCulture)
				});
			}
			return response;
		}

		private BackgroundAgentResponse CreateLogsResponse(string profileName)
		{
			BackgroundAgentSession session = GetRunningSession(profileName);
			if (session == null) return Failure("에이전트가 관리하는 실행 중 서버가 아닙니다.", "The server is not running under the agent.");
			BackgroundAgentResponse response = Success("콘솔 로그를 불러왔습니다.", "Console logs loaded.");
			string[] lines = session.SnapshotLines();
			int start = Math.Max(0, lines.Length - 1500);
			for (int i = start; i < lines.Length; i++) response.Lines.Add(lines[i]);
			return response;
		}

		private static BackgroundAgentResponse Success(string korean, string english)
		{
			return new BackgroundAgentResponse { Success = true, Message = ManagedText(korean, english) };
		}

		private static BackgroundAgentResponse Failure(string korean, string english)
		{
			return new BackgroundAgentResponse { Success = false, Message = ManagedText(korean, english) };
		}

		private void RebuildTrayMenu(ContextMenuStrip menu)
		{
			menu.Items.Clear();
			menu.Items.Add(ManagedText("MineHarbor 열기", "Open MineHarbor"), null, delegate { OpenMainWindow(); });
			menu.Items.Add(new ToolStripSeparator());
			List<ManagedProfileRecord> profiles;
			try { profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory)); }
			catch { profiles = new List<ManagedProfileRecord>(); }
			for (int i = 0; i < profiles.Count; i++)
			{
				ManagedProfileRecord profile = profiles[i];
				ToolStripMenuItem profileMenu = new ToolStripMenuItem(profile.Name);
				bool running = GetRunningSession(profile.Name) != null;
				bool external = !running && IsLocalTcpPortListening(profile.Port);
				profileMenu.DropDownItems.Add(running ? ManagedText("실행 중", "Running") : external ? ManagedText("다른 프로세스에서 실행 중", "Running in another process") : ManagedText("꺼짐", "Stopped")).Enabled = false;
				if (!running && !external) profileMenu.DropDownItems.Add(ManagedText("시작", "Start"), null, delegate { ShowTrayResult(StartProfile(profile.Name, false)); });
				else
				{
					if (running)
					{
						profileMenu.DropDownItems.Add(ManagedText("안전 종료", "Stop safely"), null, delegate { ShowTrayResult(StopProfile(profile.Name, false)); });
						profileMenu.DropDownItems.Add(ManagedText("재시작", "Restart"), null, delegate { ShowTrayResult(StopProfile(profile.Name, true)); });
						profileMenu.DropDownItems.Add(ManagedText("콘솔 열기", "Open console"), null, delegate { new BackgroundAgentConsoleForm(profile.Name).Show(); });
					}
				}
				ToolStripItem backupItem = profileMenu.DropDownItems.Add(ManagedText("즉시 백업", "Back up now"), null, delegate { ObserveImmediateBackupAsync(profile.Name); });
				backupItem.Enabled = !external;
				menu.Items.Add(profileMenu);
			}
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(settings.Paused ? ManagedText("백그라운드 운영 재개", "Resume background operations") : ManagedText("백그라운드 운영 일시 중지", "Pause background operations"), null,
				delegate { HandleRequest(new BackgroundAgentRequest { Command = settings.Paused ? "resume" : "pause" }); });
			menu.Items.Add(ManagedText("모든 서버 안전 종료", "Stop all servers safely"), null, async delegate
			{
				bool stopped = await StopAllSessionsAsync(TimeSpan.FromSeconds(30));
				if (!stopped) trayIcon.ShowBalloonTip(3000, "MineHarbor", ManagedText("일부 서버가 제한 시간 안에 종료되지 않았습니다.", "Some servers did not stop in time."), ToolTipIcon.Warning);
			});
			menu.Items.Add(ManagedText("MineHarbor 완전히 종료", "Exit MineHarbor completely"), null, async delegate
			{
				bool stopped = await StopAllSessionsAsync(TimeSpan.FromSeconds(30));
				if (stopped) RequestExit();
				else trayIcon.ShowBalloonTip(3000, "MineHarbor", ManagedText("서버 보호를 위해 에이전트 종료를 취소했습니다.", "Agent exit was cancelled to protect running servers."), ToolTipIcon.Warning);
			});
		}

		private void ShowTrayResult(BackgroundAgentResponse response)
		{
			if (response == null) return;
			trayIcon.ShowBalloonTip(2500, "MineHarbor", response.Message, response.Success ? ToolTipIcon.Info : ToolTipIcon.Warning);
		}

		private static void OpenMainWindow()
		{
			try
			{
				Process.Start(new ProcessStartInfo { FileName = AssemblyLocation(), WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory, UseShellExecute = true });
			}
			catch { }
		}

		private async Task<bool> StopAllSessionsAsync(TimeSpan timeout)
		{
			List<BackgroundAgentSession> snapshot;
			lock (sessionsLock) snapshot = new List<BackgroundAgentSession>(sessions.Values);
			for (int i = 0; i < snapshot.Count; i++)
			{
				if (!IsSessionRunning(snapshot[i])) continue;
				snapshot[i].StopRequested = true;
				try { SendSessionCommand(snapshot[i], "stop"); } catch { }
			}
			DateTime deadline = DateTime.UtcNow.Add(timeout);
			while (DateTime.UtcNow < deadline)
			{
				bool anyRunning = false;
				for (int i = 0; i < snapshot.Count; i++) if (IsSessionRunning(snapshot[i])) { anyRunning = true; break; }
				if (!anyRunning) return true;
				await Task.Delay(250).ConfigureAwait(false);
			}
			return false;
		}

		private void RecordForAllProfiles(string category, string severity, string korean, string english)
		{
			try
			{
				List<ManagedProfileRecord> profiles = ReadManagedProfiles(GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory));
				for (int i = 0; i < profiles.Count; i++) TryRecordOperationEvent(profiles[i].Directory, category, severity, korean, english, "background-agent", false);
			}
			catch { }
		}

		private void RequestExit()
		{
			if (disposed) return;
			try
			{
				if (dispatcher.InvokeRequired) dispatcher.BeginInvoke((MethodInvoker)delegate { ExitThread(); });
				else ExitThread();
			}
			catch { }
		}

		protected override void ExitThreadCore()
		{
			DisposeAgentResources();
			base.ExitThreadCore();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) DisposeAgentResources();
			base.Dispose(disposing);
		}

		private void DisposeAgentResources()
		{
			if (disposed) return;
			disposed = true;
			SystemEvents.PowerModeChanged -= HandlePowerModeChanged;
			cancellation.Cancel();
			pollTimer.Stop();
			pollTimer.Dispose();
			trayIcon.Visible = false;
			if (trayIcon.ContextMenuStrip != null) trayIcon.ContextMenuStrip.Dispose();
			trayIcon.Dispose();
			dispatcher.Dispose();
			cancellation.Dispose();
		}
	}

	private sealed class BackgroundAgentSettingsForm : Form
	{
		private readonly ModernCheckBox enabledBox;
		private readonly ModernCheckBox startupBox;
		private readonly ModernCheckBox restartBox;
		private readonly Label statusLabel;

		public BackgroundAgentSettingsForm()
		{
			bool korean = IsManagedKorean();
			Text = korean ? "백그라운드 운영 (베타)" : "Background operations (Beta)";
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(620, 430);
			Size = new Size(700, 480);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			ApplyLauncherWindowIcon(this);
			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			Controls.Add(root);
			root.Controls.Add(new Label { AutoSize = true, Font = new Font(ThemeFonts.Display, 18F, FontStyle.Bold), Text = korean ? "창을 닫아도 예약 작업을 계속합니다" : "Keep schedules running after the window closes", Margin = new Padding(0, 0, 0, 12) }, 0, 0);
			root.Controls.Add(new Label
			{
				AutoSize = true,
				MaximumSize = new Size(620, 0),
				Text = korean
					? "사용자 계정용 트레이 에이전트가 예약 백업·시작·종료·재시작·명령을 처리합니다. 에이전트가 시작한 서버만 창을 닫은 뒤에도 계속 실행됩니다."
					: "A per-user tray agent handles scheduled backups, starts, stops, restarts, and commands. Only servers started by the agent keep running after the window closes.",
				Margin = new Padding(0, 0, 0, 16)
			}, 0, 1);
			BackgroundAgentSettings settings;
			try { settings = ReadBackgroundAgentSettings(); } catch { settings = new BackgroundAgentSettings(); }
			enabledBox = new ModernCheckBox { AutoSize = true, Text = korean ? "백그라운드 운영 사용 (베타)" : "Enable background operations (Beta)", Checked = settings.Enabled, Margin = new Padding(0, 4, 0, 4) };
			startupBox = new ModernCheckBox { AutoSize = true, Text = korean ? "Windows 로그인 시 자동 시작" : "Start at Windows sign-in", Checked = settings.StartWithWindows, Margin = new Padding(0, 4, 0, 4) };
			restartBox = new ModernCheckBox { AutoSize = true, Text = korean ? "충돌한 에이전트 서버 자동 재시작" : "Restart crashed agent servers", Checked = settings.RestartAfterCrash, Margin = new Padding(0, 4, 0, 4) };
			root.Controls.Add(enabledBox, 0, 2);
			root.Controls.Add(startupBox, 0, 3);
			root.Controls.Add(restartBox, 0, 4);
			statusLabel = new Label { AutoSize = true, Text = IsBackgroundAgentRunning() ? (korean ? "현재 상태: 실행 중" : "Current status: Running") : (korean ? "현재 상태: 중지됨" : "Current status: Stopped"), Margin = new Padding(0, 12, 0, 8) };
			root.Controls.Add(statusLabel, 0, 5);
			FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
			Button save = MultiServerDashboardForm.NewManagedButton(korean ? "저장" : "Save", 100, "primary");
			Button cancel = MultiServerDashboardForm.NewManagedButton(korean ? "취소" : "Cancel", 100, "secondary");
			save.Click += delegate { SaveSettings(); };
			cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			buttons.Controls.Add(save);
			buttons.Controls.Add(cancel);
			root.Controls.Add(buttons, 0, 6);
			ConfigureAccessibleField(enabledBox, enabledBox.Text, korean ? "명시적으로 동의한 경우에만 사용자 계정용 에이전트를 실행합니다." : "Runs the per-user agent only after explicit consent.");
			ApplySimpleDialogTheme(this);
			ApplyCommonButtonToolTips(this);
		}

		private void SaveSettings()
		{
			bool korean = IsManagedKorean();
			if (startupBox.Checked && !enabledBox.Checked)
			{
				ShowMineHarborDialog(this, korean ? "자동 시작을 사용하려면 백그라운드 운영도 켜야 합니다." : "Enable background operations before enabling automatic start.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			BackgroundAgentSettings previous;
			try { previous = ReadBackgroundAgentSettings(); } catch { previous = new BackgroundAgentSettings(); }
			BackgroundAgentSettings settings = new BackgroundAgentSettings
			{
				Enabled = enabledBox.Checked,
				StartWithWindows = startupBox.Checked,
				RestartAfterCrash = restartBox.Checked,
				Paused = false
			};
			try
			{
				WriteBackgroundAgentSettings(settings);
				SetBackgroundAgentStartupRegistration(settings.Enabled && settings.StartWithWindows, AssemblyLocation());
				if (settings.Enabled)
				{
					if (!EnsureBackgroundAgentRunning()) throw new InvalidOperationException(korean ? "백그라운드 에이전트를 시작하지 못했습니다." : "Could not start the background agent.");
				}
				else SendBackgroundAgentRequest("shutdown", null, null, 1000);
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception exception)
			{
				try
				{
					WriteBackgroundAgentSettings(previous);
					SetBackgroundAgentStartupRegistration(previous.Enabled && previous.StartWithWindows, AssemblyLocation());
				}
				catch { }
				ShowMineHarborDialog(this, (korean ? "설정을 저장하지 못했습니다: " : "Could not save settings: ") + exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	private sealed class BackgroundAgentConsoleForm : Form
	{
		private readonly string profileName;
		private readonly RichTextBox outputBox;
		private readonly TextBox commandBox;
		private readonly Button sendButton;
		private readonly InlineSuggestionController commandSuggestions;
		private readonly Label statusLabel;
		private readonly System.Windows.Forms.Timer refreshTimer;
		private bool refreshing;

		public BackgroundAgentConsoleForm(string managedProfileName)
		{
			profileName = managedProfileName;
			bool korean = IsManagedKorean();
			Text = profileName + " · " + (korean ? "백그라운드 콘솔" : "Background console");
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(760, 520);
			Size = new Size(920, 680);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font(ThemeFonts.Body, 10.5F);
			ApplyLauncherWindowIcon(this);

			TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 };
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
			Controls.Add(root);
			outputBox = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BorderStyle = BorderStyle.None,
				Font = new Font("Consolas", 9.5F),
				AccessibleName = korean ? "백그라운드 서버 콘솔 로그" : "Background server console log"
			};
			root.Controls.Add(outputBox, 0, 0);
			statusLabel = new Label { AutoSize = true, Text = korean ? "로그를 불러오는 중…" : "Loading logs…", Margin = new Padding(0, 8, 0, 6) };
			root.Controls.Add(statusLabel, 0, 1);
			TableLayoutPanel commandRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
			commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			commandBox = new ModernTextBox { Dock = DockStyle.Fill };
			((ModernTextBox)commandBox).CueText = korean ? "서버 명령 입력 — Tab으로 자동완성" : "Enter a server command — Tab to complete";
			commandSuggestions = new InlineSuggestionController(
				this,
				commandBox,
				true,
				delegate(string input) { return GetManagedCommandAutoCompleteCandidates(input, new string[0]); },
				korean ? "백그라운드 서버 명령 자동완성" : "Background server command suggestions",
				korean ? "위아래 방향키로 이동하고 Tab 또는 Enter로 선택합니다." : "Use Up and Down, then Tab or Enter to select.");
			sendButton = MultiServerDashboardForm.NewManagedButton(korean ? "전송" : "Send", 96, "primary");
			sendButton.Margin = new Padding(10, 0, 0, 0);
			sendButton.Click += delegate { SendCommand(); };
			commandBox.KeyDown += delegate(object sender, KeyEventArgs eventArgs)
			{
				if (eventArgs.Handled || eventArgs.SuppressKeyPress) return;
				if (eventArgs.KeyCode != Keys.Enter || eventArgs.Shift) return;
				eventArgs.SuppressKeyPress = true;
				SendCommand();
			};
			commandRow.Controls.Add(CreateModernTextBoxSurface(commandBox, 8), 0, 0);
			commandRow.Controls.Add(sendButton, 1, 0);
			root.Controls.Add(commandRow, 0, 2);
			ConfigureAccessibleField(commandBox, korean ? "백그라운드 서버 명령" : "Background server command", korean ? "명령은 전송 버튼을 누른 경우에만 실행됩니다." : "The command runs only after you press Send.");
			refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
			refreshTimer.Tick += delegate { RefreshLogsAsync(); };
			Shown += delegate { refreshTimer.Start(); RefreshLogsAsync(); commandBox.Focus(); };
			FormClosed += delegate { refreshTimer.Stop(); refreshTimer.Dispose(); commandSuggestions.Dispose(); };
			ApplySimpleDialogTheme(this);
			ApplyCommonButtonToolTips(this);
		}

		private async void RefreshLogsAsync()
		{
			if (refreshing || IsDisposed) return;
			refreshing = true;
			try
			{
				BackgroundAgentResponse response = await Task.Run(delegate { return SendBackgroundAgentRequest("logs", profileName, null, 1200); });
				if (IsDisposed) return;
				if (response == null || !response.Success)
				{
					statusLabel.Text = response == null ? ManagedText("에이전트 연결이 끊겼습니다.", "Agent connection lost.") : response.Message;
					sendButton.Enabled = false;
					return;
				}
				string text = string.Join(Environment.NewLine, response.Lines ?? new List<string>());
				if (!string.Equals(outputBox.Text, text, StringComparison.Ordinal))
				{
					outputBox.Text = text;
					outputBox.SelectionStart = outputBox.TextLength;
					outputBox.ScrollToCaret();
				}
				sendButton.Enabled = true;
				statusLabel.Text = ManagedText("백그라운드 에이전트 연결됨", "Connected to the background agent");
			}
			finally { refreshing = false; }
		}

		private void SendCommand()
		{
			string command = NormalizeCommandForSend(commandBox.Text);
			if (command.Length == 0) return;
			QuickCommandRisk risk = GetQuickCommandRisk(command, GetBuiltInQuickCommands());
			if (risk != QuickCommandRisk.Normal)
			{
				bool dangerous = risk == QuickCommandRisk.Dangerous;
				string question = dangerous
					? ManagedText("위험 명령입니다. 실제 전송할 명령을 다시 확인한 뒤 실행하시겠습니까?\r\n\r\n", "This is a dangerous command. Review the exact command before running it.\r\n\r\n")
					: ManagedText("다음 명령은 서버 상태를 변경합니다. 실행하시겠습니까?\r\n\r\n", "This command changes server state. Run it?\r\n\r\n");
				if (ShowMineHarborDialog(this, question + command, dangerous ? ManagedText("위험 명령 확인", "Confirm dangerous command") : ManagedText("명령 실행 확인", "Confirm command"), MessageBoxButtons.YesNo, dangerous ? MessageBoxIcon.Error : MessageBoxIcon.Warning) != DialogResult.Yes) return;
			}
			BackgroundAgentResponse response = SendBackgroundAgentRequest("command", profileName, command, 2000);
			if (response == null || !response.Success)
			{
				statusLabel.Text = response == null ? ManagedText("명령 전송 중 에이전트 연결이 끊겼습니다.", "Agent connection was lost while sending the command.") : response.Message;
				return;
			}
			commandBox.Clear();
			RefreshLogsAsync();
		}
	}
}
