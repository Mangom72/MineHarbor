using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

internal static partial class Launcher
{
	private const int DiscordRemoteSettingsSchemaVersion = 1;
	private const int DiscordRemoteSettingsMaximumBytes = 65536;
	private const int DiscordRemoteMaximumResponseCharacters = 1800;
	private const int DiscordRemoteMaximumGatewayBytes = 1048576;
	private const int DiscordRemoteMaximumHttpBytes = 1048576;
	private const int DiscordRemoteRequestsPerMinute = 10;
	private const int DiscordRemoteMaximumStatusProfiles = 15;
	private const int DiscordRemoteNotificationsPerMinute = 5;
	private const int DiscordRemoteBackupWaitSeconds = 90;
	private const int DiscordRemoteMaximumNotificationCharacters = 500;
	private const int DiscordRemoteConfirmationSeconds = 60;
	private static readonly object DiscordRemoteSettingsProcessLock = new object();
	private static readonly byte[] DiscordRemoteCredentialEntropy = Encoding.UTF8.GetBytes("MineHarbor.DiscordRemoteCredential.v1");
	private static string DiscordRemoteSettingsPathOverride = null;
	private static Func<DiscordRemoteSettings, string, CancellationToken, Task> DiscordRemoteRunOverride = null;
	private static Func<string, string, string, bool, string> DiscordRemoteActionOverride = null;

	private sealed class DiscordRemoteSettings
	{
		public int SchemaVersion = DiscordRemoteSettingsSchemaVersion;
		public bool Enabled;
		public string ProtectedBotToken = string.Empty;
		public string ApplicationId = string.Empty;
		public string GuildId = string.Empty;
		public string ChannelId = string.Empty;
		public List<string> AllowedUserIds = new List<string>();
		public List<string> AllowedRoleIds = new List<string>();
		public List<string> AllowedProfiles = new List<string>();
		// 서버 시작·종료·충돌을 허용 채널에 알립니다. 사용자가 명시적으로 켜야 동작합니다.
		public bool NotifyServerEvents;
	}

	private sealed class DiscordRemoteAction
	{
		public string Command;
		public string Profile;
		public string ActorId;
		public bool Korean;
		// 서버를 지정하지 않은 전체 조회에서는 프로필마다 반복되는 추가 조회를 생략합니다.
		public bool AllProfiles;
	}

	private sealed class DiscordRemoteActionResult
	{
		public bool Success;
		public string Message;
	}

	private sealed class DiscordInteractionReply
	{
		public string Content;
		public List<object> Components = new List<object>();
		public List<object> Choices = new List<object>();
		public bool IsAutocomplete;
		public bool UpdateOriginal;
	}

	private sealed class DiscordPendingConfirmation
	{
		public string Id;
		public string UserId;
		public string GuildId;
		public string ChannelId;
		public string Command;
		public string Profile;
		public DateTime ExpiresUtc;
		public bool Korean;
	}

	private sealed class DiscordCredentialException : Exception
	{
		public DiscordCredentialException(string message) : base(message) { }
	}

	private static string GetDiscordRemoteSettingsPath()
	{
		if (!string.IsNullOrWhiteSpace(DiscordRemoteSettingsPathOverride))
			return Path.GetFullPath(DiscordRemoteSettingsPathOverride);
		return Path.Combine(GetLauncherUserDataDirectory(), "discord-remote.json");
	}

	private static DiscordRemoteSettings ReadDiscordRemoteSettings()
	{
		lock (DiscordRemoteSettingsProcessLock)
		{
			return WithDiscordRemoteSettingsLock(delegate
			{
				string path = GetDiscordRemoteSettingsPath();
				if (!File.Exists(path)) return new DiscordRemoteSettings();
				FileInfo info = new FileInfo(path);
				if (info.Length <= 0 || info.Length > DiscordRemoteSettingsMaximumBytes)
					throw new InvalidDataException("Discord 원격 제어 설정 파일 크기가 올바르지 않습니다.");
				DiscordRemoteSettings settings;
				try
				{
					settings = new JavaScriptSerializer { MaxJsonLength = DiscordRemoteSettingsMaximumBytes }
						.Deserialize<DiscordRemoteSettings>(File.ReadAllText(path, Encoding.UTF8));
				}
				catch (Exception exception)
				{
					throw new InvalidDataException("Discord 원격 제어 설정 파일이 손상되었습니다. 원본 파일은 변경하지 않았습니다.", exception);
				}
				ValidateDiscordRemoteSettings(settings, settings != null && settings.Enabled);
				return settings;
			});
		}
	}

	private static void WriteDiscordRemoteSettings(DiscordRemoteSettings settings)
	{
		ValidateDiscordRemoteSettings(settings, settings != null && settings.Enabled);
		lock (DiscordRemoteSettingsProcessLock)
		{
			WithDiscordRemoteSettingsLock(delegate
			{
				string json = new JavaScriptSerializer { MaxJsonLength = DiscordRemoteSettingsMaximumBytes }.Serialize(settings);
				if (Encoding.UTF8.GetByteCount(json) > DiscordRemoteSettingsMaximumBytes)
					throw new InvalidDataException("Discord 원격 제어 설정이 허용 크기를 초과했습니다.");
				string path = GetDiscordRemoteSettingsPath();
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				string temporary = path + ".준비중";
				File.WriteAllText(temporary, json, new UTF8Encoding(false));
				ReplaceFile(temporary, path);
				return 0;
			});
		}
	}

	private static T WithDiscordRemoteSettingsLock<T>(Func<T> action)
	{
		string fullPath = Path.GetFullPath(GetDiscordRemoteSettingsPath());
		using (SHA256 hash = SHA256.Create())
		{
			string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(fullPath.ToUpperInvariant())))
				.Replace("-", string.Empty).Substring(0, 24);
			using (Mutex mutex = new Mutex(false, "Local\\MineHarbor.DiscordRemote." + suffix))
			{
				bool entered = false;
				try
				{
					try { entered = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
					catch (AbandonedMutexException) { entered = true; }
					if (!entered) throw new IOException("다른 MineHarbor 프로세스가 Discord 원격 제어 설정을 갱신하고 있습니다.");
					return action();
				}
				finally { if (entered) mutex.ReleaseMutex(); }
			}
		}
	}

	private static void ValidateDiscordRemoteSettings(DiscordRemoteSettings settings, bool requireCredential)
	{
		if (settings == null || settings.SchemaVersion != DiscordRemoteSettingsSchemaVersion)
			throw new InvalidDataException("지원하지 않는 Discord 원격 제어 설정 버전입니다.");
		if (settings.ProtectedBotToken == null) settings.ProtectedBotToken = string.Empty;
		if (settings.ApplicationId == null) settings.ApplicationId = string.Empty;
		if (settings.GuildId == null) settings.GuildId = string.Empty;
		if (settings.ChannelId == null) settings.ChannelId = string.Empty;
		settings.AllowedUserIds = NormalizeDiscordIdList(settings.AllowedUserIds, "허용 사용자");
		settings.AllowedRoleIds = NormalizeDiscordIdList(settings.AllowedRoleIds, "허용 역할");
		settings.AllowedProfiles = NormalizeDiscordProfileList(settings.AllowedProfiles);
		if (settings.ProtectedBotToken.Length > 4096)
			throw new InvalidDataException("암호화된 Discord 봇 토큰 크기가 올바르지 않습니다.");
		if (!settings.Enabled && !requireCredential) return;
		if (string.IsNullOrWhiteSpace(settings.ProtectedBotToken))
			throw new InvalidDataException("Discord 봇 토큰을 입력해 주세요.");
		if (!IsDiscordSnowflake(settings.ApplicationId)
			|| !IsDiscordSnowflake(settings.GuildId)
			|| !IsDiscordSnowflake(settings.ChannelId))
			throw new InvalidDataException("Discord 애플리케이션·서버·채널 ID를 확인해 주세요.");
		if (settings.AllowedUserIds.Count == 0 && settings.AllowedRoleIds.Count == 0)
			throw new InvalidDataException("허용할 Discord 사용자 또는 역할을 하나 이상 지정해 주세요.");
		if (settings.AllowedProfiles.Count == 0)
			throw new InvalidDataException("Discord에서 관리할 서버 프로필을 하나 이상 선택해 주세요.");
	}

	private static List<string> NormalizeDiscordIdList(IEnumerable<string> values, string fieldName)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		if (values == null) return result;
		foreach (string raw in values)
		{
			string value = (raw ?? string.Empty).Trim();
			if (value.Length == 0) continue;
			if (!IsDiscordSnowflake(value)) throw new InvalidDataException(fieldName + " Discord ID가 올바르지 않습니다.");
			if (seen.Add(value)) result.Add(value);
			if (result.Count > 100) throw new InvalidDataException(fieldName + " 목록이 허용 개수를 초과했습니다.");
		}
		return result;
	}

	private static List<string> NormalizeDiscordProfileList(IEnumerable<string> values)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (values == null) return result;
		foreach (string raw in values)
		{
			string value = (raw ?? string.Empty).Trim();
			if (value.Length == 0) continue;
			if (!IsValidProfileName(value)) throw new InvalidDataException("Discord 관리 서버 프로필 이름이 올바르지 않습니다.");
			if (seen.Add(value)) result.Add(value);
			if (result.Count > 100) throw new InvalidDataException("Discord 관리 서버 프로필 수가 허용 범위를 초과했습니다.");
		}
		return result;
	}

	private static bool IsDiscordSnowflake(string value)
	{
		if (string.IsNullOrEmpty(value) || value.Length < 5 || value.Length > 20 || value[0] == '0') return false;
		for (int index = 0; index < value.Length; index++)
			if (value[index] < '0' || value[index] > '9') return false;
		ulong parsed;
		return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed > 0;
	}

	private static string ProtectDiscordBotToken(string token)
	{
		ValidateDiscordBotToken(token);
		byte[] clear = Encoding.UTF8.GetBytes(token);
		try
		{
			byte[] protectedBytes = ProtectedData.Protect(clear, DiscordRemoteCredentialEntropy, DataProtectionScope.CurrentUser);
			return Convert.ToBase64String(protectedBytes);
		}
		finally { Array.Clear(clear, 0, clear.Length); }
	}

	private static string UnprotectDiscordBotToken(string protectedToken)
	{
		if (string.IsNullOrWhiteSpace(protectedToken) || protectedToken.Length > 4096)
			throw new InvalidDataException("저장된 Discord 봇 토큰이 없습니다.");
		byte[] protectedBytes;
		try { protectedBytes = Convert.FromBase64String(protectedToken); }
		catch (FormatException exception) { throw new InvalidDataException("암호화된 Discord 봇 토큰 형식이 올바르지 않습니다.", exception); }
		byte[] clear = null;
		try
		{
			clear = ProtectedData.Unprotect(protectedBytes, DiscordRemoteCredentialEntropy, DataProtectionScope.CurrentUser);
			string token = Encoding.UTF8.GetString(clear);
			ValidateDiscordBotToken(token);
			return token;
		}
		catch (CryptographicException exception)
		{
			throw new InvalidDataException("현재 Windows 사용자로 Discord 봇 토큰을 복호화하지 못했습니다.", exception);
		}
		finally
		{
			Array.Clear(protectedBytes, 0, protectedBytes.Length);
			if (clear != null) Array.Clear(clear, 0, clear.Length);
		}
	}

	private static void ValidateDiscordBotToken(string token)
	{
		if (string.IsNullOrWhiteSpace(token) || token.Length < 24 || token.Length > 256)
			throw new InvalidDataException("Discord 봇 토큰 길이가 올바르지 않습니다.");
		for (int index = 0; index < token.Length; index++)
		{
			char character = token[index];
			if (character < 33 || character > 126)
				throw new InvalidDataException("Discord 봇 토큰에 허용되지 않는 문자가 있습니다.");
		}
	}

	private sealed class DiscordRemoteController : IDisposable
	{
		private readonly object stateLock = new object();
		private readonly Func<DiscordRemoteAction, DiscordRemoteActionResult> actionHandler;
		private CancellationTokenSource runCancellation;
		private Task runTask;
		private DiscordGatewayClient activeClient;
		private readonly Queue<DateTime> notificationTimes = new Queue<DateTime>();
		private string fingerprint = string.Empty;
		private string stateKo = "비활성화됨";
		private string stateEn = "Disabled";
		private bool connected;
		private bool disposed;

		public DiscordRemoteController(Func<DiscordRemoteAction, DiscordRemoteActionResult> handler)
		{
			if (handler == null) throw new ArgumentNullException("handler");
			actionHandler = handler;
		}

		public void Reload()
		{
			if (disposed) return;
			DiscordRemoteSettings settings;
			try { settings = ReadDiscordRemoteSettings(); }
			catch (Exception exception)
			{
				StopCurrentRun();
				SetState(false, "설정 오류: " + exception.Message, "Settings error: the saved file could not be validated.");
				return;
			}
			string nextFingerprint = CalculateDiscordSettingsFingerprint(settings);
			lock (stateLock)
			{
				if (string.Equals(nextFingerprint, fingerprint, StringComparison.Ordinal) && runTask != null && !runTask.IsCompleted) return;
				fingerprint = nextFingerprint;
			}
			StopCurrentRun();
			if (!settings.Enabled)
			{
				SetState(false, "비활성화됨", "Disabled");
				return;
			}
			string token;
			try { token = UnprotectDiscordBotToken(settings.ProtectedBotToken); }
			catch (Exception exception)
			{
				SetState(false, "자격 증명 오류: " + exception.Message, "Credential error: the saved token is unavailable for this Windows user.");
				return;
			}
			CancellationTokenSource nextCancellation = new CancellationTokenSource();
			lock (stateLock) runCancellation = nextCancellation;
			SetState(false, "Discord에 연결하는 중", "Connecting to Discord");
			runTask = Task.Run(async delegate
			{
				try
				{
					if (DiscordRemoteRunOverride != null)
						await DiscordRemoteRunOverride(settings, token, nextCancellation.Token).ConfigureAwait(false);
					else
					{
						using (DiscordGatewayClient client = new DiscordGatewayClient(settings, token, actionHandler, SetState))
						{
							lock (stateLock) activeClient = client;
							try { await client.RunAsync(nextCancellation.Token).ConfigureAwait(false); }
							finally { lock (stateLock) { if (ReferenceEquals(activeClient, client)) activeClient = null; } }
						}
					}
				}
				catch (OperationCanceledException) { }
				catch (DiscordCredentialException exception)
				{
					SetState(false, "Discord 인증 거부: " + exception.Message, "Discord authentication rejected. Check the bot token, installation, and permissions.");
				}
				catch (Exception exception)
				{
					SetState(false, "연결 중단: " + exception.GetType().Name, "Connection stopped: " + exception.GetType().Name);
				}
			});
		}

		private static string CalculateDiscordSettingsFingerprint(DiscordRemoteSettings settings)
		{
			string serialized = new JavaScriptSerializer().Serialize(settings);
			using (SHA256 hash = SHA256.Create())
				return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(serialized)));
		}

		// 서버 상태 변화를 허용 채널에 알립니다. 연결이 없거나 설정이 꺼져 있으면 조용히 무시하고,
		// 짧은 시간에 알림이 몰려도 채널을 도배하지 않도록 분당 발신 수를 제한합니다.
		public void NotifyServerEvent(string content)
		{
			if (disposed || string.IsNullOrWhiteSpace(content)) return;
			DiscordGatewayClient client;
			CancellationTokenSource cancellationSource;
			lock (stateLock)
			{
				client = activeClient;
				cancellationSource = runCancellation;
				if (client == null || cancellationSource == null) return;
				DateTime now = DateTime.UtcNow;
				while (notificationTimes.Count > 0 && now - notificationTimes.Peek() >= TimeSpan.FromMinutes(1)) notificationTimes.Dequeue();
				if (notificationTimes.Count >= DiscordRemoteNotificationsPerMinute) return;
				notificationTimes.Enqueue(now);
			}
			CancellationToken cancellationToken;
			try { cancellationToken = cancellationSource.Token; }
			catch (ObjectDisposedException) { return; }
			ObserveDiscordNotificationAsync(client, content, cancellationToken);
		}

		private static async void ObserveDiscordNotificationAsync(DiscordGatewayClient client, string content, CancellationToken cancellationToken)
		{
			try { await client.PostChannelNotificationAsync(content, cancellationToken).ConfigureAwait(false); }
			catch (OperationCanceledException) { }
			catch (Exception exception) { Console.Error.WriteLine("[DiscordRemote] 채널 알림 실패 (" + exception.GetType().Name + ")"); }
		}

		private void StopCurrentRun()
		{
			CancellationTokenSource previous;
			lock (stateLock)
			{
				previous = runCancellation;
				runCancellation = null;
				runTask = null;
				activeClient = null;
				connected = false;
			}
			if (previous != null)
			{
				try { previous.Cancel(); } catch { }
				previous.Dispose();
			}
		}

		private void SetState(bool isConnected, string korean, string english)
		{
			lock (stateLock)
			{
				connected = isConnected;
				stateKo = string.IsNullOrWhiteSpace(korean) ? "상태를 확인할 수 없음" : korean;
				stateEn = string.IsNullOrWhiteSpace(english) ? "Status unavailable" : english;
			}
		}

		public BackgroundAgentResponse CreateAgentResponse()
		{
			lock (stateLock)
			{
				bool currentConnection = connected;
				return new BackgroundAgentResponse
				{
					Success = currentConnection,
					Message = ManagedText(stateKo, stateEn),
					UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
				};
			}
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			StopCurrentRun();
		}
	}

	private sealed class DiscordInteractionProcessor
	{
		private readonly object syncRoot = new object();
		private readonly DiscordRemoteSettings settings;
		private readonly Func<DiscordRemoteAction, DiscordRemoteActionResult> actionHandler;
		private readonly Dictionary<string, Queue<DateTime>> requestTimes = new Dictionary<string, Queue<DateTime>>(StringComparer.Ordinal);
		private readonly Dictionary<string, DiscordPendingConfirmation> confirmations = new Dictionary<string, DiscordPendingConfirmation>(StringComparer.Ordinal);
		private readonly Dictionary<string, DateTime> processedInteractions = new Dictionary<string, DateTime>(StringComparer.Ordinal);

		public DiscordInteractionProcessor(DiscordRemoteSettings remoteSettings, Func<DiscordRemoteAction, DiscordRemoteActionResult> handler)
		{
			settings = remoteSettings;
			actionHandler = handler;
		}

		public DiscordInteractionReply Process(Dictionary<string, object> interaction, DateTime nowUtc)
		{
			string interactionId = GetString(interaction, "id");
			int type = GetInt(interaction, "type");
			Dictionary<string, object> data = GetDictionary(interaction, "data");
			bool korean = GetString(interaction, "locale").StartsWith("ko", StringComparison.OrdinalIgnoreCase);
			if (type == 4) return ProcessAutocomplete(interaction, data, korean);
			if (type != 2 && type != 3) return MessageReply(Text(korean, "지원하지 않는 Discord 상호작용입니다.", "Unsupported Discord interaction."));
			string guildId = GetString(interaction, "guild_id");
			string channelId = GetString(interaction, "channel_id");
			Dictionary<string, object> member = GetDictionary(interaction, "member");
			Dictionary<string, object> user = GetDictionary(member, "user");
			string userId = GetString(user, "id");
			if (!string.Equals(GetString(interaction, "application_id"), settings.ApplicationId, StringComparison.Ordinal)
				|| !string.Equals(guildId, settings.GuildId, StringComparison.Ordinal)
				|| !string.Equals(channelId, settings.ChannelId, StringComparison.Ordinal)
				|| !IsAuthorizedUser(userId, member))
				return MessageReply(Text(korean, "이 Discord 사용자·서버·채널에는 권한이 없습니다.", "This Discord user, server, or channel is not authorized."));
			lock (syncRoot)
			{
				CleanupState(nowUtc);
				DateTime processedAt;
				if (!string.IsNullOrEmpty(interactionId) && processedInteractions.TryGetValue(interactionId, out processedAt))
					return MessageReply(Text(korean, "이미 처리한 요청입니다.", "This request was already processed."));
				if (!string.IsNullOrEmpty(interactionId)) processedInteractions[interactionId] = nowUtc;
				if (!TakeRateLimit(userId, nowUtc))
					return MessageReply(Text(korean, "요청이 너무 잦습니다. 잠시 후 다시 시도해 주세요.", "Too many requests. Try again shortly."));
			}
			if (type == 3) return ProcessComponent(data, userId, guildId, channelId, korean, nowUtc);
			if (!string.Equals(GetString(data, "name"), "mineharbor", StringComparison.Ordinal))
				return MessageReply(Text(korean, "알 수 없는 명령입니다.", "Unknown command."));
			string command;
			string profile;
			ReadCommandOptions(data, out command, out profile);
			if (string.IsNullOrEmpty(command))
				return MessageReply(Text(korean, "실행할 하위 명령이 없습니다.", "No subcommand was provided."));
			if (string.Equals(command, "help", StringComparison.Ordinal))
				return MessageReply(CreateHelpText(korean));
			if (!(string.Equals(command, "status", StringComparison.Ordinal)
				|| string.Equals(command, "players", StringComparison.Ordinal)
				|| string.Equals(command, "errors", StringComparison.Ordinal)
				|| string.Equals(command, "start", StringComparison.Ordinal)
				|| string.Equals(command, "stop", StringComparison.Ordinal)
				|| string.Equals(command, "restart", StringComparison.Ordinal)
				|| string.Equals(command, "backup", StringComparison.Ordinal)))
				return MessageReply(Text(korean, "지원하지 않는 원격 명령입니다.", "Unsupported remote command."));
			if (string.Equals(command, "status", StringComparison.Ordinal) && string.IsNullOrEmpty(profile))
				return ProcessAllStatus(userId, korean);
			if (!IsAllowedProfile(profile))
				return MessageReply(Text(korean, "허용된 서버 프로필을 선택해 주세요.", "Choose an allowed server profile."));
			if (string.Equals(command, "stop", StringComparison.Ordinal) || string.Equals(command, "restart", StringComparison.Ordinal))
				return CreateConfirmation(userId, guildId, channelId, command, profile, korean, nowUtc);
			return Execute(command, profile, userId, korean);
		}

		private DiscordInteractionReply ProcessAutocomplete(Dictionary<string, object> interaction, Dictionary<string, object> data, bool korean)
		{
			DiscordInteractionReply reply = new DiscordInteractionReply { IsAutocomplete = true };
			string guildId = GetString(interaction, "guild_id");
			string channelId = GetString(interaction, "channel_id");
			Dictionary<string, object> member = GetDictionary(interaction, "member");
			string userId = GetString(GetDictionary(member, "user"), "id");
			if (!string.Equals(GetString(interaction, "application_id"), settings.ApplicationId, StringComparison.Ordinal)
				|| !string.Equals(guildId, settings.GuildId, StringComparison.Ordinal)
				|| !string.Equals(channelId, settings.ChannelId, StringComparison.Ordinal)
				|| !IsAuthorizedUser(userId, member))
				return reply;
			string command;
			string profile;
			ReadCommandOptions(data, out command, out profile);
			string prefix = profile ?? string.Empty;
			foreach (string allowed in settings.AllowedProfiles
				.Where(delegate(string value) { return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase); })
				.OrderBy(delegate(string value) { return value; }, StringComparer.OrdinalIgnoreCase)
				.Take(25))
			{
				reply.Choices.Add(new Dictionary<string, object> { { "name", allowed }, { "value", allowed } });
			}
			return reply;
		}

		private DiscordInteractionReply ProcessComponent(
			Dictionary<string, object> data,
			string userId,
			string guildId,
			string channelId,
			bool korean,
			DateTime nowUtc)
		{
			string customId = GetString(data, "custom_id");
			bool confirm = customId.StartsWith("mh:confirm:", StringComparison.Ordinal);
			bool cancel = customId.StartsWith("mh:cancel:", StringComparison.Ordinal);
			if (!confirm && !cancel) return MessageReply(Text(korean, "알 수 없는 확인 동작입니다.", "Unknown confirmation action."));
			string id = customId.Substring(confirm ? 11 : 10);
			DiscordPendingConfirmation pending;
			lock (syncRoot)
			{
				if (!confirmations.TryGetValue(id, out pending))
					return MessageReply(Text(korean, "확인 요청이 만료되었거나 이미 처리되었습니다.", "The confirmation expired or was already handled."));
				if (pending.ExpiresUtc < nowUtc
					|| !string.Equals(pending.UserId, userId, StringComparison.Ordinal)
					|| !string.Equals(pending.GuildId, guildId, StringComparison.Ordinal)
					|| !string.Equals(pending.ChannelId, channelId, StringComparison.Ordinal))
					return MessageReply(Text(korean, "이 확인 요청을 처리할 권한이 없습니다.", "You cannot handle this confirmation."));
				confirmations.Remove(id);
			}
			DiscordInteractionReply reply;
			if (cancel)
				reply = MessageReply(Text(pending.Korean, "요청을 취소했습니다.", "Request cancelled."));
			else
				reply = Execute(pending.Command, pending.Profile, userId, pending.Korean);
			reply.UpdateOriginal = true;
			return reply;
		}

		// 도움말에 실제로 사용할 수 있는 서버 이름을 함께 보여 주어 프로필 이름을 추측하지 않게 합니다.
		private string CreateHelpText(bool korean)
		{
			string help = Text(korean,
				"`/mineharbor`는 상태·플레이어·최근 오류 조회와 시작·안전 종료·재시작·백업을 지원합니다. 임의 콘솔 명령은 지원하지 않습니다.",
				"`/mineharbor` supports status, players, recent errors, start, safe stop, restart, and backup. Arbitrary console commands are not supported.");
			List<string> profiles = settings.AllowedProfiles;
			if (profiles == null || profiles.Count == 0)
				return help + "\n" + Text(korean,
					"아직 허용된 서버가 없습니다. MineHarbor의 Discord 원격 제어 설정에서 관리할 서버를 선택해 주세요.",
					"No servers are approved yet. Choose the servers to manage in MineHarbor's Discord remote-control settings.");
			int listed = Math.Min(profiles.Count, DiscordRemoteMaximumStatusProfiles);
			List<string> names = new List<string>();
			for (int index = 0; index < listed; index++) names.Add("`" + profiles[index] + "`");
			string line = Text(korean, "사용할 수 있는 서버: ", "Available servers: ") + string.Join(", ", names.ToArray());
			int omitted = profiles.Count - listed;
			if (omitted > 0)
			{
				string count = omitted.ToString(CultureInfo.InvariantCulture);
				line += Text(korean, " 외 " + count + "개", " and " + count + " more");
			}
			return help + "\n" + line;
		}

		private DiscordInteractionReply ProcessAllStatus(string userId, bool korean)
		{
			// 전체 상태는 프로필마다 서버 조회를 한 번씩 하므로 한 응답에서 다루는 수를 제한하고,
			// 생략된 서버는 잘라내는 대신 몇 개가 남았는지 알려 줍니다.
			List<string> lines = new List<string>();
			int reported = Math.Min(settings.AllowedProfiles.Count, DiscordRemoteMaximumStatusProfiles);
			for (int index = 0; index < reported; index++)
			{
				string profile = settings.AllowedProfiles[index];
				DiscordRemoteActionResult result = RunAction("status", profile, userId, korean, true);
				lines.Add(result == null || string.IsNullOrWhiteSpace(result.Message)
					? "`" + profile + "` · " + Text(korean, "상태를 확인할 수 없음", "Status unavailable")
					: result.Message);
			}
			int omitted = settings.AllowedProfiles.Count - reported;
			if (omitted > 0)
			{
				string count = omitted.ToString(CultureInfo.InvariantCulture);
				lines.Add(Text(korean,
					"그 외 " + count + "개 서버는 생략했습니다. `/mineharbor status server:<서버 이름>`으로 확인해 주세요.",
					count + " more server(s) omitted. Use `/mineharbor status server:<name>` to check them."));
			}
			return MessageReply(string.Join("\n", lines.ToArray()));
		}

		private DiscordInteractionReply CreateConfirmation(
			string userId,
			string guildId,
			string channelId,
			string command,
			string profile,
			bool korean,
			DateTime nowUtc)
		{
			string id = CreateDiscordConfirmationId();
			lock (syncRoot)
			{
				confirmations[id] = new DiscordPendingConfirmation
				{
					Id = id,
					UserId = userId,
					GuildId = guildId,
					ChannelId = channelId,
					Command = command,
					Profile = profile,
					ExpiresUtc = nowUtc.AddSeconds(DiscordRemoteConfirmationSeconds),
					Korean = korean
				};
			}
			bool restart = string.Equals(command, "restart", StringComparison.Ordinal);
			DiscordInteractionReply reply = MessageReply(Text(korean,
				"`" + profile + "` 서버를 " + (restart ? "안전하게 재시작" : "안전 종료") + "하시겠습니까? 60초 안에 확인해 주세요.",
				"Do you want to " + (restart ? "safely restart" : "safely stop") + " `" + profile + "`? Confirm within 60 seconds."));
			reply.Components.Add(new Dictionary<string, object>
			{
				{ "type", 1 },
				{ "components", new object[]
					{
						new Dictionary<string, object>
						{
							{ "type", 2 }, { "style", 4 },
							{ "label", Text(korean, restart ? "재시작 확인" : "종료 확인", restart ? "Confirm restart" : "Confirm stop") },
							{ "custom_id", "mh:confirm:" + id }
						},
						new Dictionary<string, object>
						{
							{ "type", 2 }, { "style", 2 },
							{ "label", Text(korean, "취소", "Cancel") },
							{ "custom_id", "mh:cancel:" + id }
						}
					}
				}
			});
			return reply;
		}

		private DiscordRemoteActionResult RunAction(string command, string profile, string userId, bool korean)
		{
			return RunAction(command, profile, userId, korean, false);
		}

		private DiscordRemoteActionResult RunAction(string command, string profile, string userId, bool korean, bool allProfiles)
		{
			try
			{
				if (DiscordRemoteActionOverride != null)
				{
					string overridden = DiscordRemoteActionOverride(command, profile, userId, korean);
					return new DiscordRemoteActionResult { Success = overridden != null, Message = overridden ?? Text(korean, "테스트 작업 실패", "Test action failed") };
				}
				if (actionHandler == null) throw new InvalidOperationException("Discord 원격 작업 처리기가 없습니다.");
				return actionHandler(new DiscordRemoteAction
				{
					Command = command,
					Profile = profile,
					ActorId = userId,
					Korean = korean,
					AllProfiles = allProfiles
				});
			}
			catch (Exception exception)
			{
				return new DiscordRemoteActionResult
				{
					Success = false,
					Message = Text(korean, "요청 처리 실패: ", "Request failed: ") + exception.GetType().Name
				};
			}
		}

		private DiscordInteractionReply Execute(string command, string profile, string userId, bool korean)
		{
			DiscordRemoteActionResult result = RunAction(command, profile, userId, korean);
			if (result == null) result = new DiscordRemoteActionResult { Success = false, Message = Text(korean, "응답 없음", "No response") };
			return MessageReply((result.Success ? "✅ " : "⚠️ ") + result.Message);
		}

		private bool IsAuthorizedUser(string userId, Dictionary<string, object> member)
		{
			if (settings.AllowedUserIds.Contains(userId, StringComparer.Ordinal)) return true;
			List<object> roles = GetList(member, "roles");
			for (int index = 0; index < roles.Count; index++)
				if (settings.AllowedRoleIds.Contains(Convert.ToString(roles[index], CultureInfo.InvariantCulture), StringComparer.Ordinal)) return true;
			return false;
		}

		private bool IsAllowedProfile(string profile)
		{
			return !string.IsNullOrWhiteSpace(profile)
				&& settings.AllowedProfiles.Contains(profile, StringComparer.OrdinalIgnoreCase);
		}

		private bool TakeRateLimit(string userId, DateTime nowUtc)
		{
			Queue<DateTime> times;
			if (!requestTimes.TryGetValue(userId, out times))
			{
				times = new Queue<DateTime>();
				requestTimes[userId] = times;
			}
			while (times.Count > 0 && nowUtc - times.Peek() >= TimeSpan.FromMinutes(1)) times.Dequeue();
			if (times.Count >= DiscordRemoteRequestsPerMinute) return false;
			times.Enqueue(nowUtc);
			return true;
		}

		private void CleanupState(DateTime nowUtc)
		{
			foreach (string id in confirmations.Where(delegate(KeyValuePair<string, DiscordPendingConfirmation> pair) { return pair.Value.ExpiresUtc < nowUtc; }).Select(delegate(KeyValuePair<string, DiscordPendingConfirmation> pair) { return pair.Key; }).ToArray())
				confirmations.Remove(id);
			foreach (string id in processedInteractions.Where(delegate(KeyValuePair<string, DateTime> pair) { return nowUtc - pair.Value > TimeSpan.FromMinutes(15); }).Select(delegate(KeyValuePair<string, DateTime> pair) { return pair.Key; }).ToArray())
				processedInteractions.Remove(id);
			foreach (string userId in requestTimes.Keys.ToArray())
			{
				Queue<DateTime> times = requestTimes[userId];
				while (times.Count > 0 && nowUtc - times.Peek() >= TimeSpan.FromMinutes(1)) times.Dequeue();
				if (times.Count == 0) requestTimes.Remove(userId);
			}
		}

		private static void ReadCommandOptions(Dictionary<string, object> data, out string command, out string profile)
		{
			command = string.Empty;
			profile = string.Empty;
			List<object> rootOptions = GetList(data, "options");
			if (rootOptions.Count == 0) return;
			Dictionary<string, object> subcommand = rootOptions[0] as Dictionary<string, object>;
			if (subcommand == null) return;
			command = GetString(subcommand, "name").Trim().ToLowerInvariant();
			List<object> options = GetList(subcommand, "options");
			for (int index = 0; index < options.Count; index++)
			{
				Dictionary<string, object> option = options[index] as Dictionary<string, object>;
				if (option == null || !string.Equals(GetString(option, "name"), "server", StringComparison.Ordinal)) continue;
				profile = GetString(option, "value").Trim();
				break;
			}
		}

		private static string CreateDiscordConfirmationId()
		{
			byte[] bytes = new byte[18];
			using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
			return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
		}

		private static DiscordInteractionReply MessageReply(string content)
		{
			return new DiscordInteractionReply { Content = TrimDiscordResponse(content) };
		}

		private static string TrimDiscordResponse(string content)
		{
			string value = (content ?? string.Empty).Replace('\0', ' ').Trim();
			if (value.Length == 0) value = "-";
			if (value.Length > DiscordRemoteMaximumResponseCharacters)
				value = value.Substring(0, DiscordRemoteMaximumResponseCharacters - 3) + "...";
			return value;
		}

		private static string Text(bool korean, string ko, string en) { return korean ? ko : en; }
	}

	private sealed class DiscordGatewayClient : IDisposable
	{
		private readonly DiscordRemoteSettings settings;
		private readonly string token;
		private readonly DiscordInteractionProcessor processor;
		private readonly Action<bool, string, string> stateChanged;
		private readonly HttpClient http;
		private readonly SemaphoreSlim gatewaySendLock = new SemaphoreSlim(1, 1);
		private string sessionId;
		private string resumeGatewayUrl;
		private long? sequence;
		private bool disposed;

		public DiscordGatewayClient(
			DiscordRemoteSettings remoteSettings,
			string botToken,
			Func<DiscordRemoteAction, DiscordRemoteActionResult> actionHandler,
			Action<bool, string, string> stateCallback)
		{
			settings = remoteSettings;
			token = botToken;
			processor = new DiscordInteractionProcessor(remoteSettings, actionHandler);
			stateChanged = stateCallback;
			HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
			http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
			http.DefaultRequestHeaders.UserAgent.ParseAdd("MineHarbor/" + BuildVersionInfo.ProductVersion + " (https://github.com/Mangom72/MineHarbor)");
		}

		public async Task RunAsync(CancellationToken tokenCancellation)
		{
			string gatewayUrl = null;
			int failures = 0;
			while (!tokenCancellation.IsCancellationRequested)
			{
				bool retry = false;
				try
				{
					if (string.IsNullOrEmpty(gatewayUrl))
					{
						await RegisterGuildCommandAsync(tokenCancellation).ConfigureAwait(false);
						gatewayUrl = await GetGatewayUrlAsync(tokenCancellation).ConfigureAwait(false);
					}
					await ConnectOnceAsync(gatewayUrl, tokenCancellation).ConfigureAwait(false);
					failures = 0;
				}
				catch (OperationCanceledException) { throw; }
				catch (DiscordCredentialException) { throw; }
				catch
				{
					failures++;
					stateChanged(false, "Discord 재연결 대기 중", "Waiting to reconnect to Discord");
					retry = true;
				}
				if (retry)
				{
					int seconds = Math.Min(120, 2 << Math.Min(5, failures));
					await Task.Delay(TimeSpan.FromSeconds(seconds), tokenCancellation).ConfigureAwait(false);
				}
			}
		}

		private async Task ConnectOnceAsync(string gatewayUrl, CancellationToken cancellationToken)
		{
			string selectedUrl = !string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(resumeGatewayUrl) ? resumeGatewayUrl : gatewayUrl;
			Uri gateway = CreateGatewayUri(selectedUrl);
			using (ClientWebSocket socket = new ClientWebSocket())
			{
				socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
				await socket.ConnectAsync(gateway, cancellationToken).ConfigureAwait(false);
				Dictionary<string, object> hello = await ReceiveGatewayPayloadAsync(socket, cancellationToken).ConfigureAwait(false);
				if (GetInt(hello, "op") != 10) throw new InvalidDataException("Discord Gateway Hello를 받지 못했습니다.");
				int heartbeatMilliseconds = GetInt(GetDictionary(hello, "d"), "heartbeat_interval");
				if (heartbeatMilliseconds < 1000 || heartbeatMilliseconds > 120000)
					throw new InvalidDataException("Discord Gateway heartbeat 간격이 올바르지 않습니다.");
				HeartbeatState heartbeat = new HeartbeatState();
				Task heartbeatTask = RunHeartbeatAsync(socket, heartbeatMilliseconds, heartbeat, cancellationToken);
				if (!string.IsNullOrWhiteSpace(sessionId) && sequence.HasValue)
					await SendGatewayPayloadAsync(socket, new Dictionary<string, object>
					{
						{ "op", 6 },
						{ "d", new Dictionary<string, object> { { "token", token }, { "session_id", sessionId }, { "seq", sequence.Value } } }
					}, cancellationToken).ConfigureAwait(false);
				else
					await SendGatewayPayloadAsync(socket, CreateIdentifyPayload(), cancellationToken).ConfigureAwait(false);
				try
				{
					while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
					{
						Dictionary<string, object> payload = await ReceiveGatewayPayloadAsync(socket, cancellationToken).ConfigureAwait(false);
						int opcode = GetInt(payload, "op");
						if (payload.ContainsKey("s") && payload["s"] != null) sequence = Convert.ToInt64(payload["s"], CultureInfo.InvariantCulture);
						if (opcode == 11)
						{
							heartbeat.Acknowledged = true;
							continue;
						}
						if (opcode == 1)
						{
							await SendHeartbeatAsync(socket, cancellationToken).ConfigureAwait(false);
							continue;
						}
						if (opcode == 7) throw new IOException("Discord Gateway가 재연결을 요청했습니다.");
						if (opcode == 9)
						{
							bool resumable = payload.ContainsKey("d") && Convert.ToBoolean(payload["d"], CultureInfo.InvariantCulture);
							if (!resumable)
							{
								sessionId = null;
								resumeGatewayUrl = null;
								sequence = null;
							}
							throw new IOException("Discord Gateway 세션이 유효하지 않습니다.");
						}
						if (opcode != 0) continue;
						string eventName = GetString(payload, "t");
						Dictionary<string, object> eventData = GetDictionary(payload, "d");
						if (string.Equals(eventName, "READY", StringComparison.Ordinal))
						{
							sessionId = GetString(eventData, "session_id");
							resumeGatewayUrl = GetString(eventData, "resume_gateway_url");
							if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(resumeGatewayUrl))
								throw new InvalidDataException("Discord Gateway 세션 정보가 없습니다.");
							CreateGatewayUri(resumeGatewayUrl);
							stateChanged(true, "연결됨 · /mineharbor 사용 가능", "Connected · /mineharbor is available");
						}
						else if (string.Equals(eventName, "RESUMED", StringComparison.Ordinal))
						{
							stateChanged(true, "재연결됨 · /mineharbor 사용 가능", "Reconnected · /mineharbor is available");
						}
						else if (string.Equals(eventName, "INTERACTION_CREATE", StringComparison.Ordinal))
						{
							ObserveInteractionAsync(eventData, cancellationToken);
						}
					}
				}
				finally
				{
					heartbeat.Stop = true;
					try { socket.Abort(); } catch { }
					try { heartbeatTask.Wait(1000); } catch { }
				}
			}
		}

		private sealed class HeartbeatState
		{
			public volatile bool Acknowledged = true;
			public volatile bool Stop;
		}

		private async Task RunHeartbeatAsync(ClientWebSocket socket, int interval, HeartbeatState state, CancellationToken cancellationToken)
		{
			int initialDelay;
			byte[] random = new byte[4];
			using (RandomNumberGenerator generator = RandomNumberGenerator.Create()) generator.GetBytes(random);
			initialDelay = (int)(BitConverter.ToUInt32(random, 0) % (uint)Math.Max(1, interval));
			await Task.Delay(initialDelay, cancellationToken).ConfigureAwait(false);
			while (!state.Stop && !cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
			{
				if (!state.Acknowledged)
				{
					try { socket.Abort(); } catch { }
					throw new IOException("Discord Gateway heartbeat 응답이 없습니다.");
				}
				state.Acknowledged = false;
				await SendHeartbeatAsync(socket, cancellationToken).ConfigureAwait(false);
				await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
			}
		}

		private Task SendHeartbeatAsync(ClientWebSocket socket, CancellationToken cancellationToken)
		{
			return SendGatewayPayloadAsync(socket, new Dictionary<string, object> { { "op", 1 }, { "d", sequence.HasValue ? (object)sequence.Value : null } }, cancellationToken);
		}

		private Dictionary<string, object> CreateIdentifyPayload()
		{
			return new Dictionary<string, object>
			{
				{ "op", 2 },
				{ "d", new Dictionary<string, object>
					{
						{ "token", token },
						{ "intents", 0 },
						{ "properties", new Dictionary<string, object>
							{
								{ "os", "windows" },
								{ "browser", "mineharbor" },
								{ "device", "mineharbor" }
							}
						}
					}
				}
			};
		}

		private async void ObserveInteractionAsync(Dictionary<string, object> interaction, CancellationToken cancellationToken)
		{
			try { await HandleInteractionAsync(interaction, cancellationToken).ConfigureAwait(false); }
			catch (OperationCanceledException) { }
			catch { }
		}

		private async Task HandleInteractionAsync(Dictionary<string, object> interaction, CancellationToken cancellationToken)
		{
			string interactionId = GetString(interaction, "id");
			string interactionToken = GetString(interaction, "token");
			int interactionType = GetInt(interaction, "type");
			if (!IsDiscordSnowflake(interactionId) || !IsSafeDiscordInteractionToken(interactionToken)) return;
			if (interactionType == 4)
			{
				DiscordInteractionReply autocomplete = processor.Process(interaction, DateTime.UtcNow);
				await SendInteractionCallbackAsync(interactionId, interactionToken, new Dictionary<string, object>
				{
					{ "type", 8 },
					{ "data", new Dictionary<string, object> { { "choices", autocomplete.Choices.ToArray() } } }
				}, false, cancellationToken).ConfigureAwait(false);
				return;
			}
			int deferType = interactionType == 3 ? 6 : 5;
			Dictionary<string, object> deferData = deferType == 5 ? new Dictionary<string, object> { { "flags", 64 } } : null;
			Dictionary<string, object> defer = new Dictionary<string, object> { { "type", deferType } };
			if (deferData != null) defer["data"] = deferData;
			await SendInteractionCallbackAsync(interactionId, interactionToken, defer, false, cancellationToken).ConfigureAwait(false);
			DiscordInteractionReply reply = processor.Process(interaction, DateTime.UtcNow);
			Dictionary<string, object> message = new Dictionary<string, object>
			{
				{ "content", reply.Content },
				{ "allowed_mentions", new Dictionary<string, object> { { "parse", new object[0] } } },
				{ "components", reply.Components.ToArray() }
			};
			await EditOriginalInteractionAsync(interactionToken, message, cancellationToken).ConfigureAwait(false);
		}

		private async Task RegisterGuildCommandAsync(CancellationToken cancellationToken)
		{
			Uri uri = CreateDiscordApiUri("/api/v10/applications/" + settings.ApplicationId + "/guilds/" + settings.GuildId + "/commands");
			Dictionary<string, object> command = CreateMineHarborCommandDefinition();
			await SendDiscordHttpAsync(HttpMethod.Post, uri, command, true, true, cancellationToken).ConfigureAwait(false);
		}

		private async Task<string> GetGatewayUrlAsync(CancellationToken cancellationToken)
		{
			Dictionary<string, object> response = await SendDiscordHttpAsync(
				HttpMethod.Get,
				CreateDiscordApiUri("/api/v10/gateway/bot"),
				null,
				true,
				true,
				cancellationToken).ConfigureAwait(false);
			string url = GetString(response, "url");
			CreateGatewayUri(url);
			return url;
		}

		private Task SendInteractionCallbackAsync(
			string interactionId,
			string interactionToken,
			object payload,
			bool allowRetry,
			CancellationToken cancellationToken)
		{
			Uri uri = CreateDiscordApiUri("/api/v10/interactions/" + interactionId + "/" + Uri.EscapeDataString(interactionToken) + "/callback");
			return SendDiscordHttpWithoutResultAsync(HttpMethod.Post, uri, payload, false, allowRetry, cancellationToken);
		}

		// 알림은 설정에 저장된 허용 채널로만 보내고 멘션을 차단합니다. 실패해도 게이트웨이 연결에는 영향을 주지 않습니다.
		public async Task PostChannelNotificationAsync(string content, CancellationToken cancellationToken)
		{
			if (!settings.NotifyServerEvents || string.IsNullOrWhiteSpace(content)) return;
			if (!IsDiscordSnowflake(settings.ChannelId)) return;
			Uri uri = CreateDiscordApiUri("/api/v10/channels/" + settings.ChannelId + "/messages");
			Dictionary<string, object> payload = new Dictionary<string, object>
			{
				{ "content", TrimDiscordNotification(content) },
				{ "allowed_mentions", new Dictionary<string, object> { { "parse", new object[0] } } }
			};
			await SendDiscordHttpWithoutResultAsync(HttpMethod.Post, uri, payload, true, true, cancellationToken).ConfigureAwait(false);
		}

		private static string TrimDiscordNotification(string content)
		{
			string value = (content ?? string.Empty).Replace('\0', ' ').Trim();
			if (value.Length > DiscordRemoteMaximumNotificationCharacters)
				value = value.Substring(0, DiscordRemoteMaximumNotificationCharacters - 3) + "...";
			return value.Length == 0 ? "-" : value;
		}

		private Task EditOriginalInteractionAsync(string interactionToken, object payload, CancellationToken cancellationToken)
		{
			Uri uri = CreateDiscordApiUri("/api/v10/webhooks/" + settings.ApplicationId + "/" + Uri.EscapeDataString(interactionToken) + "/messages/@original");
			return SendDiscordHttpWithoutResultAsync(new HttpMethod("PATCH"), uri, payload, false, true, cancellationToken);
		}

		private async Task SendDiscordHttpWithoutResultAsync(
			HttpMethod method,
			Uri uri,
			object payload,
			bool authorize,
			bool allowRetry,
			CancellationToken cancellationToken)
		{
			await SendDiscordHttpAsync(method, uri, payload, authorize, allowRetry, cancellationToken).ConfigureAwait(false);
		}

		private async Task<Dictionary<string, object>> SendDiscordHttpAsync(
			HttpMethod method,
			Uri uri,
			object payload,
			bool authorize,
			bool allowRetry,
			CancellationToken cancellationToken)
		{
			for (int attempt = 0; attempt < (allowRetry ? 2 : 1); attempt++)
			{
				using (HttpRequestMessage request = new HttpRequestMessage(method, uri))
				{
					if (authorize) request.Headers.Authorization = new AuthenticationHeaderValue("Bot", token);
					if (payload != null)
					{
						string json = new JavaScriptSerializer().Serialize(payload);
						request.Content = new StringContent(json, Encoding.UTF8, "application/json");
					}
					using (HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
					{
						string text = await ReadBoundedHttpContentAsync(response.Content, DiscordRemoteMaximumHttpBytes, cancellationToken).ConfigureAwait(false);
						if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
							throw new DiscordCredentialException("봇 토큰, 설치 상태 또는 권한을 확인해 주세요.");
						if ((int)response.StatusCode == 429)
						{
							if (allowRetry && attempt == 0)
							{
								double retrySeconds = ReadDiscordRetryAfter(response, text);
								await Task.Delay(TimeSpan.FromSeconds(Math.Max(0.25, Math.Min(60, retrySeconds))), cancellationToken).ConfigureAwait(false);
								continue;
							}
							throw new HttpRequestException("Discord API 속도 제한으로 요청을 완료하지 못했습니다.");
						}
						if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
							throw new DiscordCredentialException("애플리케이션·서버 ID, 봇 설치 상태와 권한을 확인해 주세요.");
						if (!response.IsSuccessStatusCode)
							throw new HttpRequestException("Discord API 요청이 실패했습니다. HTTP " + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));
						if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, object>();
						try { return new JavaScriptSerializer { MaxJsonLength = DiscordRemoteMaximumHttpBytes }.Deserialize<Dictionary<string, object>>(text); }
						catch (Exception exception) { throw new InvalidDataException("Discord API 응답 형식이 올바르지 않습니다.", exception); }
					}
				}
			}
			throw new HttpRequestException("Discord API 속도 제한으로 요청을 완료하지 못했습니다.");
		}

		private static double ReadDiscordRetryAfter(HttpResponseMessage response, string text)
		{
			IEnumerable<string> values;
			double parsed;
			if (response.Headers.TryGetValues("Retry-After", out values)
				&& double.TryParse(values.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
				return parsed;
			try
			{
				Dictionary<string, object> value = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(text);
				if (value != null && value.ContainsKey("retry_after"))
					return Convert.ToDouble(value["retry_after"], CultureInfo.InvariantCulture);
			}
			catch { }
			return 1;
		}

		private static async Task<string> ReadBoundedHttpContentAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
		{
			if (content == null) return string.Empty;
			if (content.Headers.ContentLength.HasValue && content.Headers.ContentLength.Value > maximumBytes)
				throw new InvalidDataException("Discord API 응답이 허용 크기를 초과했습니다.");
			using (Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
			using (MemoryStream memory = new MemoryStream())
			{
				byte[] buffer = new byte[8192];
				while (true)
				{
					int read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
					if (read <= 0) break;
					if (memory.Length + read > maximumBytes) throw new InvalidDataException("Discord API 응답이 허용 크기를 초과했습니다.");
					memory.Write(buffer, 0, read);
				}
				return Encoding.UTF8.GetString(memory.ToArray());
			}
		}

		private static Dictionary<string, object> CreateMineHarborCommandDefinition()
		{
			List<object> options = new List<object>();
			options.Add(CreateDiscordSubcommand("help", "Show MineHarbor remote-control help.", "MineHarbor 원격 제어 도움말을 표시합니다.", false));
			options.Add(CreateDiscordSubcommand("status", "Show server status.", "서버 상태를 표시합니다.", false));
			options.Add(CreateDiscordSubcommand("players", "Show online players when the bridge is connected.", "브리지 연결 시 온라인 플레이어를 표시합니다.", true));
			options.Add(CreateDiscordSubcommand("errors", "Show recent warnings and errors.", "최근 경고와 오류를 표시합니다.", true));
			options.Add(CreateDiscordSubcommand("start", "Start a stopped server.", "꺼진 서버를 시작합니다.", true));
			options.Add(CreateDiscordSubcommand("stop", "Safely stop a server after confirmation.", "확인 후 서버를 안전 종료합니다.", true));
			options.Add(CreateDiscordSubcommand("restart", "Safely restart a server after confirmation.", "확인 후 서버를 안전하게 재시작합니다.", true));
			options.Add(CreateDiscordSubcommand("backup", "Create a safe server backup.", "안전한 서버 백업을 만듭니다.", true));
			return new Dictionary<string, object>
			{
				{ "name", "mineharbor" },
				{ "type", 1 },
				{ "description", "Safely manage approved MineHarbor servers." },
				{ "description_localizations", new Dictionary<string, object> { { "ko", "허용된 MineHarbor 서버를 안전하게 관리합니다." } } },
				{ "options", options.ToArray() }
			};
		}

		private static object CreateDiscordSubcommand(string name, string description, string koreanDescription, bool serverRequired)
		{
			Dictionary<string, object> command = new Dictionary<string, object>
			{
				{ "type", 1 },
				{ "name", name },
				{ "description", description },
				{ "description_localizations", new Dictionary<string, object> { { "ko", koreanDescription } } }
			};
			if (!string.Equals(name, "help", StringComparison.Ordinal))
			{
				command["options"] = new object[]
				{
					new Dictionary<string, object>
					{
						{ "type", 3 },
						{ "name", "server" },
						{ "description", serverRequired ? "Approved server profile." : "Approved server profile; omit to show all." },
						{ "description_localizations", new Dictionary<string, object>
							{
								{ "ko", serverRequired ? "허용된 서버 프로필입니다." : "허용된 서버 프로필이며 생략하면 전체를 표시합니다." }
							}
						},
						{ "required", serverRequired },
						{ "autocomplete", true }
					}
				};
			}
			return command;
		}

		private static Uri CreateDiscordApiUri(string path)
		{
			if (string.IsNullOrEmpty(path) || !path.StartsWith("/api/v10/", StringComparison.Ordinal))
				throw new InvalidDataException("Discord API 경로가 올바르지 않습니다.");
			Uri uri = new Uri("https://discord.com" + path, UriKind.Absolute);
			if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(uri.Host, "discord.com", StringComparison.OrdinalIgnoreCase)
				|| !string.IsNullOrEmpty(uri.UserInfo)
				|| !uri.IsDefaultPort)
				throw new InvalidDataException("Discord API 주소가 허용 범위를 벗어났습니다.");
			return uri;
		}

		private static Uri CreateGatewayUri(string baseUrl)
		{
			Uri baseUri;
			if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri)
				|| !string.Equals(baseUri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)
				|| string.IsNullOrEmpty(baseUri.Host)
				|| !(string.Equals(baseUri.Host, "gateway.discord.gg", StringComparison.OrdinalIgnoreCase)
					|| baseUri.Host.EndsWith(".discord.gg", StringComparison.OrdinalIgnoreCase))
				|| !string.IsNullOrEmpty(baseUri.UserInfo)
				|| !baseUri.IsDefaultPort)
				throw new InvalidDataException("Discord Gateway 주소가 허용 범위를 벗어났습니다.");
			UriBuilder builder = new UriBuilder(baseUri);
			builder.Query = "v=10&encoding=json";
			return builder.Uri;
		}

		private static bool IsSafeDiscordInteractionToken(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length > 256) return false;
			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				if (!((character >= 'a' && character <= 'z')
					|| (character >= 'A' && character <= 'Z')
					|| (character >= '0' && character <= '9')
					|| character == '-' || character == '_' || character == '.')) return false;
			}
			return true;
		}

		private async Task<Dictionary<string, object>> ReceiveGatewayPayloadAsync(ClientWebSocket socket, CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[8192];
			using (MemoryStream memory = new MemoryStream())
			{
				while (true)
				{
					WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
					if (result.MessageType == WebSocketMessageType.Close)
					{
						int closeCode = result.CloseStatus.HasValue ? (int)result.CloseStatus.Value : 0;
						if (closeCode == 4004)
							throw new DiscordCredentialException("봇 토큰이 거부되었습니다.");
						if (closeCode == 4007 || closeCode == 4009)
						{
							sessionId = null;
							resumeGatewayUrl = null;
							sequence = null;
						}
						throw new IOException("Discord Gateway 연결이 닫혔습니다. 코드 " + closeCode.ToString(CultureInfo.InvariantCulture));
					}
					if (result.MessageType != WebSocketMessageType.Text)
						throw new InvalidDataException("Discord Gateway가 지원하지 않는 데이터 형식을 보냈습니다.");
					if (memory.Length + result.Count > DiscordRemoteMaximumGatewayBytes)
						throw new InvalidDataException("Discord Gateway 메시지가 허용 크기를 초과했습니다.");
					memory.Write(buffer, 0, result.Count);
					if (result.EndOfMessage) break;
				}
				string json = Encoding.UTF8.GetString(memory.ToArray());
				try
				{
					return new JavaScriptSerializer { MaxJsonLength = DiscordRemoteMaximumGatewayBytes }
						.Deserialize<Dictionary<string, object>>(json);
				}
				catch (Exception exception)
				{
					throw new InvalidDataException("Discord Gateway 메시지 형식이 올바르지 않습니다.", exception);
				}
			}
		}

		private async Task SendGatewayPayloadAsync(ClientWebSocket socket, object payload, CancellationToken cancellationToken)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(payload));
			if (bytes.Length > 16384) throw new InvalidDataException("Discord Gateway 송신 메시지가 너무 큽니다.");
			await gatewaySendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
			}
			finally { gatewaySendLock.Release(); }
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			gatewaySendLock.Dispose();
			http.Dispose();
		}
	}

	private static Dictionary<string, object> GetDictionary(Dictionary<string, object> source, string key)
	{
		if (source == null || !source.ContainsKey(key)) return new Dictionary<string, object>();
		Dictionary<string, object> dictionary = source[key] as Dictionary<string, object>;
		return dictionary ?? new Dictionary<string, object>();
	}

	private static List<object> GetList(Dictionary<string, object> source, string key)
	{
		if (source == null || !source.ContainsKey(key) || source[key] == null) return new List<object>();
		IEnumerable values = source[key] as IEnumerable;
		if (values == null || source[key] is string) return new List<object>();
		List<object> result = new List<object>();
		foreach (object value in values) result.Add(value);
		return result;
	}

	private static string GetString(Dictionary<string, object> source, string key)
	{
		if (source == null || !source.ContainsKey(key) || source[key] == null) return string.Empty;
		return Convert.ToString(source[key], CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static int GetInt(Dictionary<string, object> source, string key)
	{
		if (source == null || !source.ContainsKey(key) || source[key] == null) return 0;
		try { return Convert.ToInt32(source[key], CultureInfo.InvariantCulture); }
		catch { return 0; }
	}

	private sealed partial class BackgroundAgentContext
	{
		private DiscordRemoteActionResult HandleDiscordRemoteAction(DiscordRemoteAction action)
		{
			if (action == null || !IsValidProfileName(action.Profile))
				return DiscordActionFailure(action, "서버 프로필이 올바르지 않습니다.", "The server profile is invalid.");
			ManagedProfileRecord profile = FindProfile(action.Profile);
			if (profile == null)
				return DiscordActionFailure(action, "서버 프로필을 찾지 못했습니다.", "Server profile not found.");
			string command = (action.Command ?? string.Empty).Trim().ToLowerInvariant();
			// 전체 조회는 프로필마다 반복되므로 브리지 조회를 생략하고, 서버를 지정한 조회에서만 확인합니다.
			if (command == "status") return CreateDiscordStatus(profile, action.Korean, !string.IsNullOrEmpty(action.Profile) && !action.AllProfiles);
			if (command == "players") return CreateDiscordPlayers(profile, action.Korean);
			if (command == "errors") return CreateDiscordErrors(profile, action.Korean);
			BackgroundAgentResponse response;
			if (command == "start") response = StartProfile(profile.Name, false);
			else if (command == "stop" || command == "restart")
			{
				if (GetRunningSession(profile.Name) == null && IsLocalTcpPortListening(profile.Port))
					response = Failure("에이전트가 소유하지 않은 실행 중 서버는 원격 제어하지 않습니다.", "A running server not owned by the agent cannot be controlled remotely.");
				else response = StopProfile(profile.Name, command == "restart");
			}
			else if (command == "backup")
			{
				if (GetRunningSession(profile.Name) == null && IsLocalTcpPortListening(profile.Port))
					response = Failure("에이전트가 소유하지 않은 실행 중 서버는 원격 백업하지 않습니다.", "A running server not owned by the agent cannot be backed up remotely.");
				else
					response = CreateDiscordBackupResponse(profile, action.Korean);
			}
			else return DiscordActionFailure(action, "지원하지 않는 원격 명령입니다.", "Unsupported remote command.");
			RecordDiscordRemoteOperation(profile, action, response);
			return new DiscordRemoteActionResult { Success = response != null && response.Success, Message = response == null ? DiscordActionText(action.Korean, "응답이 없습니다.", "No response.") : response.Message };
		}

		// 백업은 월드 크기에 따라 오래 걸릴 수 있으므로 제한 시간까지만 기다립니다. 그 안에 끝나면 만들어진
		// 파일 이름과 크기를 알려 주고, 넘어가면 백업은 계속 진행하면서 진행 중이라고만 회신합니다.
		private BackgroundAgentResponse CreateDiscordBackupResponse(ManagedProfileRecord profile, bool korean)
		{
			Task<string> backup = StartImmediateBackupAsync(profile.Name);
			if (backup == null) return Failure("백업을 시작하지 못했습니다.", "Could not start the backup.");
			bool completed;
			try { completed = backup.Wait(TimeSpan.FromSeconds(DiscordRemoteBackupWaitSeconds)); }
			catch (Exception exception)
			{
				return Failure("백업에 실패했습니다: " + DescribeBackupFailure(exception), "Backup failed: " + DescribeBackupFailure(exception));
			}
			if (!completed)
				return Success(
					"백업이 진행 중입니다. 완료되면 운영 기록에 남습니다.",
					"The backup is still running. It will be recorded in the operations history when it finishes.");
			string path = backup.Result;
			if (string.IsNullOrWhiteSpace(path)) return Failure("백업 결과를 확인하지 못했습니다.", "Could not confirm the backup result.");
			string name = Path.GetFileName(path);
			string size = DescribeBackupSize(path);
			return Success("백업을 완료했습니다: " + name + size, "Backup completed: " + name + size);
		}

		private static string DescribeBackupFailure(Exception exception)
		{
			AggregateException aggregate = exception as AggregateException;
			Exception root = aggregate != null && aggregate.InnerExceptions.Count > 0 ? aggregate.InnerExceptions[0] : exception;
			return root.GetType().Name;
		}

		private static string DescribeBackupSize(string path)
		{
			try
			{
				FileInfo info = new FileInfo(path);
				if (!info.Exists) return string.Empty;
				double megabytes = info.Length / 1048576.0;
				return " (" + megabytes.ToString("0.0", CultureInfo.InvariantCulture) + " MB)";
			}
			catch { return string.Empty; }
		}

		private DiscordRemoteActionResult CreateDiscordStatus(ManagedProfileRecord profile, bool korean, bool includePlayers)
		{
			BackgroundAgentSession session = GetRunningSession(profile.Name);
			bool external = session == null && IsLocalTcpPortListening(profile.Port);
			string state = session != null ? session.Status : external
				? DiscordActionText(korean, "다른 프로세스에서 실행 중 · 원격 제어 불가", "Running in another process · remote control unavailable")
				: DiscordActionText(korean, "꺼짐", "Stopped");
			string uptime = string.Empty;
			if (session != null)
			{
				TimeSpan elapsed = DateTime.UtcNow - session.StartedUtc;
				uptime = DiscordActionText(korean, " · 가동 ", " · uptime ")
					+ string.Format(CultureInfo.InvariantCulture, "{0}d {1:00}:{2:00}:{3:00}", Math.Max(0, elapsed.Days), Math.Max(0, elapsed.Hours), Math.Max(0, elapsed.Minutes), Math.Max(0, elapsed.Seconds));
			}
			return new DiscordRemoteActionResult
			{
				Success = true,
				Message = "`" + profile.Name + "` · " + state + uptime + (includePlayers ? DescribeOnlinePlayers(session, korean) : string.Empty)
			};
		}

		// 명령 브리지가 연결된 경우에만 접속자 수를 덧붙이고, 확인되지 않으면 상태 문구를 그대로 둡니다.
		private string DescribeOnlinePlayers(BackgroundAgentSession session, bool korean)
		{
			if (session == null || string.IsNullOrWhiteSpace(session.ControlPipeName) || string.IsNullOrWhiteSpace(session.ControlToken))
				return string.Empty;
			ManagedChildControlResponse child = SendManagedChildControlRequest(
				session.ControlPipeName,
				NewManagedChildControlRequest(session.ControlToken, "status", null),
				1200);
			if (child == null || !child.Success || !child.PlayersAvailable) return string.Empty;
			int count = child.Players == null ? 0 : child.Players.Count;
			return DiscordActionText(korean, " · 접속 ", " · players ") + count.ToString(CultureInfo.InvariantCulture)
				+ DiscordActionText(korean, "명", string.Empty);
		}

		private DiscordRemoteActionResult CreateDiscordPlayers(ManagedProfileRecord profile, bool korean)
		{
			BackgroundAgentSession session = GetRunningSession(profile.Name);
			if (session == null)
				return new DiscordRemoteActionResult { Success = false, Message = DiscordActionText(korean, "서버가 에이전트에서 실행 중이 아닙니다.", "The server is not running under the agent.") };
			if (string.IsNullOrWhiteSpace(session.ControlPipeName) || string.IsNullOrWhiteSpace(session.ControlToken))
				return new DiscordRemoteActionResult { Success = false, Message = DiscordActionText(korean, "온라인 플레이어 정보는 현재 서버에서 지원되지 않습니다.", "Online-player information is unsupported for this server.") };
			ManagedChildControlResponse child = SendManagedChildControlRequest(
				session.ControlPipeName,
				NewManagedChildControlRequest(session.ControlToken, "status", null),
				1200);
			if (child == null || !child.Success || !child.PlayersAvailable)
				return new DiscordRemoteActionResult { Success = false, Message = DiscordActionText(korean, "명령 브리지가 연결되지 않아 온라인 플레이어를 확인할 수 없습니다.", "Online players are unavailable because the command bridge is not connected.") };
			List<string> players = child.Players == null ? new List<string>() : child.Players
				.Where(delegate(string value) { return !string.IsNullOrWhiteSpace(value); })
				.OrderBy(delegate(string value) { return value; }, StringComparer.OrdinalIgnoreCase)
				.Take(50).ToList();
			return new DiscordRemoteActionResult
			{
				Success = true,
				Message = players.Count == 0
					? DiscordActionText(korean, "`" + profile.Name + "` · 온라인 플레이어 없음", "`" + profile.Name + "` · no online players")
					: "`" + profile.Name + "` · " + players.Count.ToString(CultureInfo.InvariantCulture) + "\n" + string.Join(", ", players.ToArray())
			};
		}

		private DiscordRemoteActionResult CreateDiscordErrors(ManagedProfileRecord profile, bool korean)
		{
			BackgroundAgentResponse logs = CreateLogsResponse(profile.Name);
			if (logs == null || !logs.Success)
				return new DiscordRemoteActionResult { Success = false, Message = logs == null ? DiscordActionText(korean, "최근 로그를 확인할 수 없습니다.", "Recent logs are unavailable.") : logs.Message };
			List<string> problems = new List<string>();
			for (int index = logs.Lines.Count - 1; index >= 0 && problems.Count < 5; index--)
			{
				string line = logs.Lines[index] ?? string.Empty;
				if (line.IndexOf("warn", StringComparison.OrdinalIgnoreCase) < 0
					&& line.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0
					&& line.IndexOf("exception", StringComparison.OrdinalIgnoreCase) < 0
					&& line.IndexOf("fatal", StringComparison.OrdinalIgnoreCase) < 0
					&& line.IndexOf("crash", StringComparison.OrdinalIgnoreCase) < 0) continue;
				string sanitized = SanitizeOperationMessage(line, profile.Directory);
				if (sanitized.Length > 260) sanitized = sanitized.Substring(0, 257) + "...";
				problems.Add("• " + sanitized);
			}
			problems.Reverse();
			return new DiscordRemoteActionResult
			{
				Success = true,
				Message = problems.Count == 0
					? DiscordActionText(korean, "`" + profile.Name + "` · 최근 경고 또는 오류 없음", "`" + profile.Name + "` · no recent warnings or errors")
					: "`" + profile.Name + "`\n" + string.Join("\n", problems.ToArray())
			};
		}

		private void RecordDiscordRemoteOperation(ManagedProfileRecord profile, DiscordRemoteAction action, BackgroundAgentResponse response)
		{
			if (profile == null || action == null) return;
			string actor = MaskDiscordActor(action.ActorId);
			string ko = "Discord 원격 작업 " + action.Command + " 요청 (" + actor + "): " + (response == null ? "응답 없음" : response.Message);
			string en = "Discord remote " + action.Command + " request (" + actor + "): " + (response == null ? "No response" : response.Message);
			TryRecordOperationEvent(profile.Directory, "server", response != null && response.Success ? "info" : "warning", ko, en, "discord", false);
		}

		private static string MaskDiscordActor(string userId)
		{
			string value = userId ?? string.Empty;
			return value.Length <= 4 ? "user" : "user …" + value.Substring(value.Length - 4);
		}

		private static DiscordRemoteActionResult DiscordActionFailure(DiscordRemoteAction action, string ko, string en)
		{
			return new DiscordRemoteActionResult { Success = false, Message = DiscordActionText(action != null && action.Korean, ko, en) };
		}

		private static string DiscordActionText(bool korean, string ko, string en) { return korean ? ko : en; }
	}
}
