using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal static partial class Launcher
{
	private const long MaximumDuplicationConfigurationBytes = 4L * 1024L * 1024L;

	private sealed class DuplicationSettingsState
	{
		public bool Supported;
		public bool PistonDuplicationSupported;
		public bool GravityBlockDuplicationSupported;
		public bool TripwireDuplicationSupported;
		public bool PistonDuplication;
		public bool GravityBlockDuplication;
		public bool TripwireDuplication;
		public string ConfigurationPath;
		public bool UsesModernPaperConfiguration;
	}

	private static DuplicationSettingsState GetDuplicationSettingsState(
		string serverDirectory,
		string serverType,
		string minecraftVersion,
		bool fallbackPistonDuplication,
		bool fallbackGravityBlockDuplication,
		bool fallbackTripwireDuplication)
	{
		DuplicationSettingsState state = new DuplicationSettingsState();
		bool modern;
		string configurationPath;
		state.Supported = TryGetDuplicationConfigurationPath(serverDirectory, serverType, minecraftVersion, out configurationPath, out modern);
		state.ConfigurationPath = configurationPath;
		state.UsesModernPaperConfiguration = modern;
		if (state.Supported && File.Exists(configurationPath))
		{
			EnsureSafeDuplicationConfigurationPath(serverDirectory, configurationPath);
		}
		DetermineDuplicationCapabilities(
			serverType,
			minecraftVersion,
			configurationPath,
			modern,
			state.Supported,
			out state.PistonDuplicationSupported,
			out state.GravityBlockDuplicationSupported,
			out state.TripwireDuplicationSupported);
		state.PistonDuplication = state.PistonDuplicationSupported && fallbackPistonDuplication;
		state.GravityBlockDuplication = state.GravityBlockDuplicationSupported && fallbackGravityBlockDuplication;
		state.TripwireDuplication = state.TripwireDuplicationSupported && fallbackTripwireDuplication;

		if (!state.Supported || !File.Exists(configurationPath))
		{
			return state;
		}

		Dictionary<string, bool> values = ReadUnsupportedPaperSettings(configurationPath, modern);
		bool value;
		if (values.TryGetValue("allow-piston-duplication", out value)) state.PistonDuplication = value;
		if (values.TryGetValue("allow-unsafe-end-portal-teleportation", out value)) state.GravityBlockDuplication = value;
		if (values.TryGetValue("skip-tripwire-hook-placement-validation", out value)) state.TripwireDuplication = value;
		return state;
	}

	private static string ApplyDuplicationSettings(
		string serverDirectory,
		string serverType,
		string minecraftVersion,
		bool pistonDuplication,
		bool gravityBlockDuplication,
		bool tripwireDuplication)
	{
		bool modern;
		string configurationPath;
		if (!TryGetDuplicationConfigurationPath(serverDirectory, serverType, minecraftVersion, out configurationPath, out modern))
		{
			if (pistonDuplication || gravityBlockDuplication || tripwireDuplication)
			{
				throw new NotSupportedException("선택한 서버 종류에는 MineHarbor가 안전하게 적용할 수 있는 Paper 계열 복사 설정이 없습니다.");
			}
			return null;
		}

		if (File.Exists(configurationPath))
		{
			EnsureSafeDuplicationConfigurationPath(serverDirectory, configurationPath);
		}
		bool pistonSupported;
		bool gravitySupported;
		bool tripwireSupported;
		DetermineDuplicationCapabilities(
			serverType,
			minecraftVersion,
			configurationPath,
			modern,
			true,
			out pistonSupported,
			out gravitySupported,
			out tripwireSupported);
		if (pistonDuplication && !pistonSupported
			|| gravityBlockDuplication && !gravitySupported
			|| tripwireDuplication && !tripwireSupported)
		{
			throw new NotSupportedException("선택한 서버 버전에서 지원되지 않는 Paper 복사 설정을 적용할 수 없습니다.");
		}

		if (!File.Exists(configurationPath) && !pistonDuplication && !gravityBlockDuplication && !tripwireDuplication)
		{
			return null;
		}

		EnsureSafeDuplicationConfigurationPath(serverDirectory, configurationPath);
		if (File.Exists(configurationPath))
		{
			FileInfo existing = new FileInfo(configurationPath);
			if (existing.Length > MaximumDuplicationConfigurationBytes)
			{
				throw new InvalidDataException("Paper 설정 파일이 안전한 크기 제한을 초과했습니다.");
			}
			Dictionary<string, bool> current = ReadUnsupportedPaperSettings(configurationPath, modern);
			if (ManagedDuplicationValuesMatch(current, pistonSupported, gravitySupported, tripwireSupported, pistonDuplication, gravityBlockDuplication, tripwireDuplication))
			{
				return configurationPath;
			}
			BackupDuplicationConfigurationFile(serverDirectory, configurationPath);
		}

		Dictionary<string, bool> desired = new Dictionary<string, bool>(StringComparer.Ordinal);
		if (pistonSupported) desired.Add("allow-piston-duplication", pistonDuplication);
		if (gravitySupported) desired.Add("allow-unsafe-end-portal-teleportation", gravityBlockDuplication);
		if (tripwireSupported) desired.Add("skip-tripwire-hook-placement-validation", tripwireDuplication);
		WriteUnsupportedPaperSettings(configurationPath, modern, desired);
		return configurationPath;
	}

	private static bool ManagedDuplicationValuesMatch(
		Dictionary<string, bool> current,
		bool pistonSupported,
		bool gravitySupported,
		bool tripwireSupported,
		bool pistonDuplication,
		bool gravityBlockDuplication,
		bool tripwireDuplication)
	{
		bool value;
		if (pistonSupported && (!current.TryGetValue("allow-piston-duplication", out value) || value != pistonDuplication)) return false;
		if (gravitySupported && (!current.TryGetValue("allow-unsafe-end-portal-teleportation", out value) || value != gravityBlockDuplication)) return false;
		if (tripwireSupported && (!current.TryGetValue("skip-tripwire-hook-placement-validation", out value) || value != tripwireDuplication)) return false;
		return true;
	}

	private static void DetermineDuplicationCapabilities(
		string serverType,
		string minecraftVersion,
		string configurationPath,
		bool modern,
		bool supported,
		out bool pistonSupported,
		out bool gravitySupported,
		out bool tripwireSupported)
	{
		pistonSupported = supported;
		gravitySupported = false;
		tripwireSupported = false;
		if (!supported || !modern) return;

		string normalizedType = string.IsNullOrWhiteSpace(serverType) ? string.Empty : serverType.Trim().ToLowerInvariant();
		if (normalizedType == "paper" || normalizedType == "purpur")
		{
			gravitySupported = IsMinecraftVersionAtLeast(minecraftVersion, 1, 20, 4);
			tripwireSupported = IsMinecraftVersionAtLeast(minecraftVersion, 1, 21, 4);
		}

		// 직접 JAR 또는 알 수 없는 버전은 서버가 실제로 생성한 키만 노출합니다.
		if (File.Exists(configurationPath))
		{
			Dictionary<string, bool> existing = ReadUnsupportedPaperSettings(configurationPath, modern);
			gravitySupported = gravitySupported || existing.ContainsKey("allow-unsafe-end-portal-teleportation");
			tripwireSupported = tripwireSupported || existing.ContainsKey("skip-tripwire-hook-placement-validation");
		}
	}

	private static bool TryGetDuplicationConfigurationPath(
		string serverDirectory,
		string serverType,
		string minecraftVersion,
		out string configurationPath,
		out bool modern)
	{
		string normalizedType = string.IsNullOrWhiteSpace(serverType) ? string.Empty : serverType.Trim().ToLowerInvariant();
		string root = Path.GetFullPath(serverDirectory);
		string modernPath = Path.Combine(root, "config", "paper-global.yml");
		string legacyPath = Path.Combine(root, "paper.yml");

		if (normalizedType == "paper" || normalizedType == "purpur")
		{
			modern = IsModernPaperConfigurationVersion(minecraftVersion);
			configurationPath = modern ? modernPath : legacyPath;
			return true;
		}

		// 직접 JAR은 서버가 만든 설정 파일로 Paper 계열임을 확인한 뒤에만 수정합니다.
		if (normalizedType == "custom" && File.Exists(modernPath))
		{
			modern = true;
			configurationPath = modernPath;
			return true;
		}
		if (normalizedType == "custom" && File.Exists(legacyPath))
		{
			modern = false;
			configurationPath = legacyPath;
			return true;
		}

		modern = false;
		configurationPath = null;
		return false;
	}

	private static bool IsModernPaperConfigurationVersion(string minecraftVersion)
	{
		if (string.IsNullOrWhiteSpace(minecraftVersion)) return true;
		string[] parts = minecraftVersion.Trim().Split('.');
		int major;
		int minor;
		if (parts.Length < 2 || !int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor))
		{
			return true;
		}
		return major > 1 || major == 1 && minor >= 19;
	}

	private static bool IsMinecraftVersionAtLeast(string minecraftVersion, int requiredMajor, int requiredMinor, int requiredPatch)
	{
		if (string.IsNullOrWhiteSpace(minecraftVersion)) return false;
		string[] parts = minecraftVersion.Trim().Split('.');
		int major;
		int minor;
		int patch = 0;
		if (parts.Length < 2 || !int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor)) return false;
		if (parts.Length >= 3) int.TryParse(parts[2], out patch);
		if (major != requiredMajor) return major > requiredMajor;
		if (minor != requiredMinor) return minor > requiredMinor;
		return patch >= requiredPatch;
	}

	private static Dictionary<string, bool> ReadUnsupportedPaperSettings(string path, bool modern)
	{
		string[] lines = ReadSafeYamlLines(path);
		YamlSectionLocation location = FindUnsupportedSettingsSection(lines, modern);
		Dictionary<string, bool> result = new Dictionary<string, bool>(StringComparer.Ordinal);
		if (location.SectionStart < 0) return result;

		for (int index = location.SectionStart + 1; index < location.SectionEnd; index++)
		{
			int indent = GetYamlIndent(lines[index]);
			if (indent != location.ChildIndent) continue;
			string key;
			string valueText;
			if (!TryParseYamlEntry(lines[index], out key, out valueText) || !IsManagedDuplicationKey(key)) continue;
			if (result.ContainsKey(key)) throw new InvalidDataException("Paper 설정에 중복된 복사 설정이 있습니다: " + key);
			bool value;
			if (!bool.TryParse(RemoveYamlComment(valueText), out value))
			{
				throw new InvalidDataException("Paper 복사 설정 값이 true 또는 false가 아닙니다: " + key);
			}
			result.Add(key, value);
		}
		return result;
	}

	private static void WriteUnsupportedPaperSettings(string path, bool modern, Dictionary<string, bool> desired)
	{
		string[] source = File.Exists(path) ? ReadSafeYamlLines(path) : new string[0];
		List<string> lines = new List<string>(source);
		YamlSectionLocation location = FindUnsupportedSettingsSection(source, modern);

		if (location.SectionStart < 0)
		{
			if (!modern && location.ParentStart >= 0)
			{
				lines.Insert(location.ParentEnd, new string(' ', location.SectionIndent) + "# MineHarbor: Paper/Purpur의 비지원 복사 동작 설정");
				lines.Insert(location.ParentEnd + 1, new string(' ', location.SectionIndent) + "unsupported-settings:");
				location.SectionStart = location.ParentEnd + 1;
				location.ChildIndent = location.SectionIndent + 2;
			}
			else
			{
				if (lines.Count > 0 && lines[lines.Count - 1].Length != 0) lines.Add(string.Empty);
				lines.Add("# MineHarbor: Paper/Purpur의 비지원 복사 동작 설정");
				if (!modern)
				{
					lines.Add("settings:");
					location.SectionIndent = 2;
				}
				lines.Add(new string(' ', location.SectionIndent) + "unsupported-settings:");
				location.SectionStart = lines.Count - 1;
				location.ChildIndent = location.SectionIndent + 2;
			}
			foreach (KeyValuePair<string, bool> item in desired)
			{
				lines.Insert(++location.SectionStart, new string(' ', location.ChildIndent) + item.Key + ": " + item.Value.ToString().ToLowerInvariant());
			}
		}
		else
		{
			HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
			for (int index = location.SectionStart + 1; index < location.SectionEnd; index++)
			{
				if (GetYamlIndent(lines[index]) != location.ChildIndent) continue;
				string key;
				string valueText;
				if (!TryParseYamlEntry(lines[index], out key, out valueText) || !desired.ContainsKey(key)) continue;
				if (!found.Add(key)) throw new InvalidDataException("Paper 설정에 중복된 복사 설정이 있습니다: " + key);
				string comment = GetYamlInlineComment(valueText);
				lines[index] = new string(' ', location.ChildIndent) + key + ": " + desired[key].ToString().ToLowerInvariant() + comment;
			}
			foreach (KeyValuePair<string, bool> item in desired)
			{
				if (found.Contains(item.Key)) continue;
				lines.Insert(location.SectionEnd, new string(' ', location.ChildIndent) + item.Key + ": " + item.Value.ToString().ToLowerInvariant());
				location.SectionEnd++;
			}
		}

		string directory = Path.GetDirectoryName(path);
		Directory.CreateDirectory(directory);
		string temporary = path + ".mineharbor-" + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllLines(temporary, lines.ToArray(), new UTF8Encoding(false));
			if (new FileInfo(temporary).Length > MaximumDuplicationConfigurationBytes)
			{
				throw new InvalidDataException("수정된 Paper 설정 파일이 안전한 크기 제한을 초과했습니다.");
			}
			ReplaceFile(temporary, path);
		}
		finally
		{
			if (File.Exists(temporary)) File.Delete(temporary);
		}
	}

	private static string[] ReadSafeYamlLines(string path)
	{
		FileInfo file = new FileInfo(path);
		if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
		{
			throw new InvalidDataException("연결된 Paper 설정 파일은 수정할 수 없습니다.");
		}
		if (file.Length > MaximumDuplicationConfigurationBytes)
		{
			throw new InvalidDataException("Paper 설정 파일이 안전한 크기 제한을 초과했습니다.");
		}
		return File.ReadAllLines(path, Encoding.UTF8);
	}

	private sealed class YamlSectionLocation
	{
		public int ParentStart = -1;
		public int ParentEnd;
		public int SectionStart = -1;
		public int SectionEnd;
		public int SectionIndent;
		public int ChildIndent;
	}

	private static YamlSectionLocation FindUnsupportedSettingsSection(string[] lines, bool modern)
	{
		YamlSectionLocation location = new YamlSectionLocation();
		location.ParentEnd = lines.Length;
		location.SectionEnd = lines.Length;
		location.SectionIndent = 0;
		location.ChildIndent = 2;

		if (!modern)
		{
			for (int index = 0; index < lines.Length; index++)
			{
				string parentKey;
				string parentValue;
				if (GetYamlIndent(lines[index]) != 0 || !TryParseYamlEntry(lines[index], out parentKey, out parentValue) || parentKey != "settings") continue;
				if (location.ParentStart >= 0) throw new InvalidDataException("구형 Paper 설정에 settings 구역이 중복되어 있습니다.");
				if (RemoveYamlComment(parentValue).Length != 0) throw new InvalidDataException("구형 Paper settings 구역이 지원하지 않는 인라인 형식입니다.");
				location.ParentStart = index;
			}
			if (location.ParentStart < 0)
			{
				location.SectionIndent = 2;
				location.ChildIndent = 4;
				return location;
			}

			location.ParentEnd = FindYamlSectionEnd(lines, location.ParentStart + 1, lines.Length, 0);
			location.SectionIndent = FindMinimumYamlIndent(lines, location.ParentStart + 1, location.ParentEnd, 2);
			location.ChildIndent = location.SectionIndent + 2;
		}

		int scanStart = modern ? 0 : location.ParentStart + 1;
		int scanEnd = modern ? lines.Length : location.ParentEnd;
		for (int index = scanStart; index < scanEnd; index++)
		{
			int indent = GetYamlIndent(lines[index]);
			string key;
			string valueText;
			if (indent != location.SectionIndent || !TryParseYamlEntry(lines[index], out key, out valueText) || key != "unsupported-settings") continue;
			if (location.SectionStart >= 0) throw new InvalidDataException("Paper 설정에 unsupported-settings 구역이 중복되어 있습니다.");
			if (RemoveYamlComment(valueText).Length != 0)
			{
				throw new InvalidDataException("Paper unsupported-settings 구역이 지원하지 않는 인라인 형식입니다.");
			}
			location.SectionStart = index;
		}
		if (location.SectionStart < 0) return location;

		location.SectionEnd = FindYamlSectionEnd(
			lines,
			location.SectionStart + 1,
			modern ? lines.Length : location.ParentEnd,
			location.SectionIndent);
		location.ChildIndent = FindMinimumYamlIndent(lines, location.SectionStart + 1, location.SectionEnd, location.SectionIndent + 2);
		return location;
	}

	private static int FindYamlSectionEnd(string[] lines, int start, int end, int boundaryIndent)
	{
		int pendingTriviaStart = -1;
		for (int index = start; index < end; index++)
		{
			string trimmed = lines[index].Trim();
			if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
			{
				if (pendingTriviaStart < 0) pendingTriviaStart = index;
				continue;
			}
			if (GetYamlIndent(lines[index]) <= boundaryIndent)
			{
				return pendingTriviaStart >= 0 ? pendingTriviaStart : index;
			}
			pendingTriviaStart = -1;
		}
		return end;
	}

	private static int FindMinimumYamlIndent(string[] lines, int start, int end, int fallback)
	{
		int minimumIndent = int.MaxValue;
		for (int index = start; index < end; index++)
		{
			string trimmed = lines[index].Trim();
			if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
			minimumIndent = Math.Min(minimumIndent, GetYamlIndent(lines[index]));
		}
		return minimumIndent == int.MaxValue ? fallback : minimumIndent;
	}

	private static int GetYamlIndent(string line)
	{
		int indent = 0;
		while (indent < line.Length && line[indent] == ' ') indent++;
		if (indent < line.Length && line[indent] == '\t')
		{
			throw new InvalidDataException("탭 들여쓰기가 있는 Paper 설정은 안전하게 수정할 수 없습니다.");
		}
		return indent;
	}

	private static bool TryParseYamlEntry(string line, out string key, out string valueText)
	{
		key = null;
		valueText = null;
		string trimmed = line.TrimStart();
		if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == '-') return false;
		int separator = trimmed.IndexOf(':');
		if (separator <= 0) return false;
		key = trimmed.Substring(0, separator).Trim();
		valueText = trimmed.Substring(separator + 1).Trim();
		return key.Length > 0;
	}

	private static string RemoveYamlComment(string valueText)
	{
		int comment = valueText.IndexOf('#');
		return (comment < 0 ? valueText : valueText.Substring(0, comment)).Trim();
	}

	private static string GetYamlInlineComment(string valueText)
	{
		int comment = valueText.IndexOf('#');
		return comment < 0 ? string.Empty : " " + valueText.Substring(comment).TrimStart();
	}

	private static bool IsManagedDuplicationKey(string key)
	{
		return key == "allow-piston-duplication"
			|| key == "allow-unsafe-end-portal-teleportation"
			|| key == "skip-tripwire-hook-placement-validation";
	}

	private static void EnsureSafeDuplicationConfigurationPath(string serverDirectory, string configurationPath)
	{
		string root = Path.GetFullPath(serverDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string candidate = Path.GetFullPath(configurationPath);
		string prefix = root + Path.DirectorySeparatorChar;
		if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Paper 설정 경로가 서버 폴더 밖을 가리킵니다.");
		}

		DirectoryInfo current = new DirectoryInfo(Path.GetDirectoryName(candidate));
		while (current != null)
		{
			if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new InvalidDataException("연결된 폴더 안의 Paper 설정은 수정할 수 없습니다.");
			}
			if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase)) break;
			current = current.Parent;
		}
	}

	private static void BackupDuplicationConfigurationFile(string serverDirectory, string configurationPath)
	{
		string backupDirectory = Path.Combine(serverDirectory, ".mineharbor", "configuration-backups");
		string backupProbePath = Path.Combine(backupDirectory, "configuration-backup.probe");
		EnsureSafeDuplicationConfigurationPath(serverDirectory, backupProbePath);
		Directory.CreateDirectory(backupDirectory);
		EnsureSafeDuplicationConfigurationPath(serverDirectory, backupProbePath);
		string safeName = Path.GetFileName(configurationPath);
		string backupPath = Path.Combine(backupDirectory, safeName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".bak");
		File.Copy(configurationPath, backupPath, false);
		FileInfo[] backups = new DirectoryInfo(backupDirectory).GetFiles(safeName + "-*.bak");
		Array.Sort(backups, delegate(FileInfo left, FileInfo right) { return right.LastWriteTimeUtc.CompareTo(left.LastWriteTimeUtc); });
		for (int index = 5; index < backups.Length; index++) backups[index].Delete();
	}
}
