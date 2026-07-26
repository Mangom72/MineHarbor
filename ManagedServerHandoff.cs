using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

internal static partial class Launcher
{
	private const int ManagedChildControlSchemaVersion = 1;
	private const int ManagedChildControlRequestMaximumCharacters = 16384;
	private const int ManagedChildControlResponseMaximumCharacters = 262144;
	private const int ManagedChildControlLogMaximumLines = 4000;
	private const int ManagedChildControlLogResponseMaximumLines = 1500;
	private const int ManagedChildControlLogResponseMaximumCharacters = 196608;
	private const string ManagedChildControlPipePrefix = "MineHarbor.ManagedChild.";
	private static readonly object ManagedChildOwnerLock = new object();
	private static readonly object ManagedChildOutputLock = new object();
	private static readonly List<string> ManagedChildOutputLines = new List<string>();
	private static int ManagedChildOwnerProcessId = -1;
	private static long ManagedChildOwnerProcessStartTicks = -1;
	private static Func<string, string, int, string> ManagedChildControlTransportOverride = null;

	private sealed class ManagedChildHandoffDescriptor
	{
		public int SchemaVersion = ManagedChildControlSchemaVersion;
		public string Profile;
		public string PipeName;
		public string Token;
		public int ChildProcessId;
		public long ChildProcessStartTicks;
		public int OwnerProcessId;
		public long OwnerProcessStartTicks;
	}

	private sealed class ManagedChildControlRequest
	{
		public int SchemaVersion = ManagedChildControlSchemaVersion;
		public string Token;
		public string Command;
		public string Value;
		public int ExpectedOwnerProcessId;
		public long ExpectedOwnerProcessStartTicks;
		public int NewOwnerProcessId;
		public long NewOwnerProcessStartTicks;
	}

	private sealed class ManagedChildControlResponse
	{
		public int SchemaVersion = ManagedChildControlSchemaVersion;
		public bool Success;
		public string Message;
		public string Profile;
		public int ChildProcessId;
		public long ChildProcessStartTicks;
		public int OwnerProcessId;
		public long OwnerProcessStartTicks;
		public bool ServerRunning;
		public bool PlayersAvailable;
		public List<string> Players = new List<string>();
		public List<string> Lines = new List<string>();
	}

	private sealed class ManagedChildControlServer : IDisposable
	{
		private readonly string profileName;
		private readonly string pipeName;
		private readonly string token;
		private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
		private bool disposed;

		public ManagedChildControlServer(string profile, string pipe, string secret)
		{
			if (!IsValidProfileName(profile)) throw new InvalidDataException("관리 서버 프로필 이름이 올바르지 않습니다.");
			ValidateManagedChildControlValues(pipe, secret);
			profileName = profile;
			pipeName = pipe;
			token = secret;
		}

		public void Start()
		{
			Task.Run(async delegate
			{
				while (!cancellation.IsCancellationRequested)
				{
					NamedPipeServerStream pipe = null;
					try
					{
						pipe = CreateSecuredManagedChildPipe(pipeName);
						await pipe.WaitForConnectionAsync(cancellation.Token).ConfigureAwait(false);
						await HandleClientAsync(pipe).ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
					catch (ObjectDisposedException) { }
					catch (IOException) { }
					catch (Exception exception)
					{
						SafeWriteManagedChildDiagnostic("[ManagedControl] " + exception.GetType().Name);
						Thread.Sleep(100);
					}
					finally
					{
						if (pipe != null) pipe.Dispose();
					}
				}
			});
		}

		private async Task HandleClientAsync(NamedPipeServerStream pipe)
		{
			using (StreamReader reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true))
			using (StreamWriter writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
			{
				Task<string> readTask = reader.ReadLineAsync();
				Task completed = await Task.WhenAny(readTask, Task.Delay(3000, cancellation.Token)).ConfigureAwait(false);
				if (completed != readTask) throw new IOException("관리 서버 제어 요청 수신 시간이 초과되었습니다.");
				string line = await readTask.ConfigureAwait(false);
				ManagedChildControlResponse response;
				if (string.IsNullOrEmpty(line) || line.Length > ManagedChildControlRequestMaximumCharacters)
				{
					response = ManagedChildControlFailure("관리 서버 제어 요청 크기가 올바르지 않습니다.");
				}
				else
				{
					try
					{
						ManagedChildControlRequest request = new JavaScriptSerializer().Deserialize<ManagedChildControlRequest>(line);
						response = HandleRequest(request);
					}
					catch (Exception exception)
					{
						response = ManagedChildControlFailure(exception.Message);
					}
				}
				string serialized = new JavaScriptSerializer().Serialize(response);
				if (serialized.Length > ManagedChildControlResponseMaximumCharacters)
					serialized = new JavaScriptSerializer().Serialize(ManagedChildControlFailure("관리 서버 제어 응답이 너무 큽니다."));
				await writer.WriteLineAsync(serialized).ConfigureAwait(false);
			}
		}

		private ManagedChildControlResponse HandleRequest(ManagedChildControlRequest request)
		{
			if (request == null || request.SchemaVersion != ManagedChildControlSchemaVersion)
				return ManagedChildControlFailure("지원하지 않는 관리 서버 제어 요청입니다.");
			if (!ManagedChildTokensEqual(token, request.Token))
				return ManagedChildControlFailure("관리 서버 제어 인증에 실패했습니다.");
			string command = (request.Command ?? string.Empty).Trim().ToLowerInvariant();
			if (command == "ping" || command == "status")
				return CreateManagedChildControlStatus(profileName);
			if (command == "logs")
			{
				ManagedChildControlResponse response = CreateManagedChildControlStatus(profileName);
				response.Lines.AddRange(SnapshotManagedChildOutput());
				return response;
			}
			if (command == "command")
			{
				ValidateScheduledCommand(request.Value);
				SendServerCommand(request.Value);
				return CreateManagedChildControlStatus(profileName);
			}
			if (command == "transfer-owner")
			{
				string error;
				if (!TryTransferManagedChildOwner(
					request.ExpectedOwnerProcessId,
					request.ExpectedOwnerProcessStartTicks,
					request.NewOwnerProcessId,
					request.NewOwnerProcessStartTicks,
					out error))
					return ManagedChildControlFailure(error);
				return CreateManagedChildControlStatus(profileName);
			}
			return ManagedChildControlFailure("지원하지 않는 관리 서버 제어 명령입니다.");
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			cancellation.Cancel();
			cancellation.Dispose();
		}
	}

	private sealed class ManagedChildSafeTeeWriter : TextWriter
	{
		private readonly TextWriter primary;

		public ManagedChildSafeTeeWriter(TextWriter original)
		{
			primary = original;
		}

		public override Encoding Encoding
		{
			get { return primary == null ? Encoding.UTF8 : primary.Encoding; }
		}

		public override void Write(char value)
		{
			try { if (primary != null) primary.Write(value); } catch { }
		}

		public override void Write(string value)
		{
			try { if (primary != null) primary.Write(value); } catch { }
		}

		public override void WriteLine(string value)
		{
			CaptureManagedChildOutput(value);
			try { if (primary != null) primary.WriteLine(value); } catch { }
		}

		public override void Flush()
		{
			try { if (primary != null) primary.Flush(); } catch { }
		}
	}

	private static void ValidateManagedChildControlValues(string pipeName, string token)
	{
		if (string.IsNullOrEmpty(pipeName)
			|| pipeName.Length > 100
			|| !pipeName.StartsWith(ManagedChildControlPipePrefix, StringComparison.Ordinal)
			|| !IsLowerHex(pipeName.Substring(ManagedChildControlPipePrefix.Length), 32))
			throw new InvalidDataException("관리 서버 제어 파이프 이름이 올바르지 않습니다.");
		if (!IsLowerHex(token, 64)) throw new InvalidDataException("관리 서버 제어 토큰이 올바르지 않습니다.");
	}

	private static bool IsLowerHex(string value, int length)
	{
		if (value == null || value.Length != length) return false;
		for (int index = 0; index < value.Length; index++)
		{
			char current = value[index];
			if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f'))) return false;
		}
		return true;
	}

	private static bool ManagedChildTokensEqual(string expected, string actual)
	{
		if (expected == null || actual == null || expected.Length != actual.Length) return false;
		int difference = 0;
		for (int index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
		return difference == 0;
	}

	private static string CreateManagedChildControlPipeName()
	{
		return ManagedChildControlPipePrefix + Guid.NewGuid().ToString("N");
	}

	private static string CreateManagedChildControlToken()
	{
		byte[] bytes = new byte[32];
		using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
		StringBuilder result = new StringBuilder(64);
		for (int index = 0; index < bytes.Length; index++) result.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
		return result.ToString();
	}

	private static NamedPipeServerStream CreateSecuredManagedChildPipe(string pipeName)
	{
		PipeSecurity security = new PipeSecurity();
		SecurityIdentifier currentUser;
		using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) currentUser = identity.User;
		if (currentUser == null) throw new InvalidOperationException("현재 Windows 사용자 SID를 확인하지 못했습니다.");
		security.SetAccessRuleProtection(true, false);
		security.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));
		return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 65536, 65536, security);
	}

	private static ManagedChildControlResponse SendManagedChildControlRequest(
		string pipeName,
		ManagedChildControlRequest request,
		int timeoutMilliseconds)
	{
		if (request == null) return null;
		try
		{
			ValidateManagedChildControlValues(pipeName, request.Token);
			string serialized = new JavaScriptSerializer().Serialize(request);
			if (serialized.Length > ManagedChildControlRequestMaximumCharacters) return null;
			if (ManagedChildControlTransportOverride != null)
			{
				string overridden = ManagedChildControlTransportOverride(pipeName, serialized, timeoutMilliseconds);
				if (string.IsNullOrEmpty(overridden) || overridden.Length > ManagedChildControlResponseMaximumCharacters) return null;
				ManagedChildControlResponse overriddenResponse = new JavaScriptSerializer().Deserialize<ManagedChildControlResponse>(overridden);
				return overriddenResponse != null && overriddenResponse.SchemaVersion == ManagedChildControlSchemaVersion ? overriddenResponse : null;
			}
			using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
			{
				int timeout = Math.Max(100, Math.Min(5000, timeoutMilliseconds));
				client.Connect(timeout);
				using (StreamReader reader = new StreamReader(client, Encoding.UTF8, false, 4096, true))
				using (StreamWriter writer = new StreamWriter(client, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
				{
					Task writeTask = writer.WriteLineAsync(serialized);
					if (!writeTask.Wait(timeout)) return null;
					Task<string> readTask = reader.ReadLineAsync();
					if (!readTask.Wait(timeout)) return null;
					string line = readTask.Result;
					if (string.IsNullOrEmpty(line) || line.Length > ManagedChildControlResponseMaximumCharacters) return null;
					ManagedChildControlResponse response = new JavaScriptSerializer().Deserialize<ManagedChildControlResponse>(line);
					return response != null && response.SchemaVersion == ManagedChildControlSchemaVersion ? response : null;
				}
			}
		}
		catch
		{
			return null;
		}
	}

	private static ManagedChildControlRequest NewManagedChildControlRequest(string token, string command, string value)
	{
		return new ManagedChildControlRequest
		{
			Token = token,
			Command = command,
			Value = value
		};
	}

	private static ManagedChildControlResponse CreateManagedChildControlStatus(string profileName)
	{
		int ownerProcessId;
		long ownerProcessStartTicks;
		lock (ManagedChildOwnerLock)
		{
			ownerProcessId = ManagedChildOwnerProcessId;
			ownerProcessStartTicks = ManagedChildOwnerProcessStartTicks;
		}
		int childProcessId;
		long childProcessStartTicks;
		using (Process current = Process.GetCurrentProcess())
		{
			childProcessId = current.Id;
			childProcessStartTicks = current.StartTime.Ticks;
		}
		bool running;
		lock (ServerProcessLock)
		{
			try { running = currentServerProcess != null && !currentServerProcess.HasExited; }
			catch { running = false; }
		}
		CommandBridgeSession bridge = GetActiveCommandBridge();
		string[] players = bridge != null && bridge.Connected ? bridge.Players : new string[0];
		return new ManagedChildControlResponse
		{
			Success = true,
			Message = "ok",
			Profile = profileName,
			ChildProcessId = childProcessId,
			ChildProcessStartTicks = childProcessStartTicks,
			OwnerProcessId = ownerProcessId,
			OwnerProcessStartTicks = ownerProcessStartTicks,
			ServerRunning = running,
			PlayersAvailable = bridge != null && bridge.Connected,
			Players = new List<string>(players)
		};
	}

	private static ManagedChildControlResponse ManagedChildControlFailure(string message)
	{
		return new ManagedChildControlResponse
		{
			Success = false,
			Message = string.IsNullOrWhiteSpace(message) ? "관리 서버 제어 요청에 실패했습니다." : message
		};
	}

	private static void ConfigureManagedChildOwner(int processId, long processStartTicks)
	{
		if (processId <= 0 || processStartTicks <= 0 || !IsExactProcessIdentityAlive(processId, processStartTicks))
			throw new InvalidDataException("관리 서버 부모 프로세스를 확인하지 못했습니다.");
		lock (ManagedChildOwnerLock)
		{
			ManagedChildOwnerProcessId = processId;
			ManagedChildOwnerProcessStartTicks = processStartTicks;
		}
	}

	private static bool TryTransferManagedChildOwner(
		int expectedProcessId,
		long expectedProcessStartTicks,
		int newProcessId,
		long newProcessStartTicks,
		out string error)
	{
		error = null;
		if (expectedProcessId <= 0 || expectedProcessStartTicks <= 0
			|| newProcessId <= 0 || newProcessStartTicks <= 0)
		{
			error = "소유자 프로세스 정보가 올바르지 않습니다.";
			return false;
		}
		if (!IsExactProcessIdentityAlive(newProcessId, newProcessStartTicks))
		{
			error = "새 소유자 프로세스를 확인하지 못했습니다.";
			return false;
		}
		lock (ManagedChildOwnerLock)
		{
			if (ManagedChildOwnerProcessId != expectedProcessId
				|| ManagedChildOwnerProcessStartTicks != expectedProcessStartTicks)
			{
				error = "관리 서버의 현재 소유자가 달라 인계를 거부했습니다.";
				return false;
			}
			ManagedChildOwnerProcessId = newProcessId;
			ManagedChildOwnerProcessStartTicks = newProcessStartTicks;
			return true;
		}
	}

	private static bool IsExactProcessIdentityAlive(int processId, long processStartTicks)
	{
		if (processId <= 0 || processStartTicks <= 0) return false;
		try
		{
			using (Process process = Process.GetProcessById(processId))
				return !process.HasExited && process.StartTime.Ticks == processStartTicks;
		}
		catch
		{
			return false;
		}
	}

	private static void StartManagedChildOwnerMonitor()
	{
		Thread monitor = new Thread((ThreadStart)delegate
		{
			while (ManagedChildMode)
			{
				int ownerProcessId;
				long ownerProcessStartTicks;
				lock (ManagedChildOwnerLock)
				{
					ownerProcessId = ManagedChildOwnerProcessId;
					ownerProcessStartTicks = ManagedChildOwnerProcessStartTicks;
				}
				if (!IsExactProcessIdentityAlive(ownerProcessId, ownerProcessStartTicks))
				{
					Thread.Sleep(150);
					lock (ManagedChildOwnerLock)
					{
						if (ownerProcessId != ManagedChildOwnerProcessId
							|| ownerProcessStartTicks != ManagedChildOwnerProcessStartTicks)
							continue;
					}
					StopManagedChildAfterParentExit();
					return;
				}
				Thread.Sleep(250);
			}
		});
		monitor.IsBackground = true;
		monitor.Name = "관리 서버 소유자 감시";
		monitor.Start();
	}

	private static ManagedChildHandoffDescriptor CreateManagedChildHandoffDescriptor(ManagedServerSession session)
	{
		if (session == null || session.Profile == null || session.Process == null)
			throw new InvalidOperationException("인계할 관리 서버 세션이 없습니다.");
		ValidateManagedChildControlValues(session.ControlPipeName, session.ControlToken);
		int ownerProcessId;
		long ownerProcessStartTicks;
		using (Process current = Process.GetCurrentProcess())
		{
			ownerProcessId = current.Id;
			ownerProcessStartTicks = current.StartTime.Ticks;
		}
		return new ManagedChildHandoffDescriptor
		{
			Profile = session.Profile.Name,
			PipeName = session.ControlPipeName,
			Token = session.ControlToken,
			ChildProcessId = session.Process.Id,
			ChildProcessStartTicks = session.Process.StartTime.Ticks,
			OwnerProcessId = ownerProcessId,
			OwnerProcessStartTicks = ownerProcessStartTicks
		};
	}

	private static ManagedChildHandoffDescriptor ParseManagedChildHandoffDescriptor(string serialized, string expectedProfile)
	{
		if (string.IsNullOrEmpty(serialized) || serialized.Length > ManagedChildControlRequestMaximumCharacters)
			throw new InvalidDataException("관리 서버 인계 정보 크기가 올바르지 않습니다.");
		ManagedChildHandoffDescriptor descriptor;
		try { descriptor = new JavaScriptSerializer().Deserialize<ManagedChildHandoffDescriptor>(serialized); }
		catch (Exception exception) { throw new InvalidDataException("관리 서버 인계 정보를 읽지 못했습니다.", exception); }
		if (descriptor == null || descriptor.SchemaVersion != ManagedChildControlSchemaVersion)
			throw new InvalidDataException("지원하지 않는 관리 서버 인계 정보입니다.");
		if (!IsValidProfileName(descriptor.Profile)
			|| !string.Equals(descriptor.Profile, expectedProfile, StringComparison.OrdinalIgnoreCase))
			throw new InvalidDataException("관리 서버 인계 프로필이 일치하지 않습니다.");
		ValidateManagedChildControlValues(descriptor.PipeName, descriptor.Token);
		if (descriptor.ChildProcessId <= 0 || descriptor.ChildProcessStartTicks <= 0
			|| descriptor.OwnerProcessId <= 0 || descriptor.OwnerProcessStartTicks <= 0)
			throw new InvalidDataException("관리 서버 인계 프로세스 정보가 올바르지 않습니다.");
		return descriptor;
	}

	private static bool IsManagedChildHandoffComplete(ManagedChildHandoffDescriptor descriptor)
	{
		if (descriptor == null) return false;
		ManagedChildControlResponse response = SendManagedChildControlRequest(
			descriptor.PipeName,
			NewManagedChildControlRequest(descriptor.Token, "status", null),
			1500);
		if (response == null || !response.Success
			|| !string.Equals(response.Profile, descriptor.Profile, StringComparison.OrdinalIgnoreCase)
			|| response.ChildProcessId != descriptor.ChildProcessId
			|| response.ChildProcessStartTicks != descriptor.ChildProcessStartTicks
			|| (response.OwnerProcessId == descriptor.OwnerProcessId
				&& response.OwnerProcessStartTicks == descriptor.OwnerProcessStartTicks))
			return false;
		return IsExactProcessIdentityAlive(response.OwnerProcessId, response.OwnerProcessStartTicks);
	}

	private static void CaptureManagedChildOutput(string line)
	{
		string safeLine = line ?? string.Empty;
		if (safeLine.Length > 4096) safeLine = safeLine.Substring(0, 4096);
		lock (ManagedChildOutputLock)
		{
			ManagedChildOutputLines.Add(safeLine);
			if (ManagedChildOutputLines.Count > ManagedChildControlLogMaximumLines)
				ManagedChildOutputLines.RemoveRange(0, ManagedChildOutputLines.Count - ManagedChildControlLogMaximumLines);
		}
	}

	private static string[] SnapshotManagedChildOutput()
	{
		lock (ManagedChildOutputLock)
		{
			List<string> reversed = new List<string>();
			int characters = 0;
			for (int index = ManagedChildOutputLines.Count - 1;
				index >= 0 && reversed.Count < ManagedChildControlLogResponseMaximumLines;
				index--)
			{
				string line = ManagedChildOutputLines[index] ?? string.Empty;
				if (characters + line.Length + 2 > ManagedChildControlLogResponseMaximumCharacters) break;
				reversed.Add(line);
				characters += line.Length + 2;
			}
			reversed.Reverse();
			return reversed.ToArray();
		}
	}

	private static void SafeWriteManagedChildDiagnostic(string line)
	{
		CaptureManagedChildOutput(line);
		try { Console.Error.WriteLine(line); } catch { }
	}
}
