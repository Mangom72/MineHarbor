﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

internal static class LauncherTests
{
	private sealed class ThrowingTextWriter : TextWriter
	{
		public override Encoding Encoding { get { return Encoding.UTF8; } }
		public override void WriteLine(string value) { throw new IOException("닫힌 부모 출력 파이프"); }
	}

	private static int passed;
	private static Type launcher;

	[STAThread]
	private static int Main(string[] args)
	{
		string temporary = Path.Combine(Path.GetTempPath(), "mc-launcher-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(temporary);
		try
		{
			Assembly assembly = Assembly.LoadFrom(args[0]);
			launcher = assembly.GetType("Launcher", true);
			TestVersionSeparation(assembly);
			TestProductVersionParsing();
			TestUpdateMetadataParsing();
			TestLauncherUpdatePreferences(temporary);
			TestLauncherUpdateDirectoryCompatibility();
			TestHashAndReplacement(temporary);
			TestMockedDownload(temporary);
			TestDataLocations(temporary);
			TestSetupLayoutAndValidation(temporary);
			TestDuplicationSettings(temporary);
			TestUxAccessibility();
			TestResponsiveLauncherWorkspace();
			TestModernUiWorkflows();
			TestSecondaryDialogScaling(temporary);
			TestPlayerButtonLifecycle();
			TestModelessToolWindows();
			TestJavaSelection();
			TestConsoleClassificationAndLaunchArguments();
			TestBackupRestoreAndProfileCopy(temporary);
			TestManagedServerPortAndNetworkStatus(temporary);
			TestServerTrashLifecycle(temporary);
            TestSocketUpnpLocalServer();
			TestUpnpOwnershipRules();
			TestModrinthHash(temporary);
			TestContentManagement(temporary);
			TestServerAutomationAndDashboard(temporary);
			TestBackgroundAgentSettings(temporary);
			TestManagedServerHandoff(temporary);
			TestWindowsNotifications(temporary);
			TestDiscordRemoteManagement(temporary);
			TestOperationsHistory(temporary);
			TestDiagnosticRedaction(temporary);
			TestQuickCommandsAndBridge(temporary);
			Console.WriteLine("PASSED=" + passed);
			return 0;
		}
		catch (Exception exception)
		{
            AggregateException ae = exception as AggregateException;
            if (ae != null)
            {
                foreach (var ie in ae.InnerExceptions)
                {
                    Console.Error.WriteLine("InnerException: " + ie.GetType().FullName + ": " + ie.Message);
                    Console.Error.WriteLine(ie.StackTrace);
                }
            }
			Exception error = exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
			Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message);
			Console.Error.WriteLine(error.StackTrace);
			return 1;
		}
		finally
		{
			SetStaticField("StorageSettingsPathOverride", null);
			SetStaticField("LauncherUserDataDirectoryOverride", null);
			SetStaticField("LauncherMutexNameOverride", null);
			SetStaticField("LauncherUpdatePreferencesPathOverride", null);
			SetStaticField("BridgeArtifactOverridePath", null);
			SetStaticField("BridgeInstallFailureAfterBackup", false);
			SetStaticField("BackgroundAgentSettingsPathOverride", null);
			SetStaticField("BackgroundAgentPipeNameOverride", null);
			SetStaticField("BackgroundAgentStartupRegistrationOverride", null);
			SetStaticField("BackgroundAgentDisableTrayForTests", false);
			SetStaticField("ManagedChildControlTransportOverride", null);
			SetStaticField("ManagedChildOwnerProcessId", -1);
			SetStaticField("ManagedChildOwnerProcessStartTicks", -1L);
			SetStaticField("WindowsNotificationSettingsPathOverride", null);
			SetStaticField("WindowsNotificationDisplayOverride", null);
			SetStaticField("DiscordRemoteSettingsPathOverride", null);
			SetStaticField("DiscordRemoteRunOverride", null);
			SetStaticField("DiscordRemoteActionOverride", null);
			if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
		}
	}

	private static void TestVersionSeparation(Assembly assembly)
	{
		Type info = assembly.GetType("BuildVersionInfo", true);
		string product = Convert.ToString(info.GetField("ProductVersion").GetRawConstantValue());
		string build = Convert.ToString(info.GetField("BuildNumber").GetRawConstantValue());
		object[] productParse = { product, null };
		Equal(true, Invoke("TryParseProductVersion", productParse), "제품 버전 형식");
		Version buildVersion;
		Equal(true, Version.TryParse(build, out buildVersion), "빌드 번호 형식");
		if (string.Equals(product, build, StringComparison.Ordinal)) throw new InvalidOperationException("제품 버전과 빌드 번호가 분리되지 않았습니다.");
		Equal(buildVersion, assembly.GetName().Version, "어셈블리 빌드 번호");
		object[] productAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
		AssemblyProductAttribute productName = (AssemblyProductAttribute)productAttributes[0];
		Equal("MineHarbor — Minecraft Server Launcher", productName.Product, "제품 표시 이름");
		foreach (string resourceName in assembly.GetManifestResourceNames())
		{
			if (resourceName.IndexOf("paper.jar", StringComparison.OrdinalIgnoreCase) >= 0) throw new InvalidOperationException("Paper 서버 JAR이 실행 파일에 남아 있습니다.");
			if (resourceName.IndexOf("java25", StringComparison.OrdinalIgnoreCase) >= 0) throw new InvalidOperationException("Java 25 런타임이 실행 파일에 남아 있습니다.");
		}
		Pass();
	}

	private static void TestProductVersionParsing()
	{
		object[] valid = { "1.2.3", null };
		Equal(true, Invoke("TryParseProductVersion", valid), "정상 제품 버전");
		Equal(new Version(1, 2, 3), valid[1], "제품 버전 값");
		foreach (string invalid in new string[] { "1.2", "1.2.3.4", "v1.2.3", "1.2.x", "" })
		{
			object[] values = { invalid, null };
			Equal(false, Invoke("TryParseProductVersion", values), "잘못된 제품 버전 차단");
		}
		Pass();
	}

	private static void TestUpdateMetadataParsing()
	{
		string hash = new string('a', 64);
		Equal("Mangom72/MineHarbor", Convert.ToString(Invoke("GetLauncherReleaseRepositoryPath", new object[0])), "정식 자동 업데이트 저장소");
		string json = "{\"version\":\"0.4.2\",\"build\":\"26.2.45.30\",\"download_url\":\"https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/Minecraft-Server-Launcher.exe\",\"primary_download_url\":\"https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/MineHarbor.exe\",\"sha256\":\"" + hash + "\",\"size\":2097152,\"release_notes\":\"한국어 변경 사항\",\"release_notes_en\":\"English release notes\",\"minimum_supported_version\":\"0.1.0\"}";
		object metadata = Invoke("ParseLauncherUpdateMetadata", new object[] { json });
		Equal("0.4.2", Convert.ToString(GetField(metadata, "ProductVersion")), "업데이트 제품 버전");
		Equal("26.2.45.30", Convert.ToString(GetField(metadata, "BuildNumber")), "업데이트 빌드");
		Equal("https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/MineHarbor.exe", Convert.ToString(GetField(metadata, "Url")), "기존 버전 호환 업데이트 자산");
		Equal("한국어 변경 사항", Convert.ToString(Invoke("SelectLauncherReleaseNotes", new object[] { metadata, true })), "한국어 업데이트 변경 사항 선택");
		Equal("English release notes", Convert.ToString(Invoke("SelectLauncherReleaseNotes", new object[] { metadata, false })), "영어 업데이트 변경 사항 선택");
		string canonicalJson = json.Replace("Mangom72/mc-server-launcher", "Mangom72/MineHarbor");
		Equal("https://github.com/Mangom72/MineHarbor/releases/download/v0.4.2/MineHarbor.exe", Convert.ToString(GetField(Invoke("ParseLauncherUpdateMetadata", new object[] { canonicalJson }), "Url")), "정식 저장소 업데이트 자산");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { canonicalJson.Replace("/download/v0.4.2/", "/download/v0.4.1/") }); }, "업데이트 버전과 다른 릴리스 태그 차단");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { canonicalJson.Replace("MineHarbor.exe\"", "MineHarbor.exe?source=test\"") }); }, "쿼리가 붙은 업데이트 자산 차단");
		string legacyJson = json.Replace(",\"primary_download_url\":\"https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/MineHarbor.exe\"", string.Empty);
		object legacyMetadata = Invoke("ParseLauncherUpdateMetadata", new object[] { legacyJson });
		Equal("https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/Minecraft-Server-Launcher.exe", Convert.ToString(GetField(legacyMetadata, "Url")), "기존 업데이트 메타데이터 호환");
		Equal(true, Invoke("IsLauncherUpdateNewer", new object[] { metadata, "0.4.1", "26.2.45.29" }), "새 제품 버전 판별");
		Equal(false, Invoke("IsLauncherUpdateNewer", new object[] { metadata, "0.4.2", "26.2.45.30" }), "최신 버전 판별");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { "{}" }); }, "누락된 업데이트 메타데이터");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { json.Replace(hash, "bad") }); }, "잘못된 업데이트 해시");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { json.Replace("https://github.com/Mangom72/", "http://example.com/") }); }, "허용되지 않은 업데이트 주소");
		string compactJson = json.Replace("2097152", "300000");
		Equal("0.4.2", Convert.ToString(GetField(Invoke("ParseLauncherUpdateMetadata", new object[] { compactJson }), "ProductVersion")), "경량 런처 업데이트 허용");
		Equal("0.4.2", Convert.ToString(GetField(Invoke("ParseLauncherUpdateMetadata", new object[] { json.Replace("2097152", "1") }), "ProductVersion")), "최소 업데이트 파일 크기 제한 없음");
		ExpectFailure(delegate { Invoke("ParseLauncherUpdateMetadata", new object[] { json.Replace("2097152", "0") }); }, "빈 런처 업데이트 차단");
		string[] githubHosts = (string[])Invoke("GetGitHubReleaseDownloadHosts", new object[0]);
		Equal(true, Invoke("IsAllowedDownloadHost", new object[] { "https://release-assets.githubusercontent.com/github-production-release-asset/file", githubHosts }), "GitHub 릴리스 CDN 리디렉션 허용");
		Equal(false, Invoke("IsAllowedDownloadHost", new object[] { "https://githubusercontent.com.evil.example/file", githubHosts }), "위장 GitHub 릴리스 호스트 차단");
		Equal(true, Invoke("IsAllowedBridgeDownloadUrl", new object[] { "https://github.com/Mangom72/MineHarbor/releases/download/v0.4.2/MineHarbor-Command-Bridge-Paper-v0.4.2.jar", "0.4.2" }), "정식 명령 브리지 자산 주소");
		Equal(true, Invoke("IsAllowedBridgeDownloadUrl", new object[] { "https://github.com/Mangom72/mc-server-launcher/releases/download/v0.4.2/MineHarbor-Command-Bridge-Paper-v0.4.2.jar", "0.4.2" }), "이전 저장소 명령 브리지 자산 주소");
		Equal(false, Invoke("IsAllowedBridgeDownloadUrl", new object[] { "https://github.com/attacker/MineHarbor/releases/download/v0.4.2/MineHarbor-Command-Bridge-Paper-v0.4.2.jar", "0.4.2" }), "다른 저장소 명령 브리지 자산 차단");
		Equal(false, Invoke("IsAllowedBridgeDownloadUrl", new object[] { "https://github.com/Mangom72/MineHarbor/releases/download/v0.4.2/MineHarbor-Command-Bridge-Paper-v9.9.9.jar", "0.4.2" }), "다른 버전 명령 브리지 자산 차단");
		Equal(false, Invoke("IsAllowedBridgeDownloadUrl", new object[] { "https://github.com/Mangom72/MineHarbor/releases/download/v0.4.1/MineHarbor-Command-Bridge-Paper-v0.4.2.jar", "0.4.2" }), "다른 태그 명령 브리지 자산 차단");
		Pass();
	}

	private static void TestLauncherUpdatePreferences(string root)
	{
		string path = Path.Combine(root, "launcher-update-preferences.properties");
		SetStaticField("LauncherUpdatePreferencesPathOverride", path);
		Type type = launcher.GetNestedType("LauncherReleaseAsset", BindingFlags.NonPublic);
		object first = Activator.CreateInstance(type, true);
		SetPublic(first, "ProductVersion", "1.3.0");
		SetPublic(first, "BuildNumber", "26.2.45.35");
		Invoke("SetLauncherUpdateIgnored", new object[] { first, true });
		Equal(true, Invoke("IsLauncherUpdateIgnored", new object[] { first }), "선택한 업데이트 다시 알리지 않기 저장");
		object next = Activator.CreateInstance(type, true);
		SetPublic(next, "ProductVersion", "1.4.0");
		SetPublic(next, "BuildNumber", "26.2.45.36");
		Equal(false, Invoke("IsLauncherUpdateIgnored", new object[] { next }), "다음 업데이트는 다시 표시");
		Invoke("SetLauncherUpdateIgnored", new object[] { first, false });
		Equal(false, File.Exists(path), "업데이트 알림 숨김 해제");
		Pass();
	}

	private static void TestLauncherUpdateDirectoryCompatibility()
	{
		string current = Path.Combine(Path.GetTempPath(), "MineHarborLauncherUpdate", Guid.NewGuid().ToString("N"));
		string legacy = Path.Combine(Path.GetTempPath(), "Paper26.2LauncherUpdate", Guid.NewGuid().ToString("N"));
		string unrelated = Path.Combine(Path.GetTempPath(), "UntrustedLauncherUpdate", Guid.NewGuid().ToString("N"));
		Equal(true, Invoke("IsSafeLauncherUpdateDirectory", new object[] { current }), "현재 런처 업데이트 임시 경로");
		Equal(true, Invoke("IsSafeLauncherUpdateDirectory", new object[] { legacy }), "v0.4.2 업데이트 임시 경로 호환");
		Equal(false, Invoke("IsSafeLauncherUpdateDirectory", new object[] { unrelated }), "허용되지 않은 업데이트 임시 경로 차단");
		Equal(false, Invoke("IsSafeLauncherUpdateDirectory", new object[] { Path.Combine(Path.GetTempPath(), "MineHarborLauncherUpdate") }), "업데이트 루트 자체 차단");
		Pass();
	}

	private static void TestHashAndReplacement(string root)
	{
		string source = Path.Combine(root, "new.exe");
		string target = Path.Combine(root, "old.exe");
		File.WriteAllText(source, "new launcher", Encoding.UTF8);
		File.WriteAllText(target, "old launcher", Encoding.UTF8);
		string hash;
		using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source))).Replace("-", string.Empty);
		Invoke("ReplaceLauncherFileOnce", new object[] { source, target, hash });
		Equal(File.ReadAllText(source), File.ReadAllText(target), "검증된 런처 교체");
		File.WriteAllText(target, "old launcher", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReplaceLauncherFileOnce", new object[] { source, target, new string('0', 64) }); }, "해시 불일치 교체 차단");
		Pass();
	}

	private static void TestMockedDownload(string root)
	{
		SetStaticField("LauncherUpdateDownloadHostOverride", "127.0.0.1");
		try
		{
		byte[] payload = Encoding.UTF8.GetBytes("verified mocked launcher download");
		string completeUrl = StartSingleResponseServer(payload, payload.Length);
		object asset = CreateReleaseAsset(completeUrl, payload.Length);
		string complete = Path.Combine(root, "complete.download");
		Invoke("DownloadLauncherUpdate", new object[] { asset, complete });
		Equal(payload.Length, File.ReadAllBytes(complete).Length, "모킹된 정상 다운로드");

		string interruptedUrl = StartSingleResponseServer(payload, payload.Length + 20);
		object interruptedAsset = CreateReleaseAsset(interruptedUrl, payload.Length + 20);
		string interrupted = Path.Combine(root, "interrupted.download");
		ExpectFailure(delegate { Invoke("DownloadLauncherUpdate", new object[] { interruptedAsset, interrupted }); }, "중단된 다운로드");
		}
		finally
		{
			SetStaticField("LauncherUpdateDownloadHostOverride", null);
		}
		Pass();
	}

	private static void TestServerTrashLifecycle(string root)
	{
		string serversRoot = Path.Combine(root, "trash-lifecycle");
		string profileDirectory = Path.Combine(serversRoot, "servers", "삭제 테스트");
		Directory.CreateDirectory(profileDirectory);
		File.WriteAllText(Path.Combine(profileDirectory, "world.dat"), "world", Encoding.UTF8);
		Type profileType = launcher.GetNestedType("ManagedProfileRecord", BindingFlags.NonPublic);
		object profile = Activator.CreateInstance(profileType, true);
		SetPublic(profile, "Name", "삭제 테스트");
		SetPublic(profile, "Directory", profileDirectory);
		DateTime deletedUtc = DateTime.UtcNow.AddMinutes(-1);
		object trashed = Invoke("MoveProfileToServerTrash", new object[] { serversRoot, profile, deletedUtc });
		string trashedDirectory = Convert.ToString(GetField(trashed, "Directory"));
		Equal(false, Directory.Exists(profileDirectory), "서버를 휴지통으로 이동");
		Equal(true, File.Exists(Path.Combine(trashedDirectory, ".mineharbor-trash.json")), "휴지통 메타데이터 기록");
		IList records = (IList)Invoke("ReadServerTrashRecords", new object[] { serversRoot });
		Equal(1, records.Count, "휴지통 항목 불러오기");
		DateTime expiresUtc = (DateTime)GetField(records[0], "ExpiresUtc");
		if (Math.Abs((expiresUtc - deletedUtc).TotalDays - 30.0) > 0.01) throw new InvalidOperationException("휴지통 보관 기간이 30일이 아닙니다.");
		Invoke("RestoreServerTrashRecord", new object[] { serversRoot, records[0], "복구 테스트" });
		string restoredDirectory = Path.Combine(serversRoot, "servers", "복구 테스트");
		Equal(true, File.Exists(Path.Combine(restoredDirectory, "world.dat")), "휴지통 서버 복구");
		Equal(false, File.Exists(Path.Combine(restoredDirectory, ".mineharbor-trash.json")), "복구 후 휴지통 메타데이터 제거");

		SetPublic(profile, "Name", "복구 테스트");
		SetPublic(profile, "Directory", restoredDirectory);
		object expired = Invoke("MoveProfileToServerTrash", new object[] { serversRoot, profile, DateTime.UtcNow.AddDays(-31) });
		string expiredDirectory = Convert.ToString(GetField(expired, "Directory"));
		string unknownDirectory = Path.Combine(serversRoot, "servers-trash", "metadata-없는-폴더");
		Directory.CreateDirectory(unknownDirectory);
		Equal(1, Invoke("PurgeExpiredServerTrash", new object[] { serversRoot, DateTime.UtcNow }), "만료 서버 자동 삭제 개수");
		Equal(false, Directory.Exists(expiredDirectory), "30일 지난 서버 자동 삭제");
		Equal(true, Directory.Exists(unknownDirectory), "메타데이터 없는 폴더 보존");
		Invoke("WriteActiveProfileName", new object[] { serversRoot, "복구 테스트" });
		IList remainingProfiles = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(profileType));
		remainingProfiles.Add(profile);
		Equal(null, Invoke("UpdateActiveProfileAfterRemoval", new object[] { serversRoot, remainingProfiles, "복구 테스트" }), "마지막 서버 삭제 허용");
		Equal(false, File.Exists(Path.Combine(serversRoot, ".active-server-profile")), "마지막 서버 삭제 후 활성 프로필 해제");
		Type trashFormType = launcher.GetNestedType("ServerTrashForm", BindingFlags.NonPublic);
		using (Form trashForm = (Form)Activator.CreateInstance(trashFormType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { serversRoot }, null))
		{
			ListView trashList = (ListView)GetPrivateField(trashFormType, trashForm, "trashList");
			Button restoreButton = (Button)GetPrivateField(trashFormType, trashForm, "restoreButton");
			Button deleteButton = (Button)GetPrivateField(trashFormType, trashForm, "deleteButton");
			Equal(4, trashList.Columns.Count, "휴지통 목록 정보 열");
			if (string.IsNullOrEmpty(restoreButton.Text) || string.IsNullOrEmpty(deleteButton.Text)) throw new InvalidOperationException("휴지통 작업 버튼 문구가 없습니다.");
		}
		Pass();
	}

	private static object CreateReleaseAsset(string url, long size)
	{
		Type type = launcher.GetNestedType("LauncherReleaseAsset", BindingFlags.NonPublic);
		object asset = Activator.CreateInstance(type, true);
		type.GetField("Url", BindingFlags.Instance | BindingFlags.Public).SetValue(asset, url);
		type.GetField("Size", BindingFlags.Instance | BindingFlags.Public).SetValue(asset, size);
		return asset;
	}

	private static string StartSingleResponseServer(byte[] body, int declaredLength)
	{
		TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		Thread server = new Thread(delegate()
		{
			try
			{
				using (TcpClient client = listener.AcceptTcpClient())
				using (NetworkStream stream = client.GetStream())
				{
					byte[] request = new byte[4096];
					stream.Read(request, 0, request.Length);
					byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: " + declaredLength + "\r\nConnection: close\r\n\r\n");
					stream.Write(header, 0, header.Length);
					stream.Write(body, 0, body.Length);
					stream.Flush();
				}
			}
			finally { listener.Stop(); }
		});
		server.IsBackground = true;
		server.Start();
		return "http://127.0.0.1:" + port + "/launcher.exe";
	}

	private static void TestDataLocations(string root)
	{
		string settingsPath = Path.Combine(root, "storage.properties");
		SetStaticField("StorageSettingsPathOverride", settingsPath);
		string isolatedUserData = Path.Combine(root, "isolated-user-data");
		SetStaticField("LauncherUserDataDirectoryOverride", isolatedUserData);
		Equal(Path.GetFullPath(isolatedUserData), Invoke("GetLauncherUserDataDirectory", new object[0]), "사용자 데이터 테스트 격리");
		SetStaticField("LauncherMutexNameOverride", "Local\\MineHarbor.UiAudit." + Guid.NewGuid().ToString("N"));
		string custom = Path.Combine(root, "custom-data");
		object[] validate = { custom, null, null };
		Equal(true, Invoke("TryValidateDataRoot", validate), "사용자 지정 경로");
		Equal(Path.GetFullPath(custom), validate[1], "사용자 지정 경로 정규화");

		Invoke("SaveDataStorageSettings", new object[] { "custom", custom });
		Type settingsType = launcher.GetNestedType("DataStorageSettings", BindingFlags.NonPublic);
		object[] read = { root, null, null, null };
		Equal(true, Invoke("TryReadDataStorageSettings", read), "저장 위치 다시 불러오기");
		Equal(Path.GetFullPath(custom), read[2], "저장된 사용자 지정 위치");

		File.Delete(settingsPath);
		string portable = Path.Combine(root, "Minecraft-Servers-Data");
		Directory.CreateDirectory(portable);
		string sentinel = Path.Combine(portable, "keep.txt");
		File.WriteAllText(sentinel, "keep");
		Equal(Path.GetFullPath(portable), Invoke("ResolveServersRootDirectory", new object[] { root }), "기존 Portable 데이터 우선 사용");
		Equal(true, File.Exists(sentinel), "기존 데이터 보존");

		object[] driveRoot = { Path.GetPathRoot(root), null, null };
		Equal(false, Invoke("TryValidateDataRoot", driveRoot), "드라이브 루트 차단");
		object[] windows = { Environment.GetFolderPath(Environment.SpecialFolder.Windows), null, null };
		Equal(false, Invoke("TryValidateDataRoot", windows), "Windows 폴더 차단");
		string fileInsteadOfFolder = Path.Combine(root, "not-a-folder");
		File.WriteAllText(fileInsteadOfFolder, "x");
		object[] unavailable = { fileInsteadOfFolder, null, null };
		Equal(false, Invoke("TryValidateDataRoot", unavailable), "사용 불가능한 경로 차단");
		string installedDirectory = Path.Combine(root, "installed");
		Directory.CreateDirectory(installedDirectory);
		string installedExe = Path.Combine(installedDirectory, "MineHarbor.exe");
		File.WriteAllText(installedExe, "test");
		File.WriteAllText(Path.Combine(installedDirectory, "installed.mode"), "installed");
		Equal(true, Invoke("IsInstalledLauncherPath", new object[] { installedExe }), "설치형 경로 식별");
		Pass();
	}

	private static void TestSetupLayoutAndValidation(string root)
	{
		Type settingsType = launcher.GetNestedType("ServerSettings", BindingFlags.NonPublic);
		object settings = Activator.CreateInstance(settingsType, true);
		SetPublic(settings, "ProfileName", "테스트 서버");
		SetPublic(settings, "ServerType", "paper");
		SetPublic(settings, "MinecraftVersion", "1.21.11");
		SetPublic(settings, "Motd", "A Minecraft Server");
		SetPublic(settings, "MaxPlayers", 20);
		SetPublic(settings, "ServerPort", 25565);
		SetPublic(settings, "MemoryGb", 4);
		SetPublic(settings, "ViewDistance", 32);
		SetPublic(settings, "SimulationDistance", 32);
		SetPublic(settings, "OnlineMode", true);
		SetPublic(settings, "AutoUpdate", true);
		SetPublic(settings, "CustomJavaMajor", 21);
		SetPublic(settings, "PresetName", "보통 야생");
		SetPublic(settings, "GameMode", "survival");
		SetPublic(settings, "Difficulty", "normal");
		SetPublic(settings, "LevelType", "minecraft:normal");

		Type formType = launcher.GetNestedType("ServerSetupForm", BindingFlags.NonPublic);
		string serverDirectory = Path.Combine(root, "setup-layout-server");
		Directory.CreateDirectory(serverDirectory);
		using (Form form = (Form)Activator.CreateInstance(formType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { settings, 4, 8, false, false, serverDirectory }, null))
		{
			Panel body = null;
			foreach (Control control in form.Controls) if (control is Panel && ((Panel)control).AutoScroll) body = (Panel)control;
			if (body == null || body.AutoScrollMinSize.Height < 650) throw new InvalidOperationException("작은 화면 스크롤 영역이 없습니다.");
			Equal(0, body.AutoScrollMinSize.Width, "설정 화면 가로 스크롤 방지");
			if (form.MinimumSize.Width < 790) throw new InvalidOperationException("설정 화면 최소 폭이 콘텐츠보다 작습니다.");
			CheckBox snapshots = (CheckBox)GetPrivateField(formType, form, "includeSnapshotsBox");
			if (snapshots.Right > 700) throw new InvalidOperationException("스냅샷 옵션이 설정 콘텐츠 폭을 벗어납니다.");
			NumericUpDown port = (NumericUpDown)GetPrivateField(formType, form, "portBox");
			NumericUpDown memory = (NumericUpDown)GetPrivateField(formType, form, "memoryBox");
			Equal(1m, port.Minimum, "포트 최소값");
			Equal(65535m, port.Maximum, "포트 최대값");
			Equal(2m, memory.Minimum, "메모리 최소값");
			if (string.IsNullOrEmpty(port.AccessibleName)) throw new InvalidOperationException("포트 입력의 접근성 이름이 없습니다.");
			Label validation = (Label)GetPrivateField(formType, form, "validationLabel");
			Equal(AccessibleRole.Alert, validation.AccessibleRole, "설정 오류 접근성 역할");

			ComboBox serverType = (ComboBox)GetPrivateField(formType, form, "serverTypeBox");
			Equal("ModernComboBox", serverType.GetType().Name, "테마 대응 서버 종류 선택 상자");
			Equal(DrawMode.OwnerDrawFixed, serverType.DrawMode, "서버 종류 선택 상자 오너 드로우");
			Label rules = (Label)GetPrivateField(formType, form, "rulesLabel");
			Equal(440, rules.Top, "기본 프리셋 서버 규칙 위치");
			Panel advanced = (Panel)GetPrivateField(formType, form, "advancedPanel");
			Control duplicationGroup = (Control)GetPrivateField(formType, form, "duplicationGroup");
			if (advanced.Bottom >= duplicationGroup.Top) throw new InvalidOperationException("서버 규칙과 복사 설정 카드가 겹칩니다.");
			Equal("ModernGroupBox", duplicationGroup.GetType().Name, "현대형 복사 설정 카드");
			CheckBox pistonDuplication = (CheckBox)GetPrivateField(formType, form, "pistonDuplicationBox");
			CheckBox gravityDuplication = (CheckBox)GetPrivateField(formType, form, "gravityBlockDuplicationBox");
			CheckBox tripwireDuplication = (CheckBox)GetPrivateField(formType, form, "tripwireDuplicationBox");
			Equal("ModernCheckBox", pistonDuplication.GetType().Name, "현대형 TNT 복사 설정");
			Equal(true, pistonDuplication.Enabled, "Paper 복사 설정 활성화");
			if (pistonDuplication.Right > gravityDuplication.Left || gravityDuplication.Right > duplicationGroup.ClientSize.Width)
			{
				throw new InvalidOperationException("복사 설정 선택 항목이 서로 겹치거나 카드 경계를 벗어납니다.");
			}
			if (tripwireDuplication.Bottom > duplicationGroup.ClientSize.Height) throw new InvalidOperationException("철사덫 복사 설정이 카드 경계를 벗어납니다.");
			serverType.SelectedIndex = serverType.Items.Count - 1;
			CheckBox manual = (CheckBox)GetPrivateField(formType, form, "manualJarBox");
			ComboBox version = (ComboBox)GetPrivateField(formType, form, "versionBox");
			CheckBox automatic = (CheckBox)GetPrivateField(formType, form, "autoUpdateBox");
			Equal(false, manual.Enabled, "직접 JAR 필수 설정 표시");
			Equal(false, version.Enabled, "직접 JAR 버전 비활성화");
			Equal(false, automatic.Enabled, "직접 JAR 자동 업데이트 비활성화");
			Equal(false, pistonDuplication.Enabled, "감지되지 않은 직접 JAR의 Paper 전용 설정 비활성화");

			serverType.SelectedIndex = 0;
			Equal(true, pistonDuplication.Enabled, "Paper 선택 복귀 후 복사 설정 활성화");
			version.Items.Clear();
			version.Items.Add("1.20.1");
			version.SelectedIndex = 0;
			formType.GetMethod("ApplyVersionChoices", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { new string[] { "1.21.11", "1.20.1" }, null });
			Equal("1.20.1", Convert.ToString(version.SelectedItem), "백그라운드 버전 선택 유지");
			Equal(true, pistonDuplication.Enabled, "Paper 1.20.1 피스톤 복사 설정 지원");
			Equal(false, gravityDuplication.Enabled, "Paper 1.20.1 미지원 중력 블록 설정 비활성화");
			Equal(false, tripwireDuplication.Enabled, "Paper 1.20.1 미지원 철사덫 설정 비활성화");
		}
		Pass();
	}

	private static void TestDuplicationSettings(string root)
	{
		string paper = Path.Combine(root, "duplication-paper");
		Directory.CreateDirectory(Path.Combine(paper, "config"));
		string modernPath = Path.Combine(paper, "config", "paper-global.yml");
		File.WriteAllText(modernPath,
			"# Paper 설정 보존\r\n_version: 31\r\nunsupported-settings:\r\n  allow-piston-duplication: false # 기존 주석\r\n  perform-username-validation: true\r\nwatchdog:\r\n  early-warning-delay: 15000\r\n",
			new UTF8Encoding(false));

		string applied = Convert.ToString(Invoke("ApplyDuplicationSettings", new object[] { paper, "paper", "1.21.11", true, true, true }));
		Equal(Path.GetFullPath(modernPath), Path.GetFullPath(applied), "현대 Paper 복사 설정 경로");
		string configured = File.ReadAllText(modernPath, Encoding.UTF8);
		if (configured.IndexOf("allow-piston-duplication: true # 기존 주석", StringComparison.Ordinal) < 0) throw new InvalidOperationException("TNT·양탄자·레일 복사 설정 또는 기존 주석이 보존되지 않았습니다.");
		if (configured.IndexOf("allow-unsafe-end-portal-teleportation: true", StringComparison.Ordinal) < 0) throw new InvalidOperationException("중력 블록 복사 설정이 저장되지 않았습니다.");
		if (configured.IndexOf("skip-tripwire-hook-placement-validation: true", StringComparison.Ordinal) < 0) throw new InvalidOperationException("철사덫 복사 설정이 저장되지 않았습니다.");
		if (configured.IndexOf("perform-username-validation: true", StringComparison.Ordinal) < 0 || configured.IndexOf("watchdog:", StringComparison.Ordinal) < 0) throw new InvalidOperationException("관련 없는 Paper 설정이 손실되었습니다.");

		object state = Invoke("GetDuplicationSettingsState", new object[] { paper, "paper", "1.21.11", false, false, false });
		Equal(true, GetField(state, "Supported"), "Paper 복사 설정 지원");
		Equal(true, GetField(state, "PistonDuplicationSupported"), "현대 Paper 피스톤 복사 지원");
		Equal(true, GetField(state, "GravityBlockDuplicationSupported"), "현대 Paper 중력 블록 복사 지원");
		Equal(true, GetField(state, "TripwireDuplicationSupported"), "현대 Paper 철사덫 복사 지원");
		Equal(true, GetField(state, "PistonDuplication"), "TNT 복사 설정 다시 읽기");
		Equal(true, GetField(state, "GravityBlockDuplication"), "중력 블록 복사 설정 다시 읽기");
		Equal(true, GetField(state, "TripwireDuplication"), "철사덫 복사 설정 다시 읽기");

		Invoke("ApplyDuplicationSettings", new object[] { paper, "paper", "1.21.11", false, false, false });
		configured = File.ReadAllText(modernPath, Encoding.UTF8);
		if (Regex.Matches(configured, @"(?m)^\s*allow-piston-duplication:").Count != 1) throw new InvalidOperationException("TNT 복사 설정이 중복 저장되었습니다.");
		if (configured.IndexOf("allow-piston-duplication: false", StringComparison.Ordinal) < 0) throw new InvalidOperationException("복사 설정 비활성화가 저장되지 않았습니다.");
		string backupDirectory = Path.Combine(paper, ".mineharbor", "configuration-backups");
		if (!Directory.Exists(backupDirectory) || Directory.GetFiles(backupDirectory, "paper-global.yml-*.bak").Length == 0) throw new InvalidOperationException("Paper 설정 변경 전 백업이 없습니다.");
		int backupCount = Directory.GetFiles(backupDirectory, "paper-global.yml-*.bak").Length;
		Invoke("ApplyDuplicationSettings", new object[] { paper, "paper", "1.21.11", false, false, false });
		Equal(backupCount, Directory.GetFiles(backupDirectory, "paper-global.yml-*.bak").Length, "변경 없는 시작에서 중복 설정 백업 방지");

		Dictionary<string, string> oldProfile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "profile-name", "기존 서버" },
			{ "server-type", "paper" },
			{ "minecraft-version", "1.21.11" },
			{ "memory-gb", "4" }
		};
		object legacyOptions = Invoke("ReadLauncherOptionsFromProperties", new object[] { root, oldProfile, "기존 서버", 4, true });
		Equal(false, GetField(legacyOptions, "ManageDuplicationSettings"), "기존 프로필의 수동 Paper 설정 자동 덮어쓰기 방지");

		string legacy = Path.Combine(root, "duplication-legacy");
		Directory.CreateDirectory(legacy);
		string legacyPath = Path.Combine(legacy, "paper.yml");
		File.WriteAllText(
			legacyPath,
			"config-version: 20\r\nsettings:\r\n  verbose: false\r\n  unsupported-settings:\r\n    allow-piston-duplication: false # 구형 주석\r\n  velocity-support:\r\n    enabled: false\r\n\r\ntimings:\r\n  enabled: true\r\n",
			new UTF8Encoding(false));
		string appliedLegacyPath = Convert.ToString(Invoke("ApplyDuplicationSettings", new object[] { legacy, "purpur", "1.18.2", true, false, false }));
		Equal(legacyPath, appliedLegacyPath, "구형 Purpur paper.yml 적용 경로");
		Equal(true, File.Exists(legacyPath), "구형 Paper 설정 생성");
		string legacyContent = File.ReadAllText(legacyPath, Encoding.UTF8);
		if (!Regex.IsMatch(legacyContent, @"(?m)^\s{2}unsupported-settings:\s*$\r?\n\s{4}allow-piston-duplication: true # 구형 주석\s*$"))
		{
			throw new InvalidOperationException("구형 Paper 설정은 settings.unsupported-settings 아래에 저장되어야 합니다.");
		}
		if (legacyContent.IndexOf("velocity-support:", StringComparison.Ordinal) < 0 || legacyContent.IndexOf("timings:", StringComparison.Ordinal) < 0)
		{
			throw new InvalidOperationException("구형 Paper의 관련 없는 설정이 손실되었습니다.");
		}
		object legacyState = Invoke("GetDuplicationSettingsState", new object[] { legacy, "purpur", "1.18.2", false, false, false });
		Equal(true, GetField(legacyState, "PistonDuplicationSupported"), "구형 Paper 피스톤 복사 지원");
		Equal(false, GetField(legacyState, "GravityBlockDuplicationSupported"), "구형 Paper에 존재하지 않는 중력 블록 키 차단");
		Equal(false, GetField(legacyState, "TripwireDuplicationSupported"), "구형 Paper에 존재하지 않는 철사덫 키 차단");
		ExpectFailure(delegate { Invoke("ApplyDuplicationSettings", new object[] { legacy, "purpur", "1.18.2", true, true, false }); }, "구형 Paper에 신형 복사 키 강제 적용 차단");

		string legacyNew = Path.Combine(root, "duplication-legacy-new");
		Directory.CreateDirectory(legacyNew);
		string legacyNewPath = Convert.ToString(Invoke("ApplyDuplicationSettings", new object[] { legacyNew, "paper", "1.18.2", true, false, false }));
		string legacyNewContent = File.ReadAllText(legacyNewPath, Encoding.UTF8);
		if (!Regex.IsMatch(legacyNewContent, @"(?m)^settings:\s*$\r?\n\s{2}unsupported-settings:\s*$\r?\n\s{4}allow-piston-duplication: true\s*$"))
		{
			throw new InvalidOperationException("새 구형 Paper 서버에 중첩 설정 구조가 생성되지 않았습니다.");
		}

		string versionProbe = Path.Combine(root, "duplication-version-probe");
		Directory.CreateDirectory(versionProbe);
		object paper119 = Invoke("GetDuplicationSettingsState", new object[] { versionProbe, "paper", "1.19.4", false, false, false });
		Equal(false, GetField(paper119, "GravityBlockDuplicationSupported"), "Paper 1.19에서 후대 중력 블록 키 숨김");
		object paper1204 = Invoke("GetDuplicationSettingsState", new object[] { versionProbe, "paper", "1.20.4", false, false, false });
		Equal(true, GetField(paper1204, "GravityBlockDuplicationSupported"), "Paper 1.20.4 중력 블록 키 지원");
		Equal(false, GetField(paper1204, "TripwireDuplicationSupported"), "Paper 1.20.4에서 후대 철사덫 키 숨김");
		object paper1214 = Invoke("GetDuplicationSettingsState", new object[] { versionProbe, "paper", "1.21.4", false, false, false });
		Equal(true, GetField(paper1214, "TripwireDuplicationSupported"), "Paper 1.21.4 철사덫 키 지원");

		string spigot = Path.Combine(root, "duplication-spigot");
		Directory.CreateDirectory(spigot);
		object[] spigotProbe = { spigot, "spigot", "1.21.11", null, false };
		Equal(false, Invoke("TryGetDuplicationConfigurationPath", spigotProbe), "Spigot에 Paper 전용 설정 미적용");
		Equal(null, Invoke("ApplyDuplicationSettings", new object[] { spigot, "spigot", "1.21.11", false, false, false }), "Spigot 기본 동작 유지");
		ExpectFailure(delegate { Invoke("ApplyDuplicationSettings", new object[] { spigot, "spigot", "1.21.11", true, false, false }); }, "Spigot에 Paper 설정 강제 적용 차단");

		string custom = Path.Combine(root, "duplication-custom");
		Directory.CreateDirectory(Path.Combine(custom, "config"));
		File.WriteAllText(Path.Combine(custom, "config", "paper-global.yml"), "unsupported-settings:\r\n  allow-piston-duplication: false\r\n", new UTF8Encoding(false));
		object customState = Invoke("GetDuplicationSettingsState", new object[] { custom, "custom", "unknown", false, false, false });
		Equal(true, GetField(customState, "Supported"), "Paper 계열 직접 JAR 설정 감지");
		Equal(true, GetField(customState, "PistonDuplicationSupported"), "직접 JAR에서 감지된 기본 Paper 키 지원");
		Equal(false, GetField(customState, "GravityBlockDuplicationSupported"), "직접 JAR에 없는 신형 키 추정 금지");
		Equal(false, GetField(customState, "TripwireDuplicationSupported"), "직접 JAR에 없는 철사덫 키 추정 금지");

		string corrupt = Path.Combine(root, "duplication-corrupt");
		Directory.CreateDirectory(Path.Combine(corrupt, "config"));
		string corruptPath = Path.Combine(corrupt, "config", "paper-global.yml");
		string corruptContent = "unsupported-settings:\r\n  allow-piston-duplication: false\r\nunsupported-settings:\r\n  allow-piston-duplication: true\r\n";
		File.WriteAllText(corruptPath, corruptContent, new UTF8Encoding(false));
		ExpectFailure(delegate { Invoke("ApplyDuplicationSettings", new object[] { corrupt, "paper", "1.21.11", true, true, true }); }, "중복된 Paper 설정 구역 덮어쓰기 차단");
		Equal(corruptContent, File.ReadAllText(corruptPath, Encoding.UTF8), "손상된 Paper 설정 원본 보존");

		string duplicateKey = Path.Combine(root, "duplication-duplicate-key");
		Directory.CreateDirectory(Path.Combine(duplicateKey, "config"));
		string duplicateKeyPath = Path.Combine(duplicateKey, "config", "paper-global.yml");
		string duplicateKeyContent = "unsupported-settings:\r\n  allow-piston-duplication: false\r\n  allow-piston-duplication: true\r\n";
		File.WriteAllText(duplicateKeyPath, duplicateKeyContent, new UTF8Encoding(false));
		ExpectFailure(delegate { Invoke("ApplyDuplicationSettings", new object[] { duplicateKey, "paper", "1.21.11", true, false, false }); }, "중복된 Paper 복사 키 덮어쓰기 차단");
		Equal(duplicateKeyContent, File.ReadAllText(duplicateKeyPath, Encoding.UTF8), "중복 키가 있는 설정 원본 보존");

		string invalidValue = Path.Combine(root, "duplication-invalid-value");
		Directory.CreateDirectory(Path.Combine(invalidValue, "config"));
		string invalidValuePath = Path.Combine(invalidValue, "config", "paper-global.yml");
		string invalidValueContent = "unsupported-settings:\r\n  allow-piston-duplication: enabled\r\n";
		File.WriteAllText(invalidValuePath, invalidValueContent, new UTF8Encoding(false));
		ExpectFailure(delegate { Invoke("ApplyDuplicationSettings", new object[] { invalidValue, "paper", "1.21.11", true, false, false }); }, "잘못된 Paper 불리언 값 덮어쓰기 차단");
		Equal(invalidValueContent, File.ReadAllText(invalidValuePath, Encoding.UTF8), "잘못된 불리언 설정 원본 보존");

		for (int index = 0; index < 8; index++)
		{
			Invoke("ApplyDuplicationSettings", new object[] { paper, "paper", "1.21.11", index % 2 == 0, false, false });
		}
		Equal(5, Directory.GetFiles(backupDirectory, "paper-global.yml-*.bak").Length, "Paper 설정 백업 5개 보존");

		Type optionsType = launcher.GetNestedType("LauncherOptions", BindingFlags.NonPublic);
		object savedOptions = Activator.CreateInstance(optionsType, true);
		SetPublic(savedOptions, "ProfileName", "복사 설정 저장");
		SetPublic(savedOptions, "ServerType", "paper");
		SetPublic(savedOptions, "MinecraftVersion", "1.21.11");
		SetPublic(savedOptions, "ManualJarPath", string.Empty);
		SetPublic(savedOptions, "OwnerName", "Owner");
		SetPublic(savedOptions, "ManageDuplicationSettings", true);
		SetPublic(savedOptions, "AllowPistonDuplication", true);
		SetPublic(savedOptions, "AllowGravityBlockDuplication", true);
		SetPublic(savedOptions, "AllowTripwireDuplication", true);
		string optionsPath = Path.Combine(root, "duplication-options.properties");
		Invoke("WriteLauncherOptions", new object[] { optionsPath, savedOptions });
		Dictionary<string, string> savedProperties = (Dictionary<string, string>)Invoke("ReadSimpleProperties", new object[] { optionsPath });
		object roundTripOptions = Invoke("ReadLauncherOptionsFromProperties", new object[] { root, savedProperties, "복사 설정 저장", 4, true });
		Equal(true, GetField(roundTripOptions, "ManageDuplicationSettings"), "복사 설정 관리 플래그 왕복 저장");
		Equal(true, GetField(roundTripOptions, "AllowPistonDuplication"), "피스톤 복사 설정 왕복 저장");
		Equal(true, GetField(roundTripOptions, "AllowGravityBlockDuplication"), "중력 블록 복사 설정 왕복 저장");
		Equal(true, GetField(roundTripOptions, "AllowTripwireDuplication"), "철사덫 복사 설정 왕복 저장");
		Pass();
	}

	private static void TestUxAccessibility()
	{
		Type paletteType = launcher.GetNestedType("ThemePalette", BindingFlags.NonPublic);
		object palette = paletteType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { false });
		Color window = (Color)paletteType.GetField("Window", BindingFlags.Instance | BindingFlags.Public).GetValue(palette);
		Color muted = (Color)paletteType.GetField("Muted", BindingFlags.Instance | BindingFlags.Public).GetValue(palette);
		if (ContrastRatio(window, muted) < 4.5) throw new InvalidOperationException("라이트 모드 보조 텍스트 대비가 4.5:1보다 낮습니다.");

		Type buttonType = launcher.GetNestedType("RoundedButton", BindingFlags.NonPublic);
		using (Button button = (Button)Activator.CreateInstance(buttonType, true))
		{
			button.Enabled = false;
			Equal(Cursors.Default, button.Cursor, "비활성 버튼 커서");
			Type iconType = launcher.GetNestedType("ButtonIcon", BindingFlags.NonPublic);
			PropertyInfo iconProperty = buttonType.GetProperty("IconKind", BindingFlags.Instance | BindingFlags.Public);
			object play = Enum.Parse(iconType, "Play");
			iconProperty.SetValue(button, play, null);
			Equal("Play", Convert.ToString(iconProperty.GetValue(button, null)), "버튼 벡터 아이콘 상태");
		}
		MethodInfo ensureButtonContentFits = launcher.GetMethod("EnsureButtonContentFits", BindingFlags.Static | BindingFlags.NonPublic);
		Type managedIconType = launcher.GetNestedType("ButtonIcon", BindingFlags.NonPublic);
		PropertyInfo managedIconProperty = buttonType.GetProperty("IconKind", BindingFlags.Instance | BindingFlags.Public);
		object managedIcon = Enum.Parse(managedIconType, "Server");
		string[][] managementLabels = new string[][]
		{
			new string[] { "시작", "안전 종료", "콘솔", "새 서버", "복제", "가져오기", "이름 변경", "보관", "삭제", "휴지통", "영구 삭제", "기본 서버로", "새로고침", "이 서버 선택", "콘텐츠", "백업", "일정", "대시보드" },
			new string[] { "Start", "Stop safely", "Console", "New", "Clone", "Import", "Rename", "Archive", "Delete", "Trash", "Delete forever", "Set active", "Refresh", "Content", "Backups", "Schedules", "Dashboard" }
		};
		foreach (string[] labels in managementLabels)
		{
			foreach (string label in labels)
			{
				using (Button button = (Button)Activator.CreateInstance(buttonType, true))
				{
					button.Text = label;
					button.Width = 86;
					button.Height = 40;
					button.Font = new Font("Pretendard", 11F);
					managedIconProperty.SetValue(button, managedIcon, null);
					ensureButtonContentFits.Invoke(null, new object[] { button });
					Size measured = TextRenderer.MeasureText(button.Text, button.Font, new Size(4096, button.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
					if (measured.Width + 46 > button.Width) throw new InvalidOperationException("서버 관리 버튼 문구가 잘립니다: " + label);
					if (button.MinimumSize.Width < measured.Width + 46) throw new InvalidOperationException("서버 관리 버튼 최소 폭이 보존되지 않습니다: " + label);
				}
			}
		}

		object updateIcon = Enum.Parse(managedIconType, "Upgrade");
		using (Button updateDialogButton = (Button)Invoke("CreateLauncherUpdateDialogButton", new object[] { "지금 업데이트", 148, "primary", updateIcon, palette }))
		{
			Equal(buttonType, updateDialogButton.GetType(), "업데이트 창 공통 둥근 버튼 사용");
			Equal(44, updateDialogButton.Height, "업데이트 창 버튼 높이");
			Equal("primary", Convert.ToString(updateDialogButton.Tag), "업데이트 창 기본 동작 역할");
			Equal("Upgrade", Convert.ToString(managedIconProperty.GetValue(updateDialogButton, null)), "업데이트 창 버튼 아이콘");
		}
		Type messageDialogType = launcher.GetNestedType("MineHarborMessageDialog", BindingFlags.NonPublic);
		using (Form messageDialog = (Form)Activator.CreateInstance(messageDialogType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { "확인할 내용입니다.", "공통 대화상자", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, false }, null))
		{
			List<Button> dialogButtons = new List<Button>();
			CollectButtons(messageDialog, dialogButtons);
			Equal(3, dialogButtons.Count, "예·아니요·취소 공통 대화상자 버튼 수");
			int primaryCount = 0;
			foreach (Button dialogButton in dialogButtons)
			{
				Equal(buttonType, dialogButton.GetType(), "공통 대화상자 둥근 버튼 사용");
				if (dialogButton.Height < 44) throw new InvalidOperationException("공통 대화상자 버튼 높이가 44px보다 작습니다.");
				if (string.IsNullOrWhiteSpace(dialogButton.AccessibleName)) throw new InvalidOperationException("공통 대화상자 버튼 접근성 이름이 없습니다.");
				if (string.Equals(Convert.ToString(dialogButton.Tag), "primary", StringComparison.Ordinal)) primaryCount++;
			}
			Equal(1, primaryCount, "공통 대화상자 기본 동작 수");
			Equal(true, messageDialog.AcceptButton != null, "공통 대화상자 Enter 동작");
			Equal(true, messageDialog.CancelButton != null, "공통 대화상자 Esc 동작");
		}

		Type localizationType = launcher.GetNestedType("Localization", BindingFlags.NonPublic);
		FieldInfo languageField = localizationType.GetField("CurrentLanguage", BindingFlags.Static | BindingFlags.Public);
		object originalLanguage = languageField.GetValue(null);
		Type formType = launcher.GetNestedType("LauncherForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(formType, true))
		{
			MethodInfo applyLocalization = formType.GetMethod("ApplyLocalization", BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (string language in new string[] { "ko", "en" })
			{
				languageField.SetValue(null, language);
				applyLocalization.Invoke(form, null);
				form.PerformLayout();
				foreach (string fieldName in new string[] { "startButton", "stopButton", "settingsButton", "upgradeButton", "consoleButton", "profilesButton", "backupButton", "contentButton", "playersButton", "networkButton", "diagnosticsButton", "mainScheduleButton", "mainDashboardButton", "launcherUpdateButton" })
				{
					Button action = (Button)GetPrivateField(formType, form, fieldName);
					AssertButtonTextFits(action, language + " 메인 화면");
				}
			}
		}
		languageField.SetValue(null, originalLanguage);
		Pass();
	}

	private static void TestResponsiveLauncherWorkspace()
	{
		Type localizationType = launcher.GetNestedType("Localization", BindingFlags.NonPublic);
		FieldInfo languageField = localizationType.GetField("CurrentLanguage", BindingFlags.Static | BindingFlags.Public);
		object originalLanguage = languageField.GetValue(null);
		Type formType = launcher.GetNestedType("LauncherForm", BindingFlags.NonPublic);
		try
		{
			languageField.SetValue(null, "en");
			using (Form form = (Form)Activator.CreateInstance(formType, true))
			{
				form.Size = form.MinimumSize;
				form.CreateControl();
				formType.GetMethod("ApplyLocalization", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
				form.PerformLayout();

				Panel quickPanel = (Panel)GetPrivateField(formType, form, "quickCommandPanel");
				Label quickStatus = (Label)GetPrivateField(formType, form, "quickCommandStatus");
				Label quickSyntax = (Label)GetPrivateField(formType, form, "quickCommandSyntax");
				Control quickInput = (Control)GetPrivateField(formType, form, "quickCommandBox");
				Button quickMenu = (Button)GetPrivateField(formType, form, "quickCommandMenuButton");
				Button quickManage = (Button)GetPrivateField(formType, form, "quickCommandManageButton");
				ListBox quickSuggestions = (ListBox)GetPrivateField(formType, form, "quickCommandSuggestionList");
				Panel consolePanel = (Panel)GetPrivateField(formType, form, "consolePanel");
				TableLayoutPanel workspace = quickPanel.Parent as TableLayoutPanel;
				object consoleSuggestions = GetPrivateField(formType, form, "consoleCommandSuggestions");
				Button operationsButton = (Button)GetPrivateField(formType, form, "mainOperationsButton");
				TableLayoutPanel toolActions = operationsButton.Parent as TableLayoutPanel;
				if (workspace == null) throw new InvalidOperationException("빠른 명령과 콘솔이 분리된 작업 영역을 사용하지 않습니다.");
				if (toolActions == null || toolActions.ColumnCount != 3 || toolActions.RowCount != 3) throw new InvalidOperationException("관리 도구가 읽기 쉬운 3×3 배치를 사용하지 않습니다.");
				Equal(2, toolActions.GetColumn(operationsButton), "운영 기록 버튼 열");
				Equal(2, toolActions.GetRow(operationsButton), "운영 기록 버튼 행");
				AssertButtonTextFits(operationsButton, "영어 운영 기록");
				Equal(DockStyle.Fill, quickPanel.Dock, "빠른 명령 고정 열 채우기");
				Equal(1, workspace.GetColumn(quickPanel), "빠른 명령 오른쪽 고정 열");
				Equal(0, workspace.GetColumn(consolePanel), "콘솔 왼쪽 전용 열");
				Equal(SizeType.Absolute, workspace.ColumnStyles[1].SizeType, "빠른 명령 고정 폭 열");
				if (Math.Abs(workspace.ColumnStyles[1].Width - 410F) > 0.1F) throw new InvalidOperationException("빠른 명령 고정 폭이 구버전과 다릅니다.");
				Equal(false, quickStatus.AutoEllipsis, "빠른 명령 상태 말줄임표 제거");
				Equal(false, quickSyntax.AutoEllipsis, "빠른 명령 안내 말줄임표 제거");
				AssertSingleLineTextFits(quickStatus, "영어 빠른 명령 상태");
				AssertSingleLineTextFits(quickSyntax, "영어 빠른 명령 안내");
				AssertButtonTextFits(quickMenu, "영어 빠른 명령 선택");
				AssertButtonTextFits(quickManage, "영어 명령·브리지 관리");
				if (consoleSuggestions == null) throw new InvalidOperationException("메인 콘솔 명령 자동완성이 연결되지 않았습니다.");
				Equal("QuickCommandTokenInput", quickInput.GetType().Name, "인라인 인수 토큰 입력");
				if (string.IsNullOrWhiteSpace(quickInput.AccessibleName) || string.IsNullOrWhiteSpace(quickInput.AccessibleDescription)) throw new InvalidOperationException("단계형 빠른 명령 입력의 접근성 정보가 없습니다.");
				Equal(form, quickSuggestions.Parent, "빠른 명령 자동완성의 카드 경계 밖 표시");

				Size compactSize = form.Size;
				form.Size = new Size(form.Width, Math.Max(900, form.Height));
				form.PerformLayout();
				workspace.PerformLayout();
				Control quickInputSurface = quickInput.Parent;
				quickInputSurface.CreateControl();
				for (int suggestionIndex = 0; suggestionIndex < 20; suggestionIndex++) quickSuggestions.Items.Add("suggestion-" + suggestionIndex);
				formType.GetMethod("LayoutQuickCommandPanel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
				Point quickInputTop = form.PointToClient(quickInputSurface.PointToScreen(Point.Empty));
				if (quickSuggestions.Height <= 280) throw new InvalidOperationException("빠른 명령 자동완성 최대 높이가 충분히 확대되지 않았습니다: 높이=" + quickSuggestions.Height + ", 입력 Y=" + quickInputTop.Y + ", 창 높이=" + form.ClientSize.Height + ", 후보=" + quickSuggestions.Items.Count + ", 항목 높이=" + quickSuggestions.ItemHeight + ", 최대 크기=" + quickSuggestions.MaximumSize);
				if (quickSuggestions.Height > 430) throw new InvalidOperationException("빠른 명령 자동완성이 최대 높이를 넘습니다.");
				Rectangle quickInputBounds = new Rectangle(quickInputTop, quickInputSurface.Size);
				if (quickSuggestions.Bounds.IntersectsWith(quickInputBounds)) throw new InvalidOperationException("빠른 명령 자동완성이 입력란을 가립니다.");
				quickSuggestions.Items.Clear();
				quickSuggestions.Items.Add("single");
				formType.GetMethod("LayoutQuickCommandPanel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
				if (quickSuggestions.Height > quickSuggestions.ItemHeight + 4) throw new InvalidOperationException("후보가 적을 때 자동완성 높이가 불필요하게 큽니다.");
				form.Size = compactSize;
				form.PerformLayout();
				workspace.PerformLayout();

				int quickColumnBefore = workspace.GetColumn(quickPanel);
				consolePanel.Visible = true;
				formType.GetMethod("EnsureQuickCommandWorkspaceLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
				form.PerformLayout();
				workspace.PerformLayout();
				Equal(quickColumnBefore, workspace.GetColumn(quickPanel), "콘솔 전환 후 빠른 명령 위치 유지");
				Equal(DockStyle.Fill, quickPanel.Dock, "콘솔 전환 후 빠른 명령 고정 열 유지");
				if (workspace.GetColumn(consolePanel) == workspace.GetColumn(quickPanel)) throw new InvalidOperationException("콘솔과 빠른 명령이 같은 열에서 겹칩니다.");
				int[] workspaceWidths = workspace.GetColumnWidths();
				if (workspaceWidths.Length != 2 || workspaceWidths[0] <= 0 || Math.Abs(workspaceWidths[1] - 410) > 1) throw new InvalidOperationException("콘솔과 빠른 명령의 분리 폭이 올바르지 않습니다.");
				if (consolePanel.Bounds.IntersectsWith(quickPanel.Bounds)) throw new InvalidOperationException("콘솔이 빠른 명령 패널 뒤까지 확장되어 출력 일부를 가립니다.");
				AssertSingleLineTextFits(quickStatus, "콘솔 열림 영어 빠른 명령 상태");
				AssertButtonTextFits(quickMenu, "콘솔 열림 영어 빠른 명령 선택");
				AssertButtonTextFits(quickManage, "콘솔 열림 영어 명령·브리지 관리");
				if (quickMenu.Bounds.IntersectsWith(quickManage.Bounds)) throw new InvalidOperationException("좁은 빠른 명령 패널의 버튼이 서로 겹칩니다.");

				CheckBox wrap = (CheckBox)GetPrivateField(formType, form, "consoleWrapBox");
				ComboBox filter = (ComboBox)GetPrivateField(formType, form, "consoleFilterBox");
				TextBox search = (TextBox)GetPrivateField(formType, form, "consoleSearchBox");
				Control searchSurface = search.Parent;
				Control toolbar = wrap.Parent;
				Equal(2, toolbar.Controls.GetChildIndex(searchSurface), "콘솔 검색 도킹 순서");
				Equal(1, toolbar.Controls.GetChildIndex(filter), "콘솔 필터 도킹 순서");
				Equal(0, toolbar.Controls.GetChildIndex(wrap), "콘솔 줄 바꿈 도킹 순서");
				Size wrapText = TextRenderer.MeasureText(wrap.Text, wrap.Font, new Size(4096, Math.Max(1, wrap.Height)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
				if (wrapText.Width + 34 > wrap.Width) throw new InvalidOperationException("콘솔 줄 바꿈 문구가 표시 폭을 넘습니다.");
			}
		}
		finally
		{
			languageField.SetValue(null, originalLanguage);
		}
		Pass();
	}

	private static void AssertButtonTextFits(Button button, string context)
	{
		Size measured = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font, new Size(4096, Math.Max(1, button.Height)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		PropertyInfo iconProperty = button.GetType().GetProperty("IconKind", BindingFlags.Instance | BindingFlags.Public);
		bool hasIcon = iconProperty != null && !string.Equals(Convert.ToString(iconProperty.GetValue(button, null)), "None", StringComparison.Ordinal);
		int requiredWidth = measured.Width + (hasIcon ? 46 : 24);
		if (requiredWidth > button.Width) throw new InvalidOperationException(context + " 버튼 문구가 잘립니다: " + button.Text + " (필요 " + requiredWidth + ", 실제 " + button.Width + ")");
	}

	private static void AssertSingleLineTextFits(Label label, string context)
	{
		Size measured = TextRenderer.MeasureText(label.Text ?? string.Empty, label.Font, new Size(4096, Math.Max(1, label.Height)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		if (measured.Width > label.Width) throw new InvalidOperationException(context + " 문구가 한 줄 표시 폭을 넘습니다: " + label.Text);
	}

	private static void TestSecondaryDialogScaling(string root)
	{
		Type profileType = launcher.GetNestedType("ProfileManagerForm", BindingFlags.NonPublic);
		using (Form profile = (Form)Activator.CreateInstance(profileType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { root }, null))
		{
			Equal(AutoScaleMode.Dpi, profile.AutoScaleMode, "프로필 관리 DPI 배율");
			TableLayoutPanel layout = profile.Controls[0] as TableLayoutPanel;
			if (layout == null || layout.RowStyles[3].Height < 100F) throw new InvalidOperationException("프로필 관리 작업 버튼의 두 줄 공간이 없습니다.");
			ListView profileList = (ListView)GetPrivateField(profileType, profile, "profileList");
			if (string.IsNullOrEmpty(profileList.AccessibleName)) throw new InvalidOperationException("프로필 목록의 접근성 이름이 없습니다.");
		}

		Type backupType = launcher.GetNestedType("BackupManagerForm", BindingFlags.NonPublic);
		using (Form backup = (Form)Activator.CreateInstance(backupType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { root }, null))
		{
			Equal(AutoScaleMode.Dpi, backup.AutoScaleMode, "백업 관리 DPI 배율");
			TableLayoutPanel layout = backup.Controls[0] as TableLayoutPanel;
			if (layout == null || layout.RowStyles[3].Height < 100F) throw new InvalidOperationException("백업 관리 작업 버튼의 줄바꿈 공간이 없습니다.");
			ListView backupList = (ListView)GetPrivateField(backupType, backup, "backupList");
			if (string.IsNullOrEmpty(backupList.AccessibleName)) throw new InvalidOperationException("백업 목록의 접근성 이름이 없습니다.");
		}

		Type trashType = launcher.GetNestedType("ServerTrashForm", BindingFlags.NonPublic);
		using (Form trash = (Form)Activator.CreateInstance(trashType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { root }, null))
		{
			Equal(AutoScaleMode.Dpi, trash.AutoScaleMode, "서버 휴지통 DPI 배율");
			ListView trashList = (ListView)GetPrivateField(trashType, trash, "trashList");
			if (string.IsNullOrEmpty(trashList.AccessibleName)) throw new InvalidOperationException("휴지통 목록의 접근성 이름이 없습니다.");
		}

		Type editorType = launcher.GetNestedType("QuickCommandEditorForm", BindingFlags.NonPublic);
		using (Form editor = (Form)Activator.CreateInstance(editorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { null }, null))
		{
			Equal(AutoScaleMode.Dpi, editor.AutoScaleMode, "사용자 명령 편집 DPI 배율");
			TextBox nameBox = (TextBox)GetPrivateField(editorType, editor, "nameBox");
			if (string.IsNullOrEmpty(nameBox.AccessibleName)) throw new InvalidOperationException("사용자 명령 이름 입력의 접근성 이름이 없습니다.");
		}

		Type consentType = launcher.GetNestedType("CommandBridgeConsentForm", BindingFlags.NonPublic);
		using (Form consent = (Form)Activator.CreateInstance(consentType, true))
		{
			Equal(AutoScaleMode.Dpi, consent.AutoScaleMode, "명령 브리지 동의 DPI 배율");
			foreach (Control control in consent.Controls)
			{
				if (control.Right > consent.ClientSize.Width) throw new InvalidOperationException("명령 브리지 동의 문구가 창 경계를 벗어납니다: " + control.Text);
			}
		}
		Pass();
	}

	private static void TestModernUiWorkflows()
	{
		Type checkType = launcher.GetNestedType("ModernCheckBox", BindingFlags.NonPublic);
		Type comboType = launcher.GetNestedType("ModernComboBox", BindingFlags.NonPublic);
		Type textType = launcher.GetNestedType("ModernTextBox", BindingFlags.NonPublic);
		Type tabType = launcher.GetNestedType("ModernTabControl", BindingFlags.NonPublic);
		Type listType = launcher.GetNestedType("BufferedListView", BindingFlags.NonPublic);
		Type groupType = launcher.GetNestedType("ModernGroupBox", BindingFlags.NonPublic);
		Type roundedPanelType = launcher.GetNestedType("RoundedPanel", BindingFlags.NonPublic);
		if (checkType == null || comboType == null || textType == null || tabType == null || listType == null || groupType == null || roundedPanelType == null) throw new InvalidOperationException("현대형 공통 UI 컨트롤이 누락되었습니다.");
		using (CheckBox checkBox = (CheckBox)Activator.CreateInstance(checkType, true))
		{
			Equal(Cursors.Hand, checkBox.Cursor, "현대형 체크박스 포인터");
			if (checkBox.MinimumSize.Height < 30) throw new InvalidOperationException("현대형 체크박스 터치 영역이 너무 작습니다.");
		}
		using (ComboBox comboBox = (ComboBox)Activator.CreateInstance(comboType, true))
		{
			Equal(DrawMode.OwnerDrawFixed, comboBox.DrawMode, "현대형 드롭다운 오너 드로우");
			if (comboBox.ItemHeight < 30) throw new InvalidOperationException("현대형 드롭다운 항목 높이가 너무 작습니다.");
		}
		using (TextBox textBox = (TextBox)Activator.CreateInstance(textType, true))
		{
			textType.GetProperty("CueText").SetValue(textBox, "서버 이름 예시", null);
			Equal("서버 이름 예시", Convert.ToString(textType.GetProperty("CueText").GetValue(textBox, null)), "입력 예시 문구");
		}

		Type paletteType = launcher.GetNestedType("ThemePalette", BindingFlags.NonPublic);
		object darkPalette = paletteType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { true });
		Color darkWindow = (Color)paletteType.GetField("Window").GetValue(darkPalette);
		Color darkCard = (Color)paletteType.GetField("Card").GetValue(darkPalette);
		Color darkSecondary = (Color)paletteType.GetField("CardSecondary").GetValue(darkPalette);
		Color darkBorder = (Color)paletteType.GetField("Border").GetValue(darkPalette);
		Type launcherFormType = launcher.GetNestedType("LauncherForm", BindingFlags.NonPublic);
		FieldInfo launcherFormField = launcher.GetField("launcherForm", BindingFlags.Static | BindingFlags.NonPublic);
		object previousLauncherForm = launcherFormField.GetValue(null);
		using (Form darkOwner = (Form)Activator.CreateInstance(launcherFormType, true))
		using (Form darkDialog = new Form())
		using (RichTextBox richText = new RichTextBox())
		using (ListBox nativeList = new ListBox())
		using (CheckedListBox checkedList = new CheckedListBox())
		using (DataGridView grid = new DataGridView())
		using (Control inputSurface = (Control)Activator.CreateInstance(roundedPanelType, true))
		using (TextBox modernInput = (TextBox)Activator.CreateInstance(textType, true))
		using (Control modernGroup = (Control)Activator.CreateInstance(groupType, true))
		{
			try
			{
				launcherFormType.GetField("darkTheme", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(darkOwner, true);
				launcherFormField.SetValue(null, darkOwner);
				inputSurface.Tag = "input-surface";
				inputSurface.Controls.Add(modernInput);
				darkDialog.Controls.Add(richText);
				darkDialog.Controls.Add(nativeList);
				darkDialog.Controls.Add(checkedList);
				darkDialog.Controls.Add(grid);
				darkDialog.Controls.Add(inputSurface);
				darkDialog.Controls.Add(modernGroup);
				Invoke("ApplySimpleDialogTheme", new object[] { darkDialog });
				Equal(darkWindow, darkDialog.BackColor, "보조 창 다크 배경");
				Equal(darkCard, richText.BackColor, "다크 리치 텍스트 표면");
				Equal(darkCard, nativeList.BackColor, "다크 목록 표면");
				Equal(darkCard, checkedList.BackColor, "다크 체크 목록 표면");
				Equal(darkSecondary, inputSurface.BackColor, "다크 입력 컨테이너 표면");
				Equal(darkSecondary, modernInput.BackColor, "다크 입력 필드 표면");
				Equal(darkBorder, (Color)roundedPanelType.GetProperty("BorderColor").GetValue(inputSurface, null), "다크 입력 컨테이너 테두리");
				Equal(darkCard, modernGroup.BackColor, "다크 그룹 표면");
				Equal(darkBorder, (Color)groupType.GetProperty("BorderColor").GetValue(modernGroup, null), "다크 그룹 테두리");
				Equal(false, grid.EnableHeadersVisualStyles, "다크 표 기본 헤더 테마 차단");
				Equal(darkSecondary, grid.ColumnHeadersDefaultCellStyle.BackColor, "다크 표 헤더 표면");
				Equal(BorderStyle.None, grid.BorderStyle, "다크 표 기본 외곽선 제거");
			}
			finally
			{
				launcherFormField.SetValue(null, previousLauncherForm);
			}
		}

		string[] playerSuggestions = (string[])Invoke("GetPlayerNameAutoCompleteCandidates", new object[] { "Al", new string[] { "Zed", "Alice", "alex" } });
		Equal(2, playerSuggestions.Length, "플레이어 자동완성 필터");
		Equal("alex", playerSuggestions[0], "플레이어 자동완성 정렬");
		string[] commandSuggestions = (string[])Invoke("GetManagedCommandAutoCompleteCandidates", new object[] { "kick A", new string[] { "Zed", "Alice", "alex" } });
		Equal("kick alex", commandSuggestions[0], "관리 콘솔 플레이어 명령 자동완성");

		DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		Equal(3, Invoke("GetTimedConfirmationRemainingSeconds", new object[] { start, start, 3 }), "영구 삭제 확인 최초 대기");
		Equal(2, Invoke("GetTimedConfirmationRemainingSeconds", new object[] { start, start.AddSeconds(1.2), 3 }), "영구 삭제 확인 진행 대기");
		Equal(0, Invoke("GetTimedConfirmationRemainingSeconds", new object[] { start, start.AddSeconds(3), 3 }), "영구 삭제 확인 해제");

		MethodInfo guardActive = launcherFormType.GetMethod("IsOwnedWindowClickGuardActive", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo mouseMessage = launcherFormType.GetMethod("IsMouseClickMessage", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo mouseRelease = launcherFormType.GetMethod("IsMouseReleaseMessage", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo titleBarClose = launcherFormType.GetMethod("IsTitleBarCloseMessage", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo guardDuration = launcherFormType.GetMethod("GetOwnedWindowClickGuardDuration", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo guardedPoint = launcherFormType.GetMethod("IsPointWithinOwnedWindowClickGuard", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo closeQuestion = launcherFormType.GetMethod("GetLauncherCloseQuestionKey", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo deferredClose = launcherFormType.GetMethod("RequiresDeferredLauncherClose", BindingFlags.Static | BindingFlags.NonPublic);
		Equal(true, guardActive.Invoke(null, new object[] { 100, 350 }), "보조 창 닫기 클릭 관통 보호 활성");
		Equal(false, guardActive.Invoke(null, new object[] { 350, 350 }), "보조 창 닫기 클릭 관통 보호 만료");
		Equal(650, guardDuration.Invoke(null, new object[] { 500 }), "시스템 더블클릭 시간을 포함한 추가 클릭 보호");
		Equal(500, guardDuration.Invoke(null, new object[] { 100 }), "추가 클릭 보호 최소 시간");
		Equal(1200, guardDuration.Invoke(null, new object[] { 5000 }), "추가 클릭 보호 최대 시간");
		Equal(true, guardedPoint.Invoke(null, new object[] { new Point(100, 100), new Point(164, 36), 64 }), "닫기 좌표 주변 추가 클릭 차단");
		Equal(false, guardedPoint.Invoke(null, new object[] { new Point(100, 100), new Point(165, 100), 64 }), "닫기 좌표에서 벗어난 클릭 허용");
		Equal(true, mouseMessage.Invoke(null, new object[] { 0x0201 }), "마우스 클릭 메시지 식별");
		Equal(true, mouseMessage.Invoke(null, new object[] { 0x020B }), "X 버튼 클릭 메시지 식별");
		Equal(true, mouseMessage.Invoke(null, new object[] { 0x00A2 }), "비클라이언트 클릭 메시지 식별");
		Equal(true, mouseMessage.Invoke(null, new object[] { 0x0246 }), "포인터 클릭 메시지 식별");
		Equal(false, mouseMessage.Invoke(null, new object[] { 0x0100 }), "키보드 메시지 비차단");
		Equal(true, mouseRelease.Invoke(null, new object[] { 0x0202 }), "마우스 해제 메시지 식별");
		Equal(true, mouseRelease.Invoke(null, new object[] { 0x00A2 }), "비클라이언트 해제 메시지 식별");
		Equal(false, mouseRelease.Invoke(null, new object[] { 0x0201 }), "마우스 누름 메시지 비해제");
		Equal(true, titleBarClose.Invoke(null, new object[] { 0x00A1, new IntPtr(20) }), "제목 표시줄 닫기 입력 식별");
		Equal(false, titleBarClose.Invoke(null, new object[] { 0x00A1, new IntPtr(8) }), "제목 표시줄 최소화 입력 제외");
		Equal("Close.IdleQuestion", closeQuestion.Invoke(null, new object[] { false, false }), "서버가 꺼진 런처 종료 확인");
		Equal("Close.WorkQuestion", closeQuestion.Invoke(null, new object[] { true, false }), "작업 중 런처 종료 확인");
		Equal("Close.ServerQuestion", closeQuestion.Invoke(null, new object[] { false, true }), "서버 실행 중 런처 종료 확인");
		Equal(false, deferredClose.Invoke(null, new object[] { false, false }), "일반 종료에 서버 종료 프로세스 미사용");
		Equal(true, deferredClose.Invoke(null, new object[] { false, true }), "서버 실행 중 안전 종료 대기");

		Type profileType = launcher.GetNestedType("ManagedProfileRecord", BindingFlags.NonPublic);
		object profile = Activator.CreateInstance(profileType, true);
		SetPublic(profile, "Name", "UI 테스트");
		SetPublic(profile, "Directory", Path.GetTempPath());
		Type dashboardType = launcher.GetNestedType("ServerStatusDashboardForm", BindingFlags.NonPublic);
		using (Form dashboard = (Form)Activator.CreateInstance(dashboardType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { profile, null }, null))
		{
			TableLayoutPanel values = (TableLayoutPanel)GetPrivateField(dashboardType, dashboard, "values");
			Equal("ModernMetricTable", values.GetType().Name, "대시보드 현대형 상태 표");
			Equal(false, values.AutoScroll, "대시보드 불필요한 스크롤 제거");
			Equal(TableLayoutPanelCellBorderStyle.None, values.CellBorderStyle, "대시보드 과한 격자 제거");
		}
		Type sessionType = launcher.GetNestedType("ManagedServerSession", BindingFlags.NonPublic);
		object session = Activator.CreateInstance(sessionType, true);
		SetPublic(session, "Profile", profile);
		SetPublic(session, "Status", "중지됨");
		Type managedConsoleType = launcher.GetNestedType("ManagedConsoleForm", BindingFlags.NonPublic);
		using (Form managedConsole = (Form)Activator.CreateInstance(managedConsoleType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { session }, null))
		{
			managedConsole.PerformLayout();
			TextBox managedSearch = (TextBox)GetPrivateField(managedConsoleType, managedConsole, "searchBox");
			TextBox managedCommand = (TextBox)GetPrivateField(managedConsoleType, managedConsole, "commandBox");
			ComboBox managedFilter = (ComboBox)GetPrivateField(managedConsoleType, managedConsole, "filterBox");
			RichTextBox managedOutput = (RichTextBox)GetPrivateField(managedConsoleType, managedConsole, "outputBox");
			Control toolbar = managedSearch.Parent.Parent;
			Control commandPanel = managedCommand.Parent.Parent;
			if (managedSearch.Parent.Right > managedFilter.Left) throw new InvalidOperationException("관리 콘솔 검색 입력과 필터가 겹칩니다.");
			if (managedOutput.Top < toolbar.Bottom || managedOutput.Bottom > commandPanel.Top) throw new InvalidOperationException("관리 콘솔 출력 영역이 도구막대 또는 명령 영역과 겹칩니다.");
		}
		Pass();
	}

	private static void TestPlayerButtonLifecycle()
	{
		Type formType = launcher.GetNestedType("LauncherForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(formType, true))
		{
			Button playersButton = (Button)GetPrivateField(formType, form, "playersButton");
			Equal(false, playersButton.Enabled, "서버 시작 전 플레이어 버튼 비활성화");
			formType.GetMethod("ServerStarted", BindingFlags.Instance | BindingFlags.Public).Invoke(form, new object[] { "127.0.0.1:25565" });
			Equal(true, playersButton.Enabled, "서버 시작 후 플레이어 버튼 활성화");
			formType.GetMethod("WorkflowFinished", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { 0, false });
			Equal(false, playersButton.Enabled, "서버 종료 후 플레이어 버튼 비활성화");
		}
		Pass();
	}

	private static void TestModelessToolWindows()
	{
		Type formType = launcher.GetNestedType("LauncherForm", BindingFlags.NonPublic);
		using (Form owner = (Form)Activator.CreateInstance(formType, true))
		{
			MethodInfo showTool = formType.GetMethod("ShowModelessToolWindow", BindingFlags.Instance | BindingFlags.NonPublic);
			MethodInfo ensureSafe = formType.GetMethod("EnsureNoBlockingToolWindow", BindingFlags.Instance | BindingFlags.NonPublic);
			MethodInfo filter = formType.GetMethod("PreFilterMessage", BindingFlags.Instance | BindingFlags.Public);
			MethodInfo windowProcedure = formType.GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic);
			int created = 0;
			int closed = 0;
			Form child = null;
			Func<Form> factory = delegate
			{
				created++;
				child = new Form();
				child.Text = "모델리스 테스트";
				return child;
			};
			Action onClosed = delegate { closed++; };
			showTool.Invoke(owner, new object[] { "test-tool", factory, true, onClosed });
			Equal(true, owner.Enabled, "기능 창을 연 뒤 메인 창 활성 상태 유지");
			Equal(true, child.Visible, "기능 창 모델리스 표시");
			Equal(1, created, "기능 창 최초 생성");
			showTool.Invoke(owner, new object[] { "test-tool", factory, true, onClosed });
			Equal(1, created, "같은 기능 창 중복 생성 방지");
			Equal(false, ensureSafe.Invoke(owner, null), "관리 창이 열린 동안 서버 변경 차단");
			Message closeDown = Message.Create(child.Handle, 0x00A1, new IntPtr(20), IntPtr.Zero);
			object[] closeArguments = new object[] { closeDown };
			Equal(false, filter.Invoke(owner, closeArguments), "보조 창 닫기 입력 자체는 유지");
			Message mouseActivate = Message.Create(owner.Handle, 0x0021, IntPtr.Zero, IntPtr.Zero);
			object[] activationArguments = new object[] { mouseActivate };
			windowProcedure.Invoke(owner, activationArguments);
			Equal(new IntPtr(4), ((Message)activationArguments[0]).Result, "보조 창 닫기 중 주 창 활성화와 클릭 소비");
			Button underlyingButton = new Button();
			owner.Controls.Add(underlyingButton);
			Message underlyingDown = Message.Create(underlyingButton.Handle, 0x0201, IntPtr.Zero, IntPtr.Zero);
			object[] downArguments = new object[] { underlyingDown };
			Equal(true, filter.Invoke(owner, downArguments), "보조 창 닫기 중 주 창 클릭 관통 차단");
			child.Close();
			FieldInfo closePoint = formType.GetField("ownedWindowCloseScreenPoint", BindingFlags.Instance | BindingFlags.NonPublic);
			closePoint.SetValue(owner, new Point(Control.MousePosition.X + 200, Control.MousePosition.Y + 200));
			Message underlyingUp = Message.Create(underlyingButton.Handle, 0x0202, IntPtr.Zero, IntPtr.Zero);
			object[] upArguments = new object[] { underlyingUp };
			Equal(true, filter.Invoke(owner, upArguments), "보조 창 닫기 후 포인터가 이동해도 마우스 해제 관통 차단");
			Message deliberateFarDown = Message.Create(underlyingButton.Handle, 0x0201, IntPtr.Zero, IntPtr.Zero);
			object[] deliberateFarDownArguments = new object[] { deliberateFarDown };
			Equal(false, filter.Invoke(owner, deliberateFarDownArguments), "닫기 해제 처리 후 다른 위치의 의도적인 누름 허용");
			Message deliberateFarUp = Message.Create(underlyingButton.Handle, 0x0202, IntPtr.Zero, IntPtr.Zero);
			object[] deliberateFarUpArguments = new object[] { deliberateFarUp };
			Equal(false, filter.Invoke(owner, deliberateFarUpArguments), "닫기 해제 처리 후 다른 위치의 의도적인 해제 허용");
			Application.DoEvents();
			closePoint.SetValue(owner, Control.MousePosition);
			FieldInfo closeHandle = formType.GetField("ownedWindowCloseHandle", BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo guardUntil = formType.GetField("ownedWindowClickGuardUntilTick", BindingFlags.Instance | BindingFlags.NonPublic);
			closeHandle.SetValue(owner, underlyingButton.Handle);
			guardUntil.SetValue(owner, unchecked(Environment.TickCount + 500));
			Message repeatedDown = Message.Create(underlyingButton.Handle, 0x0201, IntPtr.Zero, IntPtr.Zero);
			object[] repeatedArguments = new object[] { repeatedDown };
			Equal(true, filter.Invoke(owner, repeatedArguments), "보조 창 종료 직후 같은 위치의 추가 클릭 차단");
			guardUntil.SetValue(owner, Environment.TickCount);
			Message laterDown = Message.Create(underlyingButton.Handle, 0x0201, IntPtr.Zero, IntPtr.Zero);
			object[] laterArguments = new object[] { laterDown };
			Equal(false, filter.Invoke(owner, laterArguments), "추가 클릭 보호 만료 후 클릭 허용");
			Equal(1, closed, "기능 창 종료 후 콜백 실행");
			Equal(true, ensureSafe.Invoke(owner, null), "기능 창 종료 후 서버 변경 허용");
		}
		Pass();
	}

	private static double ContrastRatio(Color first, Color second)
	{
		double lighter = Math.Max(RelativeLuminance(first), RelativeLuminance(second));
		double darker = Math.Min(RelativeLuminance(first), RelativeLuminance(second));
		return (lighter + 0.05) / (darker + 0.05);
	}

	private static double RelativeLuminance(Color color)
	{
		double red = LinearChannel(color.R / 255.0);
		double green = LinearChannel(color.G / 255.0);
		double blue = LinearChannel(color.B / 255.0);
		return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
	}

	private static double LinearChannel(double value)
	{
		return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
	}

	private static void TestJavaSelection()
	{
		Equal(11, Invoke("ResolvePaperFamilyJavaMajor", new object[] { "1.12.2" }), "구버전 Paper Java");
		Equal(17, Invoke("ResolvePaperFamilyJavaMajor", new object[] { "1.19.4" }), "1.19 Paper Java");
		Equal(21, Invoke("ResolvePaperFamilyJavaMajor", new object[] { "1.21.11" }), "1.21 Paper Java");
		Equal(25, Invoke("ResolvePaperFamilyJavaMajor", new object[] { "26.2" }), "26.2 Paper Java");
		Pass();
	}

	private static void TestConsoleClassificationAndLaunchArguments()
	{
		string terminal = "ServerMain WARN Advanced terminal features are not available in this environment";
		string unsafeWarning = "WARNING: sun.misc.Unsafe::objectFieldOffset will be removed in a future release";
		Equal("Compatibility", Convert.ToString(Invoke("ClassifyConsoleLine", new object[] { terminal })), "터미널 호환성 경고 분류");
		Equal("Compatibility", Convert.ToString(Invoke("ClassifyConsoleLine", new object[] { unsafeWarning })), "Unsafe 호환성 경고 분류");
		Equal("Warning", Convert.ToString(Invoke("ClassifyConsoleLine", new object[] { "Server thread WARN Plugin warning" })), "일반 경고 분류");
		Equal("Error", Convert.ToString(Invoke("ClassifyConsoleLine", new object[] { "Caused by: java.lang.IllegalStateException" })), "오류 분류");
		Equal(true, Invoke("ConsoleLineMatchesFilter", new object[] { terminal, 2 }), "호환성 필터 포함");
		Equal(false, Invoke("ConsoleLineMatchesFilter", new object[] { terminal, 1 }), "일반 경고에서 호환성 경고 제외");
		Equal(" --nojline --nogui", Convert.ToString(Invoke("GetServerConsoleArgument", new object[] { "paper", "26.2" })), "최신 Paper GUI 콘솔 인수");
		Equal(string.Empty, Convert.ToString(Invoke("GetServerConsoleArgument", new object[] { "paper", "1.12.2" })), "구버전 Paper 콘솔 인수 보존");
		Pass();
	}

	private static void TestBackupRestoreAndProfileCopy(string root)
	{
		string profile = Path.Combine(root, "backup-profile");
		Directory.CreateDirectory(Path.Combine(profile, "world"));
		File.WriteAllText(Path.Combine(profile, "world", "level.dat"), "world-v1");
		File.WriteAllText(Path.Combine(profile, "server.properties"), "server-port=25565");
		Directory.CreateDirectory(Path.Combine(profile, ".mineharbor", "content-trash"));
		File.WriteAllText(Path.Combine(profile, ".mineharbor", "content-trash", "removed.jar"), "removed");
		string backup = Convert.ToString(Invoke("CreateComprehensiveServerBackup", new object[] { profile, 3, "test" }));
		using (ZipArchive archive = ZipFile.OpenRead(backup)) Equal(null, archive.GetEntry("profile/.mineharbor/content-trash/removed.jar"), "콘텐츠 휴지통 백업 제외");
		File.WriteAllText(Path.Combine(profile, "server.properties"), "server-port=25566");
		Invoke("RestoreComprehensiveBackup", new object[] { profile, backup, 3 });
		Equal("server-port=25565", File.ReadAllText(Path.Combine(profile, "server.properties")), "백업 복원");
		Equal(true, File.Exists(Path.Combine(profile, ".mineharbor", "content-trash", "removed.jar")), "복원 중 콘텐츠 휴지통 보존");
		string injectedBackup = Path.Combine(root, "backup-with-unmanifested-file.zip");
		File.Copy(backup, injectedBackup, true);
		using (ZipArchive archive = ZipFile.Open(injectedBackup, ZipArchiveMode.Update))
		{
			ZipArchiveEntry injected = archive.CreateEntry("profile/plugins/unmanifested.jar");
			using (StreamWriter writer = new StreamWriter(injected.Open())) writer.Write("not-listed-in-manifest");
		}
		ExpectFailure(delegate { Invoke("VerifyComprehensiveBackup", new object[] { injectedBackup, null }); }, "manifest에 없는 백업 파일 차단");

		string clone = Path.Combine(root, "backup-profile-clone");
		Invoke("CopyProfileDirectory", new object[] { profile, clone });
		Equal(true, File.Exists(Path.Combine(clone, "world", "level.dat")), "프로필 복제");
		Pass();
	}

	private static void TestManagedServerPortAndNetworkStatus(string root)
	{
		string profileDirectory = Path.Combine(root, "managed-port-profile");
		Directory.CreateDirectory(profileDirectory);
		File.WriteAllText(Path.Combine(profileDirectory, "server.properties"), "server-port=25565\r\n", new UTF8Encoding(false));
		Type profileType = launcher.GetNestedType("ManagedProfileRecord", BindingFlags.NonPublic);
		object profile = Activator.CreateInstance(profileType, true);
		SetPublic(profile, "Name", "포트 테스트");
		SetPublic(profile, "Directory", profileDirectory);
		SetPublic(profile, "Port", 25565);
		Type dashboardType = launcher.GetNestedType("MultiServerDashboardForm", BindingFlags.NonPublic);
		MethodInfo updatePort = dashboardType.GetMethod("UpdateManagedProfilePort", BindingFlags.Static | BindingFlags.NonPublic);
		updatePort.Invoke(null, new object[] { profile, 25566 });
		Equal(25566, GetField(profile, "Port"), "멀티 서버 자동 포트 변경 상태");
		Equal(25566, Convert.ToInt32(Invoke("ReadConfiguredServerPort", new object[] { Path.Combine(profileDirectory, "server.properties"), 25565 })), "멀티 서버 자동 포트 변경 저장");

		Type sessionType = launcher.GetNestedType("ManagedServerSession", BindingFlags.NonPublic);
		object session = Activator.CreateInstance(sessionType, true);
		SetPublic(session, "Profile", profile);
		Invoke("ParseManagedServerLine", new object[] { session, "[외부 접속] 외부 접속 실패" });
		Equal(Convert.ToString(Invoke("ManagedText", new object[] { "접속 불가", "Unreachable" })), Convert.ToString(GetField(session, "Status")), "포트포워딩 실패 상태");
		Invoke("ParseManagedServerLine", new object[] { session, "[외부 접속] TCP 응답 확인 · 서버 일치 미확인 · 203.0.113.10:25566" });
		Equal(true, Invoke("IsManagedExternalAccessUnverifiedLine", new object[] { "[외부 접속] TCP 응답 확인 · 서버 일치 미확인 · 203.0.113.10:25566" }), "일반 TCP 응답을 미확인 상태로 분리");
		Equal(false, Invoke("IsManagedExternalAccessVerifiedLine", new object[] { "[외부 접속] TCP 응답 확인 · 서버 일치 미확인 · 203.0.113.10:25566" }), "일반 TCP 응답의 확정 오판 방지");
		Equal("203.0.113.10:25566", Convert.ToString(GetField(session, "Address")), "미확인 외부 주소 후보 갱신");
		Invoke("ParseManagedServerLine", new object[] { session, "[외부 접속] UPnP 매핑 확인됨 · 203.0.113.10:25566" });
		Equal(Convert.ToString(Invoke("ManagedText", new object[] { "온라인", "Online" })), Convert.ToString(GetField(session, "Status")), "검증된 UPnP 외부 접속 복구 상태");
		Equal(true, Invoke("IsManagedExternalAccessVerifiedLine", new object[] { "[외부 접속] UPnP 매핑 확인됨 · 203.0.113.10:25566" }), "검증 완료 UPnP 상태 식별");
		Invoke("ParseManagedServerLine", new object[] { session, "[외부 접속] UPnP 대체 포트 확인됨 · 203.0.113.10:25567" });
		Equal("203.0.113.10:25567", Convert.ToString(GetField(session, "Address")), "검증된 UPnP 대체 외부 포트 주소 갱신");
		Pass();
	}

	private static void TestUpnpOwnershipRules()
	{
		Type trackerType = launcher.GetNestedType("UpnpMappingOwnershipTracker", BindingFlags.NonPublic | BindingFlags.Static);
		if (trackerType == null) throw new Exception("UpnpMappingOwnershipTracker not found");
		string dummyFile = Path.Combine(Path.GetTempPath(), "test-upnp-mappings-" + Guid.NewGuid().ToString("N") + ".tsv");
		PropertyInfo pathProperty = trackerType.GetProperty("TrackerFilePathOverride", BindingFlags.NonPublic | BindingFlags.Static);
		if (pathProperty == null) throw new Exception("UPnP tracker test override not found");
		string oldPath = (string)pathProperty.GetValue(null, null);
		pathProperty.SetValue(null, dummyFile, null);
		try
		{
			Type recordType = launcher.GetNestedType("UpnpMappedPort", BindingFlags.NonPublic);
			object record = Activator.CreateInstance(recordType, true);
			SetMember(record, "ExternalPort", 25566);
			SetMember(record, "InternalPort", 25566);
			SetMember(record, "InternalIp", "192.168.1.20");
			SetMember(record, "Protocol", "TCP");
			SetMember(record, "Description", "MH-123456789012");
			SetMember(record, "CreatedAt", DateTime.UtcNow);
			Type listType = typeof(List<>).MakeGenericType(recordType);
			IList list = (IList)Activator.CreateInstance(listType);
			list.Add(record);
			MethodInfo saveMethod = trackerType.GetMethod("SaveMappings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			MethodInfo loadMethod = trackerType.GetMethod("LoadMappings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (saveMethod == null || loadMethod == null) throw new Exception("UPnP tracker methods not found");
			saveMethod.Invoke(null, new object[] { list });
			Equal(true, File.Exists(dummyFile), "TSV 소유권 파일 생성");
			IList loaded = (IList)loadMethod.Invoke(null, new object[0]);
			Equal(1, loaded.Count, "TSV 소유권 데이터 복원");
			Equal(25566, GetMember(loaded[0], "ExternalPort"), "복원된 외부 포트 일치");

			Type attemptType = launcher.GetNestedType("UpnpMappingAttempt", BindingFlags.NonPublic);
			Type createdType = launcher.GetNestedType("CreatedUpnpMapping", BindingFlags.NonPublic);
			object attempt = Activator.CreateInstance(attemptType, true);
			FakeMappingCollection exactCollection = new FakeMappingCollection(new FakeMapping(25566, "TCP", 25566, "192.168.1.20", "MH-123456789012"));
			SetPublic(attempt, "Collection", exactCollection);
			object created = Activator.CreateInstance(createdType, true);
			SetPublic(created, "ExternalPort", 25566); SetPublic(created, "InternalPort", 25566); SetPublic(created, "Protocol", "TCP"); SetPublic(created, "InternalClient", "192.168.1.20"); SetPublic(created, "Description", "MH-123456789012");
			((IList)GetField(attempt, "Created")).Add(created);
			Invoke("DeleteCreatedUpnpMappings", new object[] { attempt });
			Equal(1, exactCollection.RemoveCount, "정확히 일치하는 현재 실행 매핑만 삭제");

			object mismatchAttempt = Activator.CreateInstance(attemptType, true);
			FakeMappingCollection mismatchCollection = new FakeMappingCollection(new FakeMapping(25566, "TCP", 25566, "192.168.1.20", "다른 설명"));
			SetPublic(mismatchAttempt, "Collection", mismatchCollection);
			((IList)GetField(mismatchAttempt, "Created")).Add(created);
			Invoke("DeleteCreatedUpnpMappings", new object[] { mismatchAttempt });
			Equal(0, mismatchCollection.RemoveCount, "설명이 다른 매핑 삭제 차단");

			using (ManualResetEvent notStopped = new ManualResetEvent(false))
			{
				for (int cycle = 0; cycle < 12; cycle++)
				{
					object lifecycleAttempt = Activator.CreateInstance(attemptType, true);
					FakeMappingCollection lifecycleCollection = new FakeMappingCollection(null);
					SetPublic(lifecycleAttempt, "Collection", lifecycleCollection);
					string lifecycleDescription = "MH-" + cycle.ToString("D12");
					Equal(true, Invoke("TryAddSingleUpnpMapping", new object[] { lifecycleAttempt, 25566, 25565, "TCP", "192.168.1.20", lifecycleDescription, notStopped }), "반복 실행 매핑 생성 " + cycle);
					Invoke("DeleteCreatedUpnpMappings", new object[] { lifecycleAttempt });
					Equal(0, lifecycleCollection.Count, "반복 실행 매핑 정리 " + cycle);
				}

				object lostResponseAttempt = Activator.CreateInstance(attemptType, true);
				FakeMappingCollection lostResponseCollection = new FakeMappingCollection(null, true);
				SetPublic(lostResponseAttempt, "Collection", lostResponseCollection);
				Equal(true, Invoke("TryAddSingleUpnpMapping", new object[] { lostResponseAttempt, 25567, 25565, "TCP", "192.168.1.20", "MH-999999999999", notStopped }), "Add 응답 유실 후 현재 매핑 회수");
				Equal(1, ((IList)GetField(lostResponseAttempt, "Created")).Count, "응답 유실 매핑 소유권 기록");
				Invoke("DeleteCreatedUpnpMappings", new object[] { lostResponseAttempt });
				Equal(0, lostResponseCollection.Count, "응답 유실 매핑 종료 정리");

				int[] candidates = (int[])Invoke("GetUpnpExternalPortCandidates", new object[] { 25565 });
				Equal(9, candidates.Length, "기본 포트와 대체 포트 후보 수");
				Equal(25565, candidates[0], "기본 외부 포트 우선");
				object fallbackAttempt = Activator.CreateInstance(attemptType, true);
				FakeMappingCollection fallbackCollection = new FakeMappingCollection(new FakeMapping(25565, "TCP", 25565, "192.168.1.99", "사용자 매핑"));
				SetPublic(fallbackAttempt, "Collection", fallbackCollection);
				Equal(true, Invoke("TryAddTcpUpnpMappingWithFallback", new object[] { fallbackAttempt, candidates, 25565, "192.168.1.20", "MH-888888888888", notStopped }), "충돌 후 COM 대체 외부 포트 사용");
				Equal(25566, GetField(fallbackAttempt, "ExternalPort"), "선택된 COM 대체 외부 포트");
				Invoke("DeleteCreatedUpnpMappings", new object[] { fallbackAttempt });
				Equal(1, fallbackCollection.Count, "기존 사용자 매핑 보존");
				Equal(true, fallbackCollection[25565, "TCP"] != null, "기본 포트의 사용자 매핑 유지");

				FieldInfo generationField = launcher.GetField("currentUpnpGeneration", BindingFlags.Static | BindingFlags.NonPublic);
				long oldGenerationValue = Convert.ToInt64(generationField.GetValue(null));
				try
				{
					generationField.SetValue(null, 2L);
					FakeMappingCollection delayedCollection = new FakeMappingCollection(new FakeMapping(25568, "TCP", 25565, "192.168.1.20", "MH-777777777777"));
					object delayedAttempt = Activator.CreateInstance(attemptType, true);
					SetPublic(delayedAttempt, "Collection", delayedCollection);
					SetPublic(delayedAttempt, "Generation", 1L);
					object delayedCreated = Activator.CreateInstance(createdType, true);
					SetPublic(delayedCreated, "ExternalPort", 25568); SetPublic(delayedCreated, "InternalPort", 25565); SetPublic(delayedCreated, "Protocol", "TCP"); SetPublic(delayedCreated, "InternalClient", "192.168.1.20"); SetPublic(delayedCreated, "Description", "MH-777777777777");
					((IList)GetField(delayedAttempt, "Created")).Add(delayedCreated);
					Invoke("DeleteCreatedUpnpMappings", new object[] { delayedAttempt });
					Equal(0, delayedCollection.RemoveCount, "새 실행 이후 이전 실행의 지연 삭제 차단");
					object currentAttempt = Activator.CreateInstance(attemptType, true);
					SetPublic(currentAttempt, "Collection", delayedCollection);
					SetPublic(currentAttempt, "Generation", 2L);
					((IList)GetField(currentAttempt, "Created")).Add(delayedCreated);
					Invoke("DeleteCreatedUpnpMappings", new object[] { currentAttempt });
					Equal(1, delayedCollection.RemoveCount, "현재 실행이 인계받은 매핑 정리");
				}
				finally { generationField.SetValue(null, oldGenerationValue); }

				FieldInfo cleanupField = launcher.GetField("upnpComCleanupInProgress", BindingFlags.Static | BindingFlags.NonPublic);
				int oldCleanupValue = Convert.ToInt32(cleanupField.GetValue(null));
				try
				{
					cleanupField.SetValue(null, 1);
					Type networkType = launcher.GetNestedType("NetworkDetails", BindingFlags.NonPublic);
					object network = Activator.CreateInstance(networkType, true);
					SetPublic(network, "LocalIpv4", "127.0.0.1");
					object blockedAttempt = Invoke("TryCreateUpnpMappings", new object[] { 25565, network, false, notStopped });
					Equal(true, Convert.ToString(GetField(blockedAttempt, "Error")).Contains("이전 서버 실행"), "이전 COM 정리 중 새 매핑 차단");
					Equal(null, GetField(blockedAttempt, "SocketService"), "정리 경합 중 SSDP/SOAP 장치 검색 미실행");
				}
				finally { cleanupField.SetValue(null, oldCleanupValue); }
			}
		}
		finally { pathProperty.SetValue(null, oldPath, null); if (File.Exists(dummyFile)) File.Delete(dummyFile); }
		Pass();
	}

	private static void TestModrinthHash(string root)
	{
		string file = Path.Combine(root, "modrinth.jar");
		File.WriteAllText(file, "modrinth verified content", Encoding.UTF8);
		string sha1;
		string sha512;
		using (SHA1 algorithm = SHA1.Create()) sha1 = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(file))).Replace("-", string.Empty).ToLowerInvariant();
		using (SHA512 algorithm = SHA512.Create()) sha512 = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(file))).Replace("-", string.Empty).ToLowerInvariant();
		Type fileType = launcher.GetNestedType("ModrinthFileInfo", BindingFlags.NonPublic);
		object info = Activator.CreateInstance(fileType, true);
		SetPublic(info, "Sha1", sha1);
		SetPublic(info, "Sha512", sha512);
		Equal(true, Invoke("VerifyModrinthHash", new object[] { file, info }), "Modrinth 해시 검증");
		SetPublic(info, "Sha512", new string('0', 128));
		Equal(false, Invoke("VerifyModrinthHash", new object[] { file, info }), "Modrinth 해시 불일치 차단");
		Pass();
	}

	private static void TestContentManagement(string root)
	{
		string server = Path.Combine(root, "content-profile");
		Directory.CreateDirectory(Path.Combine(server, "plugins"));
		Directory.CreateDirectory(Path.Combine(server, "world", "datapacks"));
		File.WriteAllText(Path.Combine(server, "world", "level.dat"), "test-world");
		File.WriteAllText(Path.Combine(server, "server.properties"), "level-name=world\r\n");
		string manualJar = Path.Combine(server, "plugins", "manual.jar");
		File.WriteAllText(manualJar, "manual plugin");

		IList initial = (IList)Invoke("ScanInstalledContent", new object[] { server, "paper" });
		Equal(1, initial.Count, "수동 플러그인 검색");
		object manualItem = initial[0];
		Equal(false, GetField(manualItem, "Managed"), "수동 설치와 관리 설치 구분");
		object manualEntry = GetField(manualItem, "Entry");
		Invoke("SetContentEnabled", new object[] { server, manualEntry, false });
		Equal(false, File.Exists(manualJar), "수동 플러그인 비활성화 이동");
		Invoke("SetContentEnabled", new object[] { server, manualEntry, true });
		Equal(true, File.Exists(manualJar), "수동 플러그인 다시 활성화");

		string manifestPath = Path.Combine(server, ".mineharbor", "content-manifest.json");
		string validManifest = File.ReadAllText(manifestPath);
		File.WriteAllText(manifestPath, "{broken", new UTF8Encoding(false));
		ExpectFailure(delegate { Invoke("LoadContentManifest", new object[] { server }); }, "손상된 콘텐츠 manifest 차단");
		File.WriteAllText(manifestPath, validManifest, new UTF8Encoding(false));
		int itemsStart = validManifest.IndexOf("\"items\":[", StringComparison.Ordinal) + 9;
		int itemsEnd = validManifest.LastIndexOf(']');
		string oneItem = validManifest.Substring(itemsStart, itemsEnd - itemsStart);
		string duplicated = validManifest.Substring(0, itemsStart) + oneItem + "," + oneItem + validManifest.Substring(itemsEnd);
		File.WriteAllText(manifestPath, duplicated, new UTF8Encoding(false));
		ExpectFailure(delegate { Invoke("LoadContentManifest", new object[] { server }); }, "중복 콘텐츠 manifest 항목 차단");
		File.WriteAllText(manifestPath, validManifest, new UTF8Encoding(false));

		Equal(false, Invoke("VerifyContentFileHash", new object[] { manualJar, new string('0', 128), string.Empty }), "콘텐츠 해시 불일치 차단");
		Dictionary<string, string[]> graph = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
		graph["a"] = new string[] { "b" }; graph["b"] = new string[] { "a" };
		ExpectFailure(delegate { Invoke("ValidateContentDependencyGraph", new object[] { graph }); }, "콘텐츠 의존성 순환 차단");

		string invalidDatapack = Path.Combine(root, "invalid-datapack.zip");
		using (ZipArchive archive = ZipFile.Open(invalidDatapack, ZipArchiveMode.Create))
		using (StreamWriter writer = new StreamWriter(archive.CreateEntry("wrapper/readme.txt").Open())) writer.Write("invalid");
		ExpectFailure(delegate { Invoke("ValidateDatapackArchive", new object[] { invalidDatapack }); }, "pack.mcmeta 없는 데이터팩 차단");
		string unsafeDatapack = Path.Combine(root, "unsafe-datapack.zip");
		using (ZipArchive archive = ZipFile.Open(unsafeDatapack, ZipArchiveMode.Create))
		{
			using (StreamWriter writer = new StreamWriter(archive.CreateEntry("pack.mcmeta").Open())) writer.Write("{\"pack\":{\"pack_format\":48,\"description\":\"test\"}}");
			using (StreamWriter writer = new StreamWriter(archive.CreateEntry("..").Open())) writer.Write("unsafe");
		}
		ExpectFailure(delegate { Invoke("ValidateDatapackArchive", new object[] { unsafeDatapack }); }, "데이터팩 특수 경로 차단");
		string validDatapack = Path.Combine(root, "valid-datapack.zip");
		using (ZipArchive archive = ZipFile.Open(validDatapack, ZipArchiveMode.Create))
		{
			using (StreamWriter writer = new StreamWriter(archive.CreateEntry("pack.mcmeta").Open())) writer.Write("{\"pack\":{\"pack_format\":48,\"description\":\"test\"}}");
			using (StreamWriter writer = new StreamWriter(archive.CreateEntry("data/test/functions/load.mcfunction").Open())) writer.Write("say test");
		}
		Invoke("ValidateDatapackArchive", new object[] { validDatapack });

		Type optionsType = launcher.GetNestedType("LauncherOptions", BindingFlags.NonPublic);
		object options = Activator.CreateInstance(optionsType, true);
		SetPublic(options, "ServerDirectory", server); SetPublic(options, "ProfileName", "content-test"); SetPublic(options, "ServerType", "paper"); SetPublic(options, "MinecraftVersion", "1.21.11");
		string installedDatapack = Convert.ToString(Invoke("InstallLocalContentFile", new object[] { validDatapack, options, "datapack", "world" }));
		Equal(true, File.Exists(installedDatapack), "검증된 데이터팩 설치");
		ExpectFailure(delegate { Invoke("InstallLocalContentFile", new object[] { validDatapack, options, "datapack", "world" }); }, "데이터팩 중복 설치 차단");

		string localSource = Path.Combine(root, "local-content.jar");
		File.WriteAllText(localSource, "local managed plugin");
		string localInstalled = Convert.ToString(Invoke("InstallLocalContentFile", new object[] { localSource, options, "plugin", string.Empty }));
		Equal(true, File.Exists(localInstalled), "로컬 콘텐츠 파일 설치");
		ExpectFailure(delegate { Invoke("InstallLocalContentFile", new object[] { localSource, options, "plugin", string.Empty }); }, "동일 파일 중복 설치 차단");

		object cycleManifest = Invoke("LoadContentManifest", new object[] { server });
		IList cycleItems = (IList)GetField(cycleManifest, "Items");
		cycleItems.Add(CreateContentManifestEntry("cycle-a", "cycle-a", "cycle-a.jar", new string[] { "cycle-b" }));
		cycleItems.Add(CreateContentManifestEntry("cycle-b", "cycle-b", "cycle-b.jar", new string[] { "cycle-a" }));
		ExpectFailure(delegate { Invoke("SaveContentManifest", new object[] { server, cycleManifest }); }, "manifest 의존성 순환 저장 차단");

		object dependencyManifest = Invoke("LoadContentManifest", new object[] { server });
		IList dependencyItems = (IList)GetField(dependencyManifest, "Items");
		object dependencyEntry = CreateContentManifestEntry("required-library", "required-library", "required-library.jar", new string[0]);
		object consumerEntry = CreateContentManifestEntry("consumer-plugin", "consumer-plugin", "consumer-plugin.jar", new string[] { "required-library" });
		dependencyItems.Add(dependencyEntry); dependencyItems.Add(consumerEntry);
		File.WriteAllText(Path.Combine(server, "plugins", "required-library.jar"), "dependency");
		File.WriteAllText(Path.Combine(server, "plugins", "consumer-plugin.jar"), "consumer");
		Invoke("SaveContentManifest", new object[] { server, dependencyManifest });
		ExpectFailure(delegate { Invoke("RemoveContentItem", new object[] { server, dependencyEntry }); }, "사용 중인 필수 의존성 제거 차단");
		Equal(true, File.Exists(Path.Combine(server, "plugins", "required-library.jar")), "의존성 제거 실패 시 원본 보존");

		IList afterInstall = (IList)Invoke("ScanInstalledContent", new object[] { server, "paper" });
		object missingEntry = null;
		for (int i = 0; i < afterInstall.Count; i++) if (string.Equals(Convert.ToString(GetField(afterInstall[i], "FullPath")), localInstalled, StringComparison.OrdinalIgnoreCase)) missingEntry = GetField(afterInstall[i], "Entry");
		if (missingEntry == null) throw new InvalidOperationException("설치된 로컬 콘텐츠를 manifest에서 찾지 못했습니다.");
		File.Delete(localInstalled);
		ExpectFailure(delegate { Invoke("RemoveContentItem", new object[] { server, missingEntry }); }, "제거 대상 파일 없음 처리");

		Type formType = launcher.GetNestedType("UnifiedContentManagerForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(formType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { options }, null))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "콘텐츠 관리 DPI 배율");
			ListView list = (ListView)GetPrivateField(formType, form, "installedList");
			if (string.IsNullOrEmpty(list.AccessibleName)) throw new InvalidOperationException("콘텐츠 목록 접근성 이름이 없습니다.");
		}
		Pass();
	}

	private static void TestServerAutomationAndDashboard(string root)
	{
		string server = Path.Combine(root, "automation-profile");
		Directory.CreateDirectory(Path.Combine(server, "world"));
		File.WriteAllText(Path.Combine(server, "world", "level.dat"), "world");
		File.WriteAllText(Path.Combine(server, "server.properties"), "level-name=world\r\n");
		object configuration = Invoke("ReadServerAutomationConfiguration", new object[] { server });
		Equal(2, GetField(configuration, "SchemaVersion"), "자동화 설정 스키마 기본값");
		Type jobType = launcher.GetNestedType("ServerAutomationJob", BindingFlags.NonPublic);
		object job = Activator.CreateInstance(jobType, true);
		SetPublic(job, "Id", "scheduled-backup");
		SetPublic(job, "Name", "정기 백업");
		SetPublic(job, "Action", "backup");
		SetPublic(job, "Enabled", true);
		SetPublic(job, "ScheduleKind", "interval");
		SetPublic(job, "IntervalMinutes", 60);
		SetPublic(job, "DailyLocalTime", "04:00");
		SetPublic(job, "WarningSeconds", 0);
		SetPublic(job, "NextRunUtc", DateTime.UtcNow.AddMinutes(-1).ToString("o"));
		((IList)GetField(configuration, "Jobs")).Add(job);
		Invoke("WriteServerAutomationConfiguration", new object[] { server, configuration });
		IList firstClaims = (IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow });
		Equal(1, firstClaims.Count, "예약 작업 첫 실행 임대");
		IList duplicateClaims = (IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow });
		Equal(0, duplicateClaims.Count, "예약 작업 중복 실행 차단");
		Invoke("CompleteAutomationJob", new object[] { firstClaims[0], DateTime.UtcNow, "test-completed" });
		object completedConfiguration = Invoke("ReadServerAutomationConfiguration", new object[] { server });
		object completedJob = ((IList)GetField(completedConfiguration, "Jobs"))[0];
		Equal(false, GetField(completedJob, "Running"), "예약 작업 임대 해제");
		Equal("test-completed", GetField(completedJob, "LastResult"), "예약 작업 최근 결과 기록");
		SetPublic(completedJob, "Running", true);
		SetPublic(completedJob, "LeaseUtc", DateTime.UtcNow.ToString("o"));
		SetPublic(completedJob, "LeaseProcessId", int.MaxValue);
		SetPublic(completedJob, "LeaseProcessStartTicks", 1L);
		SetPublic(completedJob, "NextRunUtc", DateTime.UtcNow.AddMinutes(-1).ToString("o"));
		Invoke("WriteServerAutomationConfiguration", new object[] { server, completedConfiguration });
		IList recoveredClaims = (IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow });
		Equal(1, recoveredClaims.Count, "종료된 프로세스의 예약 작업 임대 복구");
		Invoke("CompleteAutomationJob", new object[] { recoveredClaims[0], DateTime.UtcNow, "lease-recovered" });

		string automationPath = Convert.ToString(Invoke("GetAutomationConfigurationPath", new object[] { server }));
		File.WriteAllText(automationPath, "{\"SchemaVersion\":1,\"Jobs\":[]}", Encoding.UTF8);
		object migrated = Invoke("ReadServerAutomationConfiguration", new object[] { server });
		Equal(2, GetField(migrated, "SchemaVersion"), "기존 자동화 스키마 메모리 마이그레이션");
		Equal("{\"SchemaVersion\":1,\"Jobs\":[]}", File.ReadAllText(automationPath, Encoding.UTF8), "읽기 중 기존 자동화 파일 미변경");
		File.WriteAllText(automationPath, "{\"SchemaVersion\":999,\"Jobs\":[]}", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadServerAutomationConfiguration", new object[] { server }); }, "미래 자동화 스키마 거부");
		Equal("{\"SchemaVersion\":999,\"Jobs\":[]}", File.ReadAllText(automationPath, Encoding.UTF8), "미래 자동화 스키마 원본 보존");
		File.WriteAllText(automationPath, "{ broken", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadServerAutomationConfiguration", new object[] { server }); }, "손상된 자동화 설정 차단");
		Equal("{ broken", File.ReadAllText(automationPath, Encoding.UTF8), "손상된 자동화 설정 원본 보존");
		File.Delete(automationPath);
		ExpectFailure(delegate { Invoke("ValidateScheduledCommand", new object[] { "say first\nstop" }); }, "예약 명령 줄바꿈 차단");

		object weekly = Activator.CreateInstance(jobType, true);
		SetPublic(weekly, "Id", "weekly-test");
		SetPublic(weekly, "Name", "요일 일정");
		SetPublic(weekly, "Action", "backup");
		SetPublic(weekly, "ScheduleKind", "weekly");
		SetPublic(weekly, "DailyLocalTime", "04:00");
		SetPublic(weekly, "Weekdays", "Monday,Wednesday");
		DateTime weeklyNext = (DateTime)Invoke("CalculateNextAutomationRunUtc", new object[] { weekly, DateTime.UtcNow });
		DayOfWeek localDay = weeklyNext.ToLocalTime().DayOfWeek;
		if (localDay != DayOfWeek.Monday && localDay != DayOfWeek.Wednesday) throw new InvalidOperationException("요일 일정이 선택하지 않은 요일을 계산했습니다.");
		if (weeklyNext <= DateTime.UtcNow) throw new InvalidOperationException("요일 일정의 다음 실행 시각이 미래가 아닙니다.");
		SetPublic(weekly, "Weekdays", "1");
		ExpectFailure(delegate { Invoke("CalculateNextAutomationRunUtc", new object[] { weekly, DateTime.UtcNow }); }, "숫자형 요일 값 거부");

		object skippedConfiguration = Invoke("ReadServerAutomationConfiguration", new object[] { server });
		object skipped = Activator.CreateInstance(jobType, true);
		SetPublic(skipped, "Id", "missed-skip");
		SetPublic(skipped, "Name", "놓친 작업");
		SetPublic(skipped, "Action", "backup");
		SetPublic(skipped, "ScheduleKind", "interval");
		SetPublic(skipped, "IntervalMinutes", 60);
		SetPublic(skipped, "MissedRunPolicy", "skip");
		SetPublic(skipped, "MaximumDelayMinutes", 10);
		SetPublic(skipped, "NextRunUtc", DateTime.UtcNow.AddHours(-2).ToString("o"));
		((IList)GetField(skippedConfiguration, "Jobs")).Add(skipped);
		Invoke("WriteServerAutomationConfiguration", new object[] { server, skippedConfiguration });
		Equal(0, ((IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow })).Count, "최대 지연 초과 작업 건너뛰기");
		object skippedAfter = ((IList)GetField(Invoke("ReadServerAutomationConfiguration", new object[] { server }), "Jobs"))[0];
		if (Convert.ToString(GetField(skippedAfter, "LastResult")).IndexOf("건너뜀", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("놓친 작업의 건너뛰기 결과가 기록되지 않았습니다.");
		((IList)GetField(skippedConfiguration, "Jobs")).Clear();
		object once = Activator.CreateInstance(jobType, true);
		SetPublic(once, "Id", "once-test");
		SetPublic(once, "Name", "일회성 백업");
		SetPublic(once, "Action", "backup");
		SetPublic(once, "ScheduleKind", "once");
		SetPublic(once, "OnceLocalDateTime", DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm"));
		SetPublic(once, "MissedRunPolicy", "run-once");
		SetPublic(once, "NextRunUtc", DateTime.UtcNow.AddSeconds(-5).ToString("o"));
		((IList)GetField(skippedConfiguration, "Jobs")).Add(once);
		Invoke("WriteServerAutomationConfiguration", new object[] { server, skippedConfiguration });
		IList onceClaims = (IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow });
		Equal(1, onceClaims.Count, "일회성 예약 한 번 임대");
		object onceStored = ((IList)GetField(Invoke("ReadServerAutomationConfiguration", new object[] { server }), "Jobs"))[0];
		Equal(false, GetField(onceStored, "Enabled"), "일회성 예약 재실행 비활성화");
		Equal(0, ((IList)Invoke("ClaimDueAutomationJobs", new object[] { server, DateTime.UtcNow })).Count, "일회성 예약 중복 실행 차단");
		Invoke("CompleteAutomationJob", new object[] { onceClaims[0], DateTime.UtcNow, "once-completed" });

		string backups = Path.Combine(server, "server-backups");
		Directory.CreateDirectory(backups);
		for (int i = 0; i < 5; i++)
		{
			string path = Path.Combine(backups, "server-2026010" + i + "-000000-000-test.zip");
			File.WriteAllBytes(path, new byte[64]);
			File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-i));
		}
		Invoke("PruneServerBackupsWithPolicy", new object[] { backups, 2, 2, 104857600L, DateTime.UtcNow });
		Equal(2, Directory.GetFiles(backups, "server-*.zip").Length, "백업 개수·기간 보존 정책");
		Equal(true, File.Exists(Path.Combine(backups, "server-20260100-000000-000-test.zip")), "최신 백업 보존");
		string emptyServer = Path.Combine(root, "automation-empty-backup");
		Directory.CreateDirectory(emptyServer);
		ExpectFailure(delegate { Invoke("CreateComprehensiveServerBackup", new object[] { emptyServer, 3, "scheduled" }); }, "백업 대상 없음 실패 처리");

		Type profileType = launcher.GetNestedType("ManagedProfileRecord", BindingFlags.NonPublic);
		object profile = Activator.CreateInstance(profileType, true);
		SetPublic(profile, "Name", "대시보드 테스트"); SetPublic(profile, "Directory", server); SetPublic(profile, "ServerType", "paper"); SetPublic(profile, "MinecraftVersion", "1.21.11"); SetPublic(profile, "Port", 25565); SetPublic(profile, "MemoryGb", 2);
		Type sessionType = launcher.GetNestedType("ManagedServerSession", BindingFlags.NonPublic);
		object session = Activator.CreateInstance(sessionType, true);
		SetPublic(session, "Profile", profile);
		Invoke("ParseManagedServerLine", new object[] { session, "[MineHarbor Metrics] tps1=20.000,tps5=19.900,tps15=19.800,mspt=12.500" });
		Equal(true, GetField(session, "MetricsAvailable"), "브리지 TPS 지표 수신");
		Equal(12.5, GetField(session, "Mspt"), "브리지 MSPT 지표 수신");
		string disconnected = Convert.ToString(Invoke("GetTickHealthStatus", new object[] { null }));
		if (disconnected.IndexOf("지원되지", StringComparison.OrdinalIgnoreCase) < 0 && disconnected.IndexOf("Unavailable", StringComparison.OrdinalIgnoreCase) < 0) throw new InvalidOperationException("브리지 연결 해제 상태를 명시하지 않았습니다.");
		object snapshot = AwaitTaskResult(Invoke("CollectServerStatusAsync", new object[] { profile, null, CancellationToken.None }));
		if (string.IsNullOrWhiteSpace(Convert.ToString(GetField(snapshot, "ServerSize")))) throw new InvalidOperationException("대시보드 서버 용량이 비어 있습니다.");

		Type automationFormType = launcher.GetNestedType("AutomationManagerForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(automationFormType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { server }, null))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "자동화 UI DPI 배율");
			ListView list = (ListView)GetPrivateField(automationFormType, form, "jobList");
			if (string.IsNullOrWhiteSpace(list.AccessibleName)) throw new InvalidOperationException("예약 작업 목록 접근성 이름이 없습니다.");
		}
		Type automationJobFormType = launcher.GetNestedType("AutomationJobForm", BindingFlags.NonPublic);
		using (Form jobForm = (Form)Activator.CreateInstance(automationJobFormType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { null }, null))
		{
			ComboBox action = (ComboBox)GetPrivateField(automationJobFormType, jobForm, "actionBox");
			ComboBox schedule = (ComboBox)GetPrivateField(automationJobFormType, jobForm, "scheduleBox");
			TextBox command = (TextBox)GetPrivateField(automationJobFormType, jobForm, "commandBox");
			action.SelectedIndex = 4;
			Equal(true, command.Enabled, "예약 명령 입력 활성화");
			Equal(4, schedule.Items.Count, "고급 예약 방식 표시");
			Label preview = (Label)GetPrivateField(automationJobFormType, jobForm, "previewLabel");
			if (string.IsNullOrWhiteSpace(preview.AccessibleName) || string.IsNullOrWhiteSpace(preview.Text)) throw new InvalidOperationException("예약 미리보기 접근성 정보가 없습니다.");
			if (GetPrivateField(automationJobFormType, jobForm, "commandSuggestions") == null) throw new InvalidOperationException("예약 명령 자동완성이 연결되지 않았습니다.");
		}
		Type dashboardFormType = launcher.GetNestedType("ServerStatusDashboardForm", BindingFlags.NonPublic);
		using (Form dashboard = (Form)Activator.CreateInstance(dashboardFormType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { profile, session }, null))
		{
			dashboard.Show(); Application.DoEvents(); dashboard.Close(); Application.DoEvents();
			CancellationTokenSource closedCancellation = (CancellationTokenSource)GetPrivateField(dashboardFormType, dashboard, "cancellation");
			Equal(true, closedCancellation.IsCancellationRequested, "UI 종료 후 비동기 콜백 취소");
		}
		Pass();
	}

	private static void TestBackgroundAgentSettings(string root)
	{
		string path = Path.Combine(root, "background-agent", "settings.json");
		SetStaticField("BackgroundAgentSettingsPathOverride", path);
		SetStaticField("BackgroundAgentPipeNameOverride", "MineHarbor.Tests." + Guid.NewGuid().ToString("N"));
		Type settingsType = launcher.GetNestedType("BackgroundAgentSettings", BindingFlags.NonPublic);
		object defaults = Invoke("ReadBackgroundAgentSettings", new object[0]);
		Equal(1, GetField(defaults, "SchemaVersion"), "백그라운드 설정 기본 스키마");
		Equal(false, GetField(defaults, "Enabled"), "백그라운드 운영 기본 비활성화");
		Equal(false, Invoke("IsBackgroundAgentRunning", new object[0]), "없는 에이전트 연결 상태");

		object settings = Activator.CreateInstance(settingsType, true);
		SetPublic(settings, "Enabled", true);
		SetPublic(settings, "StartWithWindows", true);
		SetPublic(settings, "RestartAfterCrash", true);
		SetPublic(settings, "Paused", true);
		Invoke("WriteBackgroundAgentSettings", new object[] { settings });
		object loaded = Invoke("ReadBackgroundAgentSettings", new object[0]);
		Equal(true, GetField(loaded, "Enabled"), "백그라운드 운영 동의 저장");
		Equal(true, GetField(loaded, "StartWithWindows"), "Windows 자동 시작 저장");
		Equal(true, GetField(loaded, "RestartAfterCrash"), "충돌 자동 재시작 저장");
		Equal(true, GetField(loaded, "Paused"), "백그라운드 운영 일시 중지 저장");

		bool startupEnabled = false;
		string startupPath = null;
		Action<bool, string> registration = delegate(bool enabled, string executable)
		{
			startupEnabled = enabled;
			startupPath = executable;
		};
		SetStaticField("BackgroundAgentStartupRegistrationOverride", registration);
		Invoke("SetBackgroundAgentStartupRegistration", new object[] { true, @"C:\MineHarbor\MineHarbor.exe" });
		Equal(true, startupEnabled, "자동 시작 등록 동의 전달");
		Equal(@"C:\MineHarbor\MineHarbor.exe", startupPath, "자동 시작 실행 파일 전달");

		string agentData = Path.Combine(root, "background-agent-data");
		Directory.CreateDirectory(agentData);
		string storagePath = Path.Combine(root, "background-agent-storage.properties");
		SetStaticField("StorageSettingsPathOverride", storagePath);
		SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "background-agent-user"));
		Invoke("SaveDataStorageSettings", new object[] { "custom", agentData });
		SetPublic(settings, "Paused", false);
		Invoke("WriteBackgroundAgentSettings", new object[] { settings });
		SetStaticField("BackgroundAgentDisableTrayForTests", true);
		Type contextType = launcher.GetNestedType("BackgroundAgentContext", BindingFlags.NonPublic);
		object context = Activator.CreateInstance(contextType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { settings }, null);
		try
		{
			object response = null;
			for (int attempt = 0; attempt < 20 && response == null; attempt++)
			{
				Thread.Sleep(50);
				response = Invoke("SendBackgroundAgentRequest", new object[] { "ping", null, null, 500 });
			}
			if (response == null) throw new InvalidOperationException("테스트 백그라운드 에이전트 IPC에 연결하지 못했습니다.");
			Equal(true, GetField(response, "Success"), "현재 사용자 로컬 IPC 상태 요청");
			object pause = Invoke("SendBackgroundAgentRequest", new object[] { "pause", null, null, 1000 });
			Equal(true, GetField(pause, "Success"), "백그라운드 예약 일시 중지 IPC");
			Equal(true, GetField(Invoke("ReadBackgroundAgentSettings", new object[0]), "Paused"), "IPC 일시 중지 상태 저장");
			object resume = Invoke("SendBackgroundAgentRequest", new object[] { "resume", null, null, 1000 });
			Equal(true, GetField(resume, "Success"), "백그라운드 예약 재개 IPC");
			int testNotifications = 0;
			SetStaticField("WindowsNotificationDisplayOverride", new Action<string, string, ToolTipIcon>(delegate { testNotifications++; }));
			object notification = Invoke("SendBackgroundAgentRequest", new object[] { "test-notification", null, null, 1000 });
			Equal(true, GetField(notification, "Success"), "Windows 테스트 알림 IPC");
			Equal(1, testNotifications, "Windows 테스트 알림 전달");
		}
		finally
		{
			((IDisposable)context).Dispose();
			SetStaticField("BackgroundAgentDisableTrayForTests", false);
		}

		File.WriteAllText(path, "{\"SchemaVersion\":999}", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadBackgroundAgentSettings", new object[0]); }, "미래 백그라운드 설정 스키마 거부");
		Equal("{\"SchemaVersion\":999}", File.ReadAllText(path, Encoding.UTF8), "미래 백그라운드 설정 원본 보존");
		File.WriteAllText(path, "{ broken", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadBackgroundAgentSettings", new object[0]); }, "손상된 백그라운드 설정 차단");
		Equal("{ broken", File.ReadAllText(path, Encoding.UTF8), "손상된 백그라운드 설정 원본 보존");
		File.Delete(path);

		Type settingsFormType = launcher.GetNestedType("BackgroundAgentSettingsForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(settingsFormType, true))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "백그라운드 설정 UI DPI 배율");
			CheckBox enabledBox = (CheckBox)GetPrivateField(settingsFormType, form, "enabledBox");
			if (string.IsNullOrWhiteSpace(enabledBox.AccessibleName) || string.IsNullOrWhiteSpace(enabledBox.AccessibleDescription))
				throw new InvalidOperationException("백그라운드 설정 접근성 정보가 없습니다.");
		}
		Type consoleFormType = launcher.GetNestedType("BackgroundAgentConsoleForm", BindingFlags.NonPublic);
		using (Form console = (Form)Activator.CreateInstance(consoleFormType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { "테스트 서버" }, null))
		{
			Equal(AutoScaleMode.Dpi, console.AutoScaleMode, "백그라운드 콘솔 DPI 배율");
			TextBox commandBox = (TextBox)GetPrivateField(consoleFormType, console, "commandBox");
			if (string.IsNullOrWhiteSpace(commandBox.AccessibleName) || GetPrivateField(consoleFormType, console, "commandSuggestions") == null)
				throw new InvalidOperationException("백그라운드 콘솔 자동완성 또는 접근성 정보가 없습니다.");
		}
		SetStaticField("BackgroundAgentSettingsPathOverride", null);
		SetStaticField("BackgroundAgentPipeNameOverride", null);
		SetStaticField("BackgroundAgentStartupRegistrationOverride", null);
		SetStaticField("BackgroundAgentDisableTrayForTests", false);
		SetStaticField("WindowsNotificationDisplayOverride", null);
		SetStaticField("StorageSettingsPathOverride", Path.Combine(root, "storage.properties"));
		SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "isolated-user-data"));
		Pass();
	}

	private static void TestManagedServerHandoff(string root)
	{
		string dataRoot = Path.Combine(root, "handoff-data");
		string profileDirectory = Path.Combine(dataRoot, "servers", "인계 테스트 서버");
		Directory.CreateDirectory(profileDirectory);
		File.WriteAllText(
			Path.Combine(profileDirectory, ".launcher-properties-configured"),
			"launcher-settings-version=7\r\nprofile-name=인계 테스트 서버\r\nserver-type=paper\r\nminecraft-version=1.21.11\r\nmemory-gb=2\r\n",
			Encoding.UTF8);
		File.WriteAllText(Path.Combine(profileDirectory, "server.properties"), "server-port=25591\r\n", Encoding.UTF8);
		File.WriteAllText(Path.Combine(profileDirectory, "eula.txt"), "eula=true\r\n", Encoding.UTF8);
		SetStaticField("StorageSettingsPathOverride", Path.Combine(root, "handoff-storage.properties"));
		SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "handoff-user"));
		Invoke("SaveDataStorageSettings", new object[] { "custom", dataRoot });
		SetStaticField("BackgroundAgentSettingsPathOverride", Path.Combine(root, "handoff-background.json"));
		SetStaticField("BackgroundAgentPipeNameOverride", "MineHarbor.Handoff.Tests." + Guid.NewGuid().ToString("N"));
		SetStaticField("WindowsNotificationSettingsPathOverride", Path.Combine(root, "handoff-notifications.json"));
		SetStaticField("BackgroundAgentDisableTrayForTests", true);

		string pipeName = Convert.ToString(Invoke("CreateManagedChildControlPipeName", new object[0]));
		string token = Convert.ToString(Invoke("CreateManagedChildControlToken", new object[0]));
		Equal(true, pipeName.StartsWith("MineHarbor.ManagedChild.", StringComparison.Ordinal), "관리 자식 제어 파이프 난수 이름");
		Equal(64, token.Length, "관리 자식 제어 토큰 길이");
		Invoke("ValidateManagedChildControlValues", new object[] { pipeName, token });
		ExpectFailure(delegate { Invoke("ValidateManagedChildControlValues", new object[] { "MineHarbor.ManagedChild.bad", token }); }, "잘못된 관리 자식 파이프 거부");
		ExpectFailure(delegate { Invoke("ValidateManagedChildControlValues", new object[] { pipeName, new string('0', 63) }); }, "잘못된 관리 자식 토큰 거부");

		int currentProcessId;
		long currentProcessStartTicks;
		using (Process current = Process.GetCurrentProcess())
		{
			currentProcessId = current.Id;
			currentProcessStartTicks = current.StartTime.Ticks;
		}
		Equal(true, Invoke("IsExactProcessIdentityAlive", new object[] { currentProcessId, currentProcessStartTicks }), "PID와 시작 시각 소유자 확인");
		Equal(false, Invoke("IsExactProcessIdentityAlive", new object[] { currentProcessId, currentProcessStartTicks + 1 }), "PID 재사용 시작 시각 불일치 차단");
		SetStaticField("ManagedChildOwnerProcessId", 123);
		SetStaticField("ManagedChildOwnerProcessStartTicks", 456L);
		object[] wrongOwner = { 124, 456L, currentProcessId, currentProcessStartTicks, null };
		Equal(false, Invoke("TryTransferManagedChildOwner", wrongOwner), "현재 소유자가 다른 인계 거부");
		object[] validOwner = { 123, 456L, currentProcessId, currentProcessStartTicks, null };
		Equal(true, Invoke("TryTransferManagedChildOwner", validOwner), "검증된 새 소유자로 원자적 전환");
		Equal(currentProcessId, GetStaticField("ManagedChildOwnerProcessId"), "전환된 소유자 PID");
		Equal(currentProcessStartTicks, GetStaticField("ManagedChildOwnerProcessStartTicks"), "전환된 소유자 시작 시각");
		((IList)GetStaticField("ManagedChildOutputLines")).Clear();
		Invoke("CaptureManagedChildOutput", new object[] { "인계 제어 로그" });
		Type safeWriterType = launcher.GetNestedType("ManagedChildSafeTeeWriter", BindingFlags.NonPublic);
		using (TextWriter safeWriter = (TextWriter)Activator.CreateInstance(
			safeWriterType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { new ThrowingTextWriter() },
			null))
		{
			safeWriter.WriteLine("부모 출력 종료 후 로그");
		}
		Equal(2, ((IList)GetStaticField("ManagedChildOutputLines")).Count, "부모 출력 파이프 종료 후 자식 로그 유지");
		Type controlServerType = launcher.GetNestedType("ManagedChildControlServer", BindingFlags.NonPublic);
		object controlServer = Activator.CreateInstance(
			controlServerType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { "인계 테스트 서버", pipeName, token },
			null);
		InvokeInstance(controlServer, "Start", new object[0]);
		try
		{
			object statusRequest = Invoke("NewManagedChildControlRequest", new object[] { token, "status", null });
			object statusResponse = null;
			for (int attempt = 0; attempt < 20 && statusResponse == null; attempt++)
			{
				Thread.Sleep(25);
				statusResponse = Invoke("SendManagedChildControlRequest", new object[] { pipeName, statusRequest, 500 });
			}
			if (statusResponse == null) throw new InvalidOperationException("실제 관리 자식 제어 파이프에 연결하지 못했습니다.");
			Equal(true, GetField(statusResponse, "Success"), "현재 사용자 관리 자식 제어 파이프");
			Equal(currentProcessId, GetField(statusResponse, "OwnerProcessId"), "제어 파이프 소유자 상태");
			object logsRequest = Invoke("NewManagedChildControlRequest", new object[] { token, "logs", null });
			object logsResponse = Invoke("SendManagedChildControlRequest", new object[] { pipeName, logsRequest, 1000 });
			IList controlLines = (IList)GetField(logsResponse, "Lines");
			Equal(2, controlLines.Count, "자식 보유 최근 로그 조회");
			object wrongTokenRequest = Invoke("NewManagedChildControlRequest", new object[] { new string('0', 64), "status", null });
			object wrongTokenResponse = Invoke("SendManagedChildControlRequest", new object[] { pipeName, wrongTokenRequest, 1000 });
			Equal(false, GetField(wrongTokenResponse, "Success"), "잘못된 제어 토큰 인증 거부");
		}
		finally
		{
			((IDisposable)controlServer).Dispose();
		}

		Type settingsType = launcher.GetNestedType("BackgroundAgentSettings", BindingFlags.NonPublic);
		object settings = Activator.CreateInstance(settingsType, true);
		SetPublic(settings, "Enabled", true);
		Invoke("WriteBackgroundAgentSettings", new object[] { settings });

		int commands = 0;
		bool loseFirstTransferResponse = true;
		JavaScriptSerializer serializer = new JavaScriptSerializer();
		Func<string, string, int, string> transport = delegate(string requestedPipe, string requestJson, int timeout)
		{
			Equal(pipeName, requestedPipe, "인계 제어 파이프 대상");
			Dictionary<string, object> request = serializer.Deserialize<Dictionary<string, object>>(requestJson);
			Equal(token, Convert.ToString(request["Token"]), "인계 제어 토큰 전달");
			string command = Convert.ToString(request["Command"]);
			int ownerId = currentProcessId;
			long ownerStart = currentProcessStartTicks;
			if (string.Equals(command, "transfer-owner", StringComparison.Ordinal))
			{
				ownerId = Convert.ToInt32(request["NewOwnerProcessId"]);
				ownerStart = Convert.ToInt64(request["NewOwnerProcessStartTicks"]);
			}
			if (string.Equals(command, "command", StringComparison.Ordinal)) commands++;
			Dictionary<string, object> response = new Dictionary<string, object>
			{
				{ "SchemaVersion", 1 },
				{ "Success", true },
				{ "Message", "ok" },
				{ "Profile", "인계 테스트 서버" },
				{ "ChildProcessId", currentProcessId },
				{ "ChildProcessStartTicks", currentProcessStartTicks },
				{ "OwnerProcessId", ownerId },
				{ "OwnerProcessStartTicks", ownerStart },
				{ "ServerRunning", true },
				{ "Lines", string.Equals(command, "logs", StringComparison.Ordinal) ? new string[] { "인계 전 로그", "인계 후 로그" } : new string[0] }
			};
			if (string.Equals(command, "transfer-owner", StringComparison.Ordinal) && loseFirstTransferResponse)
			{
				loseFirstTransferResponse = false;
				return null;
			}
			return serializer.Serialize(response);
		};
		SetStaticField("ManagedChildControlTransportOverride", transport);

		Type descriptorType = launcher.GetNestedType("ManagedChildHandoffDescriptor", BindingFlags.NonPublic);
		object descriptor = Activator.CreateInstance(descriptorType, true);
		SetPublic(descriptor, "Profile", "인계 테스트 서버");
		SetPublic(descriptor, "PipeName", pipeName);
		SetPublic(descriptor, "Token", token);
		SetPublic(descriptor, "ChildProcessId", currentProcessId);
		SetPublic(descriptor, "ChildProcessStartTicks", currentProcessStartTicks);
		SetPublic(descriptor, "OwnerProcessId", currentProcessId);
		SetPublic(descriptor, "OwnerProcessStartTicks", currentProcessStartTicks);
		string descriptorJson = serializer.Serialize(descriptor);
		Equal("인계 테스트 서버", GetField(Invoke("ParseManagedChildHandoffDescriptor", new object[] { descriptorJson, "인계 테스트 서버" }), "Profile"), "인계 설명 검증");
		string invalidDescriptor = descriptorJson.Replace(token, new string('z', 64));
		ExpectFailure(delegate { Invoke("ParseManagedChildHandoffDescriptor", new object[] { invalidDescriptor, "인계 테스트 서버" }); }, "잘못된 인계 토큰 거부");
		object previousOwnerDescriptor = Activator.CreateInstance(descriptorType, true);
		SetPublic(previousOwnerDescriptor, "Profile", "인계 테스트 서버");
		SetPublic(previousOwnerDescriptor, "PipeName", pipeName);
		SetPublic(previousOwnerDescriptor, "Token", token);
		SetPublic(previousOwnerDescriptor, "ChildProcessId", currentProcessId);
		SetPublic(previousOwnerDescriptor, "ChildProcessStartTicks", currentProcessStartTicks);
		SetPublic(previousOwnerDescriptor, "OwnerProcessId", 123);
		SetPublic(previousOwnerDescriptor, "OwnerProcessStartTicks", 456L);
		Equal(true, Invoke("IsManagedChildHandoffComplete", new object[] { previousOwnerDescriptor }), "인계 응답 유실 후 새 소유자 상태 복구");

		Type contextType = launcher.GetNestedType("BackgroundAgentContext", BindingFlags.NonPublic);
		object context = Activator.CreateInstance(contextType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { settings }, null);
		try
		{
			object ping = null;
			for (int attempt = 0; attempt < 20 && ping == null; attempt++)
			{
				Thread.Sleep(50);
				ping = Invoke("SendBackgroundAgentRequest", new object[] { "ping", null, null, 500 });
			}
			if (ping == null) throw new InvalidOperationException("인계 테스트 에이전트 IPC에 연결하지 못했습니다.");
			object adopt = Invoke("SendBackgroundAgentRequest", new object[] { "adopt", "인계 테스트 서버", descriptorJson, 3000 });
			Equal(true, GetField(adopt, "Success"), "소유권 전환 응답 유실 후 실행 중 서버 인계");
			object duplicate = Invoke("SendBackgroundAgentRequest", new object[] { "adopt", "인계 테스트 서버", descriptorJson, 3000 });
			Equal(true, GetField(duplicate, "Success"), "인계 응답 유실 후 중복 요청 멱등 처리");
			object status = Invoke("SendBackgroundAgentRequest", new object[] { "status", null, null, 1000 });
			IList profileStates = (IList)GetField(status, "Profiles");
			Equal(1, profileStates.Count, "인계 서버 에이전트 상태 등록");
			Equal(currentProcessId, GetField(profileStates[0], "ProcessId"), "인계 서버 프로세스 ID 유지");
			object commandResponse = Invoke("SendBackgroundAgentRequest", new object[] { "command", "인계 테스트 서버", "list", 1000 });
			Equal(true, GetField(commandResponse, "Success"), "인계 서버 명령 제어 채널");
			Equal(1, commands, "인계 서버 명령 한 번만 전달");
			object logs = Invoke("SendBackgroundAgentRequest", new object[] { "logs", "인계 테스트 서버", null, 1000 });
			Equal(true, GetField(logs, "Success"), "인계 서버 로그 제어 채널");
			IList lines = (IList)GetField(logs, "Lines");
			Equal(2, lines.Count, "인계 전후 최근 로그 유지");
			Type dashboardType = launcher.GetNestedType("MultiServerDashboardForm", BindingFlags.NonPublic);
			using (Form dashboard = (Form)Activator.CreateInstance(
				dashboardType,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new object[] { dataRoot, false },
				null))
			{
				dashboardType.GetField("handoffInProgress", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dashboard, true);
				FormClosingEventArgs closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
				InvokeInstance(dashboard, "OnDashboardClosing", new object[] { dashboard, closing });
				Equal(true, closing.Cancel, "인계 진행 중 추가 창 닫기 차단");
			}
		}
		finally
		{
			((IDisposable)context).Dispose();
			SetStaticField("ManagedChildControlTransportOverride", null);
			SetStaticField("ManagedChildOwnerProcessId", -1);
			SetStaticField("ManagedChildOwnerProcessStartTicks", -1L);
			SetStaticField("BackgroundAgentDisableTrayForTests", false);
			SetStaticField("BackgroundAgentSettingsPathOverride", null);
			SetStaticField("BackgroundAgentPipeNameOverride", null);
			SetStaticField("WindowsNotificationSettingsPathOverride", null);
			SetStaticField("StorageSettingsPathOverride", Path.Combine(root, "storage.properties"));
			SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "isolated-user-data"));
		}
		Pass();
	}

	private static void TestWindowsNotifications(string root)
	{
		string settingsPath = Path.Combine(root, "windows-notifications", "settings.json");
		SetStaticField("WindowsNotificationSettingsPathOverride", settingsPath);
		Type settingsType = launcher.GetNestedType("WindowsNotificationSettings", BindingFlags.NonPublic);
		object defaults = Invoke("ReadWindowsNotificationSettings", new object[0]);
		Equal(1, GetField(defaults, "SchemaVersion"), "Windows 알림 설정 기본 스키마");
		Equal(false, GetField(defaults, "Enabled"), "Windows 알림 기본 비활성화");
		Equal("warning", GetField(defaults, "MinimumSeverity"), "Windows 알림 기본 최소 중요도");

		object settings = Activator.CreateInstance(settingsType, true);
		SetPublic(settings, "Enabled", true);
		SetPublic(settings, "MinimumSeverity", "warning");
		SetPublic(settings, "QuietHoursEnabled", true);
		SetPublic(settings, "QuietStartMinutes", 22 * 60);
		SetPublic(settings, "QuietEndMinutes", 7 * 60);
		Invoke("WriteWindowsNotificationSettings", new object[] { settings });
		object loaded = Invoke("ReadWindowsNotificationSettings", new object[0]);
		Equal(true, GetField(loaded, "Enabled"), "Windows 알림 동의 저장");
		Equal("warning", GetField(loaded, "MinimumSeverity"), "Windows 알림 중요도 저장");
		Equal(true, Invoke("IsWithinWindowsNotificationQuietHours", new object[] { loaded, new DateTime(2026, 7, 26, 23, 30, 0) }), "자정 경계 조용한 시간");
		Equal(true, Invoke("IsWithinWindowsNotificationQuietHours", new object[] { loaded, new DateTime(2026, 7, 26, 6, 30, 0) }), "새벽 조용한 시간");
		Equal(false, Invoke("IsWithinWindowsNotificationQuietHours", new object[] { loaded, new DateTime(2026, 7, 26, 12, 0, 0) }), "주간 알림 허용");
		Equal(false, Invoke("ShouldDeliverWindowsNotification", new object[] { loaded, "server", "info", new DateTime(2026, 7, 26, 12, 0, 0) }), "최소 중요도 미만 차단");
		Equal(true, Invoke("ShouldDeliverWindowsNotification", new object[] { loaded, "server", "error", new DateTime(2026, 7, 26, 12, 0, 0) }), "서버 오류 알림 허용");
		SetPublic(loaded, "ServerEvents", false);
		Equal(false, Invoke("ShouldDeliverWindowsNotification", new object[] { loaded, "server", "error", new DateTime(2026, 7, 26, 12, 0, 0) }), "비활성 종류 알림 차단");
		SetPublic(loaded, "ServerEvents", true);
		SetPublic(loaded, "QuietHoursEnabled", false);
		SetPublic(loaded, "MinimumSeverity", "info");
		Invoke("WriteWindowsNotificationSettings", new object[] { loaded });

		string notificationData = Path.Combine(root, "windows-notification-data");
		string storagePath = Path.Combine(root, "windows-notification-storage.properties");
		SetStaticField("StorageSettingsPathOverride", storagePath);
		SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "windows-notification-user"));
		Invoke("SaveDataStorageSettings", new object[] { "custom", notificationData });
		string server = Path.Combine(notificationData, "servers", "알림 테스트 서버");
		Directory.CreateDirectory(server);
		File.WriteAllText(Path.Combine(server, ".launcher-properties-configured"), "launcher-settings-version=7\r\nprofile-name=알림 테스트 서버\r\nserver-type=paper\r\nminecraft-version=1.21.11\r\nmemory-gb=2\r\n", Encoding.UTF8);
		File.WriteAllText(Path.Combine(server, "server.properties"), "server-port=25565\r\n", Encoding.UTF8);
		Invoke("RecordOperationEvent", new object[] { server, "server", "warning", "기존 알림", "Existing notification", "launcher", false });

		int displayed = 0;
		string displayedTitle = null;
		string displayedMessage = null;
		SetStaticField("WindowsNotificationDisplayOverride", new Action<string, string, ToolTipIcon>(delegate(string title, string message, ToolTipIcon icon)
		{
			displayed++;
			displayedTitle = title;
			displayedMessage = message;
		}));
		Type monitorType = launcher.GetNestedType("WindowsNotificationMonitor", BindingFlags.NonPublic);
		object monitor = Activator.CreateInstance(monitorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { null }, null);
		DateTime initialUtc = new DateTime(2026, 7, 26, 3, 0, 0, DateTimeKind.Utc);
		DateTime daytime = new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Local);
		InvokeInstance(monitor, "PollAt", new object[] { initialUtc, daytime });
		Equal(0, displayed, "에이전트 시작 전 알림 재생 방지");
		Invoke("RecordOperationEvent", new object[] { server, "server", "error", "경로 " + server + " IP=192.168.0.10 token=secret", "Path " + server + " IP=192.168.0.10 token=secret", "recovery", false });
		InvokeInstance(monitor, "PollAt", new object[] { initialUtc.AddSeconds(1), daytime });
		Equal(1, displayed, "새 운영 오류 Windows 알림 전달");
		if (displayedTitle == null || displayedTitle.IndexOf("알림 테스트 서버", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("Windows 알림에 서버 식별 정보가 없습니다.");
		if (displayedMessage == null || displayedMessage.Contains(server) || displayedMessage.Contains("192.168.0.10") || displayedMessage.Contains("secret"))
			throw new InvalidOperationException("Windows 알림 민감 정보 가림이 적용되지 않았습니다.");
		Invoke("RecordOperationEvent", new object[] { server, "backup", "warning", "백업 경고", "Backup warning", "background-agent", false });
		Invoke("RecordOperationEvent", new object[] { server, "automation", "error", "예약 실패", "Scheduled job failed", "automation", false });
		InvokeInstance(monitor, "PollAt", new object[] { initialUtc.AddSeconds(10), daytime });
		Equal(2, displayed, "동시 알림 요약 전달");
		if (displayedMessage.IndexOf("1", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("동시 Windows 알림 요약 개수가 없습니다.");

		File.WriteAllText(settingsPath, "{\"SchemaVersion\":999}", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadWindowsNotificationSettings", new object[0]); }, "미래 Windows 알림 설정 스키마 거부");
		Equal("{\"SchemaVersion\":999}", File.ReadAllText(settingsPath, Encoding.UTF8), "미래 Windows 알림 설정 원본 보존");
		File.WriteAllText(settingsPath, "{ broken", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadWindowsNotificationSettings", new object[0]); }, "손상된 Windows 알림 설정 차단");
		Equal("{ broken", File.ReadAllText(settingsPath, Encoding.UTF8), "손상된 Windows 알림 설정 원본 보존");
		Type formType = launcher.GetNestedType("WindowsNotificationSettingsForm", BindingFlags.NonPublic);
		using (Form blockedForm = (Form)Activator.CreateInstance(formType, true))
		{
			Equal(false, InvokeInstance(blockedForm, "SaveSettings", new object[] { false }), "손상된 Windows 알림 설정 UI 덮어쓰기 차단");
			Equal("{ broken", File.ReadAllText(settingsPath, Encoding.UTF8), "Windows 알림 설정 UI 원본 보존");
		}
		File.Delete(settingsPath);

		using (Form form = (Form)Activator.CreateInstance(formType, true))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "Windows 알림 설정 UI DPI 배율");
			CheckBox enabledBox = (CheckBox)GetPrivateField(formType, form, "enabledBox");
			ComboBox severityBox = (ComboBox)GetPrivateField(formType, form, "severityBox");
			if (string.IsNullOrWhiteSpace(enabledBox.AccessibleName) || string.IsNullOrWhiteSpace(enabledBox.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(severityBox.AccessibleName) || string.IsNullOrWhiteSpace(severityBox.AccessibleDescription))
				throw new InvalidOperationException("Windows 알림 설정 접근성 정보가 없습니다.");
		}

		SetStaticField("WindowsNotificationSettingsPathOverride", null);
		SetStaticField("WindowsNotificationDisplayOverride", null);
		SetStaticField("StorageSettingsPathOverride", Path.Combine(root, "storage.properties"));
		SetStaticField("LauncherUserDataDirectoryOverride", Path.Combine(root, "isolated-user-data"));
		Pass();
	}

	private static void TestDiscordRemoteManagement(string root)
	{
		string settingsPath = Path.Combine(root, "discord-remote", "settings.json");
		Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
		SetStaticField("DiscordRemoteSettingsPathOverride", settingsPath);
		Type settingsType = launcher.GetNestedType("DiscordRemoteSettings", BindingFlags.NonPublic);
		object defaults = Invoke("ReadDiscordRemoteSettings", new object[0]);
		Equal(1, GetField(defaults, "SchemaVersion"), "Discord 원격 설정 기본 스키마");
		Equal(false, GetField(defaults, "Enabled"), "Discord 원격 기본 비활성화");
		Equal(false, Invoke("IsDiscordRemoteRegistrationConfigured", new object[] { defaults }), "Discord 미등록 상태 판정");
		Equal(true, Invoke("ShouldShowDiscordRegistrationGuide", new object[0]), "Discord 미등록 진입 가이드 표시");

		string clearToken = "test.token." + new string('x', 40);
		string protectedToken = Convert.ToString(Invoke("ProtectDiscordBotToken", new object[] { clearToken }));
		if (string.IsNullOrWhiteSpace(protectedToken) || protectedToken.Contains(clearToken))
			throw new InvalidOperationException("Discord 봇 토큰이 안전하게 암호화되지 않았습니다.");
		Equal(clearToken, Invoke("UnprotectDiscordBotToken", new object[] { protectedToken }), "현재 사용자 DPAPI Discord 토큰 복호화");

		const string applicationId = "11111111111111111";
		const string guildId = "22222222222222222";
		const string channelId = "33333333333333333";
		const string userId = "44444444444444444";
		const string secondUserId = "55555555555555555";
		const string roleId = "66666666666666666";
		const string profile = "Discord 테스트 서버";
		object settings = Activator.CreateInstance(settingsType, true);
		SetPublic(settings, "Enabled", true);
		SetPublic(settings, "ProtectedBotToken", protectedToken);
		SetPublic(settings, "ApplicationId", applicationId);
		SetPublic(settings, "GuildId", guildId);
		SetPublic(settings, "ChannelId", channelId);
		SetPublic(settings, "AllowedUserIds", new List<string> { userId, secondUserId });
		SetPublic(settings, "AllowedRoleIds", new List<string> { roleId });
		SetPublic(settings, "AllowedProfiles", new List<string> { profile });
		Invoke("WriteDiscordRemoteSettings", new object[] { settings });
		object loaded = Invoke("ReadDiscordRemoteSettings", new object[0]);
		Equal(true, GetField(loaded, "Enabled"), "Discord 원격 명시적 동의 저장");
		Equal(profile, ((IList)GetField(loaded, "AllowedProfiles"))[0], "Discord 허용 프로필 저장");
		Equal(true, Invoke("IsDiscordRemoteRegistrationConfigured", new object[] { loaded }), "Discord 등록 완료 상태 판정");
		Equal(false, Invoke("ShouldShowDiscordRegistrationGuide", new object[0]), "Discord 등록 완료 후 자동 가이드 생략");
		string storedJson = File.ReadAllText(settingsPath, Encoding.UTF8);
		if (storedJson.Contains(clearToken)) throw new InvalidOperationException("Discord 봇 토큰이 설정 JSON에 평문으로 저장되었습니다.");
		IList parsedIds = (IList)Invoke("ParseDiscordIdText", new object[] { userId + ", " + secondUserId + "\r\n" + userId });
		Equal(2, parsedIds.Count, "Discord ID 목록 구분·중복 제거");
		ExpectFailure(delegate { Invoke("ParseDiscordIdText", new object[] { "not-an-id" }); }, "잘못된 Discord ID 거부");
		Type gatewayType = launcher.GetNestedType("DiscordGatewayClient", BindingFlags.NonPublic);
		MethodInfo createCommand = gatewayType.GetMethod("CreateMineHarborCommandDefinition", BindingFlags.Static | BindingFlags.NonPublic);
		IDictionary commandDefinition = (IDictionary)createCommand.Invoke(null, new object[0]);
		Equal(8, ((IList)commandDefinition["options"]).Count, "Discord 길드 명령 하위 명령 수");
		if (commandDefinition.Contains("contexts") || commandDefinition.Contains("integration_types"))
			throw new InvalidOperationException("전역 명령 전용 Discord context 필드가 길드 명령 등록 본문에 포함되었습니다.");

		Type processorType = launcher.GetNestedType("DiscordInteractionProcessor", BindingFlags.NonPublic);
		int actions = 0;
		Func<string, string, string, bool, string> actionOverride = delegate(string command, string selectedProfile, string actor, bool korean)
		{
			actions++;
			return "OK " + command + " " + selectedProfile;
		};
		SetStaticField("DiscordRemoteActionOverride", actionOverride);
		object processor = Activator.CreateInstance(
			processorType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { loaded, null },
			null);
		DateTime now = new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

		Dictionary<string, object> wrongApplicationInteraction =
			NewDiscordInteraction("70000000000000000", 2, guildId, channelId, userId, null, "status", profile, null, "ko");
		wrongApplicationInteraction["application_id"] = "99999999999999999";
		object wrongApplication = InvokeInstance(processor, "Process", new object[] { wrongApplicationInteraction, now });
		if (Convert.ToString(GetField(wrongApplication, "Content")).IndexOf("권한", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("다른 Discord 애플리케이션 요청이 차단되지 않았습니다.");
		Equal(0, actions, "다른 Discord 애플리케이션 요청은 서버 작업 미실행");

		object unauthorized = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000001", 2, "99999999999999999", channelId, userId, null, "status", profile, null, "ko"),
			now
		});
		if (Convert.ToString(GetField(unauthorized, "Content")).IndexOf("권한", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("허용되지 않은 Discord 길드 요청이 차단되지 않았습니다.");
		Equal(0, actions, "권한 없는 Discord 요청은 서버 작업 미실행");

		object status = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000002", 2, guildId, channelId, userId, null, "status", profile, null, "ko"),
			now
		});
		if (Convert.ToString(GetField(status, "Content")).IndexOf("OK status", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("허용된 Discord 상태 요청이 처리되지 않았습니다.");
		Equal(1, actions, "허용된 Discord 상태 작업 단일 실행");

		object unknown = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000003", 2, guildId, channelId, userId, null, "command", profile, null, "ko"),
			now
		});
		if (Convert.ToString(GetField(unknown, "Content")).IndexOf("지원하지", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("Discord 임의 콘솔 명령 진입이 차단되지 않았습니다.");
		Equal(1, actions, "Discord 임의 콘솔 명령 서버 작업 미실행");

		object roleAuthorized = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000004", 2, guildId, channelId, "77777777777777777", new string[] { roleId }, "status", profile, null, "en-US"),
			now
		});
		if (Convert.ToString(GetField(roleAuthorized, "Content")).IndexOf("OK status", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("허용된 Discord 역할 요청이 처리되지 않았습니다.");

		object stopRequest = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000005", 2, guildId, channelId, userId, null, "stop", profile, null, "ko"),
			now
		});
		IList rows = (IList)GetField(stopRequest, "Components");
		Equal(1, rows.Count, "Discord 안전 종료 확인 행");
		IDictionary row = (IDictionary)rows[0];
		IList buttons = (IList)row["components"];
		Equal(2, buttons.Count, "Discord 안전 종료 확인·취소 버튼");
		string confirmId = Convert.ToString(((IDictionary)buttons[0])["custom_id"]);
		Equal(2, actions, "Discord 종료 선택만으로 서버 작업 미실행");

		object wrongOwner = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000006", 3, guildId, channelId, secondUserId, null, null, null, confirmId, "ko"),
			now.AddSeconds(1)
		});
		if (Convert.ToString(GetField(wrongOwner, "Content")).IndexOf("권한", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("다른 Discord 사용자의 확인 버튼 사용이 차단되지 않았습니다.");
		Equal(2, actions, "다른 사용자의 확인으로 서버 작업 미실행");

		object confirmed = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000007", 3, guildId, channelId, userId, null, null, null, confirmId, "ko"),
			now.AddSeconds(2)
		});
		if (Convert.ToString(GetField(confirmed, "Content")).IndexOf("OK stop", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("Discord 안전 종료 확인이 실행되지 않았습니다.");
		Equal(3, actions, "Discord 안전 종료 확인 후 단일 실행");

		object replay = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000008", 3, guildId, channelId, userId, null, null, null, confirmId, "ko"),
			now.AddSeconds(3)
		});
		if (Convert.ToString(GetField(replay, "Content")).IndexOf("이미", StringComparison.OrdinalIgnoreCase) < 0
			&& Convert.ToString(GetField(replay, "Content")).IndexOf("만료", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("Discord 확인 버튼 재사용이 차단되지 않았습니다.");
		Equal(3, actions, "Discord 확인 버튼 재사용 서버 작업 미실행");

		object expiringRequest = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000009", 2, guildId, channelId, userId, null, "restart", profile, null, "ko"),
			now
		});
		IList expiringRows = (IList)GetField(expiringRequest, "Components");
		string expiringId = Convert.ToString(((IDictionary)((IList)((IDictionary)expiringRows[0])["components"])[0])["custom_id"]);
		object expired = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000010", 3, guildId, channelId, userId, null, null, null, expiringId, "ko"),
			now.AddSeconds(61)
		});
		if (Convert.ToString(GetField(expired, "Content")).IndexOf("만료", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("Discord 확인 요청 만료가 적용되지 않았습니다.");
		Equal(3, actions, "만료된 Discord 확인 서버 작업 미실행");

		object autocomplete = InvokeInstance(processor, "Process", new object[]
		{
			NewDiscordInteraction("70000000000000011", 4, guildId, channelId, userId, null, "status", "Dis", null, "ko"),
			now
		});
		Equal(true, GetField(autocomplete, "IsAutocomplete"), "Discord 서버 자동완성 응답");
		Equal(1, ((IList)GetField(autocomplete, "Choices")).Count, "허용된 Discord 서버만 자동완성");

		// 서버 지정 없는 전체 상태 조회는 프로필마다 조회를 한 번씩 하므로 개수를 제한하고,
		// 응답 길이 상한에 걸려 남은 서버가 조용히 사라지지 않도록 생략 수를 알려야 합니다.
		object manyProfileSettings = Activator.CreateInstance(settingsType, true);
		SetPublic(manyProfileSettings, "Enabled", true);
		SetPublic(manyProfileSettings, "ProtectedBotToken", protectedToken);
		SetPublic(manyProfileSettings, "ApplicationId", applicationId);
		SetPublic(manyProfileSettings, "GuildId", guildId);
		SetPublic(manyProfileSettings, "ChannelId", channelId);
		SetPublic(manyProfileSettings, "AllowedUserIds", new List<string> { userId });
		SetPublic(manyProfileSettings, "AllowedRoleIds", new List<string>());
		List<string> bulkProfiles = new List<string>();
		for (int index = 0; index < 20; index++) bulkProfiles.Add("대량 서버 " + index.ToString("00"));
		SetPublic(manyProfileSettings, "AllowedProfiles", bulkProfiles);
		int bulkStatusCalls = 0;
		SetStaticField("DiscordRemoteActionOverride", new Func<string, string, string, bool, string>(
			delegate(string command, string selectedProfile, string actor, bool korean)
			{
				bulkStatusCalls++;
				return "`" + selectedProfile + "` · OK";
			}));
		object bulkProcessor = Activator.CreateInstance(
			processorType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { manyProfileSettings, null },
			null);
		object bulkStatus = InvokeInstance(bulkProcessor, "Process", new object[]
		{
			NewDiscordInteraction("73000000000000000", 2, guildId, channelId, userId, null, "status", null, null, "ko"),
			now
		});
		Equal(15, bulkStatusCalls, "Discord 전체 상태 조회 서버 수 제한");
		string bulkContent = Convert.ToString(GetField(bulkStatus, "Content"));
		if (bulkContent.Length > 1800)
			throw new InvalidOperationException("Discord 전체 상태 응답 길이 상한이 적용되지 않았습니다.");
		if (bulkContent.IndexOf("그 외 5개 서버는 생략", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("Discord 전체 상태에서 생략된 서버 수가 안내되지 않았습니다.");
		SetStaticField("DiscordRemoteActionOverride", actionOverride);

		object limitedProcessor = Activator.CreateInstance(
			processorType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { loaded, null },
			null);
		object limited = null;
		for (int index = 0; index < 11; index++)
		{
			limited = InvokeInstance(limitedProcessor, "Process", new object[]
			{
				NewDiscordInteraction((71000000000000000L + index).ToString(), 2, guildId, channelId, userId, null, "help", null, null, "ko"),
				now
			});
		}
		if (Convert.ToString(GetField(limited, "Content")).IndexOf("너무", StringComparison.OrdinalIgnoreCase) < 0)
			throw new InvalidOperationException("Discord 사용자별 요청 속도 제한이 적용되지 않았습니다.");

		SetStaticField("DiscordRemoteActionOverride", new Func<string, string, string, bool, string>(delegate { return new string('x', 2400); }));
		object trimProcessor = Activator.CreateInstance(
			processorType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			null,
			new object[] { loaded, null },
			null);
		object trimmed = InvokeInstance(trimProcessor, "Process", new object[]
		{
			NewDiscordInteraction("72000000000000000", 2, guildId, channelId, userId, null, "status", profile, null, "ko"),
			now
		});
		if (Convert.ToString(GetField(trimmed, "Content")).Length > 1800)
			throw new InvalidOperationException("Discord 응답 길이 상한이 적용되지 않았습니다.");
		SetStaticField("DiscordRemoteActionOverride", null);

		File.WriteAllText(settingsPath, "{\"SchemaVersion\":999}", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadDiscordRemoteSettings", new object[0]); }, "미래 Discord 설정 스키마 거부");
		Equal("{\"SchemaVersion\":999}", File.ReadAllText(settingsPath, Encoding.UTF8), "미래 Discord 설정 원본 보존");
		File.WriteAllText(settingsPath, "{ broken", Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadDiscordRemoteSettings", new object[0]); }, "손상된 Discord 설정 차단");
		Equal("{ broken", File.ReadAllText(settingsPath, Encoding.UTF8), "손상된 Discord 설정 원본 보존");
		File.Delete(settingsPath);
		SetPublic(settings, "Enabled", false);
		Invoke("WriteDiscordRemoteSettings", new object[] { settings });
		Type formType = launcher.GetNestedType("DiscordRemoteSettingsForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(formType, true))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "Discord 원격 설정 UI DPI 배율");
			CheckBox enabled = (CheckBox)GetPrivateField(formType, form, "enabledBox");
			TextBox token = (TextBox)GetPrivateField(formType, form, "tokenBox");
			TextBox channel = (TextBox)GetPrivateField(formType, form, "channelIdBox");
			TextBox allowedUsers = (TextBox)GetPrivateField(formType, form, "allowedUsersBox");
			TextBox allowedRoles = (TextBox)GetPrivateField(formType, form, "allowedRolesBox");
			CheckedListBox profiles = (CheckedListBox)GetPrivateField(formType, form, "profilesBox");
			Label validation = (Label)GetPrivateField(formType, form, "validationLabel");
			Button guideButton = (Button)GetPrivateField(formType, form, "guideButton");
			Button saveButton = (Button)GetPrivateField(formType, form, "saveButton");
			if (string.IsNullOrWhiteSpace(enabled.AccessibleName) || string.IsNullOrWhiteSpace(enabled.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(token.AccessibleName) || string.IsNullOrWhiteSpace(token.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(allowedUsers.AccessibleName) || string.IsNullOrWhiteSpace(allowedUsers.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(allowedRoles.AccessibleName) || string.IsNullOrWhiteSpace(allowedRoles.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(profiles.AccessibleName) || string.IsNullOrWhiteSpace(profiles.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(guideButton.AccessibleName) || string.IsNullOrWhiteSpace(guideButton.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(saveButton.AccessibleName) || string.IsNullOrWhiteSpace(saveButton.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(validation.AccessibleName))
				throw new InvalidOperationException("Discord 원격 설정 접근성 정보가 없습니다.");
			if (!token.UseSystemPasswordChar) throw new InvalidOperationException("Discord 봇 토큰 입력이 가려지지 않습니다.");
			TableLayoutPanel connection = channel.Parent.Parent as TableLayoutPanel;
			if (connection == null || connection.RowCount != 10
				|| connection.RowStyles[8].SizeType != SizeType.Absolute
				|| Math.Abs(connection.RowStyles[8].Height - 42F) > 0.1F
				|| connection.RowStyles[9].SizeType != SizeType.Percent)
				throw new InvalidOperationException("Discord 채널 입력 뒤의 남는 공간이 입력 높이를 늘립니다.");
			Equal(true, allowedUsers.Multiline, "Discord 허용 사용자 다중 입력");
			Equal(true, allowedRoles.Multiline, "Discord 허용 역할 다중 입력");
			if (allowedUsers.MinimumSize.Height < 72 || allowedRoles.MinimumSize.Height < 72)
				throw new InvalidOperationException("Discord 허용 목록 입력 높이가 보장되지 않습니다.");
			TableLayoutPanel allowlists = allowedUsers.Parent.Parent.Parent as TableLayoutPanel;
			if (allowlists == null || !string.Equals(allowlists.Name, "discordAllowlists", StringComparison.Ordinal)
				|| allowlists.ColumnCount != 2 || allowlists.Controls.Count != 2)
				throw new InvalidOperationException("Discord 허용 사용자·역할 입력이 별도 보안 영역에 표시되지 않습니다.");

			allowedUsers.Clear();
			allowedRoles.Clear();
			if (profiles.Items.Count == 0) profiles.Items.Add(profile);
			profiles.SetItemChecked(0, true);
			enabled.Checked = true;
			MethodInfo validateInput = formType.GetMethod("TryValidateInput", BindingFlags.Instance | BindingFlags.NonPublic);
			object[] missingAllowlist = new object[] { null, null };
			Equal(false, validateInput.Invoke(form, missingAllowlist), "Discord 허용 목록 누락 즉시 검증");
			Equal(allowedUsers, missingAllowlist[1], "Discord 허용 목록 누락 입력 포커스 대상");
			allowedUsers.Text = userId;
			object[] validInput = new object[] { null, null };
			Equal(true, validateInput.Invoke(form, validInput), "Discord 허용 사용자 입력 후 연결 준비");

			// 여러 줄 입력의 예시 문구는 첫 줄 커서 위치에 맞춰야 하고, 한 줄 입력은 세로 가운데를 유지합니다.
			Equal(ContentAlignment.TopLeft, FindCueLabel(allowedUsers).TextAlign, "Discord 허용 사용자 예시 문구 첫 줄 정렬");
			Equal(ContentAlignment.TopLeft, FindCueLabel(allowedRoles).TextAlign, "Discord 허용 역할 예시 문구 첫 줄 정렬");
			Equal(ContentAlignment.MiddleLeft, FindCueLabel(channel).TextAlign, "Discord 채널 ID 예시 문구 세로 가운데 정렬");

			// 사용 설정과 토큰 제거는 함께 켤 수 없지만, 방금 누른 쪽이 말없이 되돌아가서는 안 됩니다.
			CheckBox removeToken = (CheckBox)GetPrivateField(formType, form, "removeTokenBox");
			Equal(true, removeToken.Enabled, "저장된 토큰이 있으면 제거 선택 가능");
			enabled.Checked = false;
			removeToken.Checked = true;
			enabled.Checked = true;
			Equal(false, removeToken.Checked, "Discord 사용 설정을 켜면 토큰 제거 자동 해제");
			Equal(true, enabled.Checked, "Discord 사용 설정이 조용히 되돌아가지 않음");
			removeToken.Checked = true;
			Equal(false, enabled.Checked, "Discord 토큰 제거를 켜면 원격 제어 해제");
			string removeTokenNotice = Convert.ToString(validation.Text);
			if (removeTokenNotice.IndexOf("토큰", StringComparison.Ordinal) < 0
				&& removeTokenNotice.IndexOf("token", StringComparison.OrdinalIgnoreCase) < 0)
				throw new InvalidOperationException("Discord 토큰 제거 결과 안내가 표시되지 않았습니다.");
			removeToken.Checked = false;

			// 창을 닫으면 평문 봇 토큰이 입력 컨트롤에 남지 않아야 합니다.
			token.Text = "test.token." + new string('y', 40);
			formType.GetMethod("OnFormClosed", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(form, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
			Equal(0, token.TextLength, "Discord 창을 닫으면 평문 봇 토큰 제거");
		}
		Type guideType = launcher.GetNestedType("DiscordRemoteRegistrationGuideForm", BindingFlags.NonPublic);
		using (Form guide = (Form)Activator.CreateInstance(guideType, true))
		{
			Equal(AutoScaleMode.Dpi, guide.AutoScaleMode, "Discord 등록 가이드 DPI 배율");
			Equal(false, guide.AutoScroll, "Discord 등록 가이드 불필요한 전체 스크롤 없음");
			Panel stepsViewport = (Panel)GetPrivateField(guideType, guide, "stepsViewport");
			TableLayoutPanel steps = (TableLayoutPanel)GetPrivateField(guideType, guide, "stepsPanel");
			Equal(true, stepsViewport.AutoScroll, "Discord 등록 단계의 작은 창 내부 스크롤");
			Equal(true, steps.AutoSize, "Discord 등록 단계 내용 기반 높이");
			Equal(4, steps.Controls.Count, "Discord 등록 가이드 단계 수");
			for (int index = 0; index < steps.RowStyles.Count; index++)
				Equal(SizeType.AutoSize, steps.RowStyles[index].SizeType, "Discord 등록 카드 내용 기반 행 " + index);
			IList descriptions = (IList)GetPrivateField(guideType, guide, "stepDescriptions");
			Equal(4, descriptions.Count, "Discord 등록 설명 수");
			for (int index = 0; index < descriptions.Count; index++)
			{
				Label description = (Label)descriptions[index];
				Equal(true, description.AutoSize, "Discord 등록 설명 자동 높이 " + index);
				Equal(false, description.AutoEllipsis, "Discord 등록 설명 생략 금지 " + index);
				if (description.MaximumSize.Width <= 0 || description.MaximumSize.Height != 0)
					throw new InvalidOperationException("Discord 등록 설명의 줄바꿈 너비가 올바르지 않습니다.");
			}
			Label securityLabel = (Label)GetPrivateField(guideType, guide, "securityLabel");
			Equal(true, securityLabel.AutoSize, "Discord 보안 안내 자동 높이");
			Equal(false, securityLabel.AutoEllipsis, "Discord 보안 안내 생략 금지");
			Button portal = (Button)GetPrivateField(guideType, guide, "portalButton");
			Button start = (Button)GetPrivateField(guideType, guide, "startButton");
			Button later = (Button)GetPrivateField(guideType, guide, "laterButton");
			if (string.IsNullOrWhiteSpace(portal.AccessibleName) || string.IsNullOrWhiteSpace(portal.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(start.AccessibleName) || string.IsNullOrWhiteSpace(start.AccessibleDescription)
				|| string.IsNullOrWhiteSpace(later.AccessibleName) || string.IsNullOrWhiteSpace(later.AccessibleDescription))
				throw new InvalidOperationException("Discord 등록 가이드 접근성 정보가 없습니다.");
			Equal(start, guide.AcceptButton, "Discord 등록 가이드 Enter 기본 동작");
			Equal(later, guide.CancelButton, "Discord 등록 가이드 Esc 취소 동작");
		}
		SetStaticField("DiscordRemoteSettingsPathOverride", null);
		SetStaticField("DiscordRemoteRunOverride", null);
		SetStaticField("DiscordRemoteActionOverride", null);
		Pass();
	}

	private static Label FindCueLabel(TextBox textBox)
	{
		if (textBox == null || textBox.Parent == null) throw new InvalidOperationException("입력 예시 문구를 담을 표면이 없습니다.");
		foreach (Control child in textBox.Parent.Controls)
		{
			Label label = child as Label;
			if (label != null) return label;
		}
		throw new InvalidOperationException("입력 예시 문구 레이블을 찾지 못했습니다.");
	}

	private static Dictionary<string, object> NewDiscordInteraction(
		string id,
		int type,
		string guildId,
		string channelId,
		string userId,
		string[] roles,
		string command,
		string profile,
		string customId,
		string locale)
	{
		Dictionary<string, object> interaction = new Dictionary<string, object>
		{
			{ "id", id },
			{ "application_id", "11111111111111111" },
			{ "type", type },
			{ "guild_id", guildId },
			{ "channel_id", channelId },
			{ "locale", locale ?? "en-US" },
			{ "member", new Dictionary<string, object>
				{
					{ "user", new Dictionary<string, object> { { "id", userId } } },
					{ "roles", roles ?? new string[0] }
				}
			}
		};
		Dictionary<string, object> data = new Dictionary<string, object>();
		if (type == 3) data["custom_id"] = customId;
		else
		{
			data["name"] = "mineharbor";
			Dictionary<string, object> subcommand = new Dictionary<string, object>
			{
				{ "type", 1 },
				{ "name", command ?? string.Empty }
			};
			if (profile != null)
			{
				subcommand["options"] = new object[]
				{
					new Dictionary<string, object>
					{
						{ "type", 3 },
						{ "name", "server" },
						{ "value", profile },
						{ "focused", type == 4 }
					}
				};
			}
			data["options"] = new object[] { subcommand };
		}
		interaction["data"] = data;
		return interaction;
	}

	private static void TestOperationsHistory(string root)
	{
		string serversRoot = Path.Combine(root, "operations-root");
		string server = Path.Combine(serversRoot, "운영 기록 서버");
		Directory.CreateDirectory(server);
		File.WriteAllText(Path.Combine(server, ".launcher-properties-configured"), "launcher-settings-version=7\r\nprofile-name=운영 기록 서버\r\nserver-type=paper\r\nminecraft-version=1.21.11\r\nmemory-gb=2\r\n", Encoding.UTF8);
		File.WriteAllText(Path.Combine(server, "server.properties"), "server-port=25565\r\n", Encoding.UTF8);
		Invoke("RecordOperationEvent", new object[] { server, "server", "info", "서버 시작", "Server started", "launcher", false });
		Invoke("RecordOperationEvent", new object[] { server, "automation", "warning", "경로 " + server + "\n IP=192.168.1.2 token=secret", "Path " + server + "\n IP=192.168.1.2 token=secret", "automation", false });
		object document = Invoke("ReadOperationsHistory", new object[] { server });
		IList entries = (IList)GetField(document, "Entries");
		Equal(2, entries.Count, "운영 기록 항목 저장");
		object first = entries[0];
		object second = entries[1];
		Equal(GetField(first, "Hash"), GetField(second, "PreviousHash"), "운영 기록 연속 해시");
		string sanitized = Convert.ToString(GetField(second, "MessageKo"));
		if (sanitized.Contains(server) || sanitized.Contains("192.168.1.2") || sanitized.Contains("secret") || sanitized.Contains("\n")) throw new InvalidOperationException("운영 기록 민감 정보 가림이 적용되지 않았습니다.");
		Equal(true, Invoke("MarkOperationEventRead", new object[] { server, GetField(first, "Id"), true }), "운영 기록 읽음 처리");
		document = Invoke("ReadOperationsHistory", new object[] { server });
		Equal(true, GetField(((IList)GetField(document, "Entries"))[0], "IsRead"), "운영 기록 읽음 상태 저장");

		// IPv6로 접속한 플레이어 주소도 가리되, 같은 줄의 로그 시각은 남겨야 합니다.
		const string ipv6Line = "[12:34:56] Steve[/[2001:db8:85a3::8a2e:370:7334]:52341] lost connection";
		Invoke("RecordOperationEvent", new object[] { server, "server", "warning", ipv6Line, ipv6Line, "launcher", false });
		document = Invoke("ReadOperationsHistory", new object[] { server });
		entries = (IList)GetField(document, "Entries");
		string ipv6Sanitized = Convert.ToString(GetField(entries[entries.Count - 1], "MessageEn"));
		if (ipv6Sanitized.IndexOf("2001:db8", StringComparison.OrdinalIgnoreCase) >= 0)
			throw new InvalidOperationException("운영 기록 IPv6 주소 가림이 적용되지 않았습니다.");
		if (ipv6Sanitized.IndexOf("[IP]", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("운영 기록 IPv6 주소가 [IP]로 대체되지 않았습니다.");
		if (ipv6Sanitized.IndexOf("12:34:56", StringComparison.Ordinal) < 0)
			throw new InvalidOperationException("운영 기록 로그 시각이 IPv6 가림에 함께 지워졌습니다.");

		string historyPath = Convert.ToString(Invoke("GetOperationsHistoryPath", new object[] { server }));
		string validJson = File.ReadAllText(historyPath, Encoding.UTF8);
		string tamperedJson = validJson.Replace("서버 시작", "서버 변조");
		File.WriteAllText(historyPath, tamperedJson, Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadOperationsHistory", new object[] { server }); }, "운영 기록 해시 변조 차단");
		Equal(tamperedJson, File.ReadAllText(historyPath, Encoding.UTF8), "손상된 운영 기록 원본 보존");
		File.WriteAllText(historyPath, validJson, Encoding.UTF8);
		string futureJson = validJson.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":999");
		File.WriteAllText(historyPath, futureJson, Encoding.UTF8);
		ExpectFailure(delegate { Invoke("ReadOperationsHistory", new object[] { server }); }, "미래 운영 기록 스키마 거부");
		Equal(futureJson, File.ReadAllText(historyPath, Encoding.UTF8), "미래 운영 기록 원본 보존");
		File.WriteAllText(historyPath, validJson, Encoding.UTF8);

		Type formType = launcher.GetNestedType("OperationsHistoryForm", BindingFlags.NonPublic);
		using (Form form = (Form)Activator.CreateInstance(formType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { serversRoot }, null))
		{
			Equal(AutoScaleMode.Dpi, form.AutoScaleMode, "운영 기록 UI DPI 배율");
			ListView list = (ListView)GetPrivateField(formType, form, "historyList");
			if (string.IsNullOrWhiteSpace(list.AccessibleName) || string.IsNullOrWhiteSpace(list.AccessibleDescription)) throw new InvalidOperationException("운영 기록 목록 접근성 정보가 없습니다.");
		}
		Pass();
	}

	private static void TestDiagnosticRedaction(string root)
	{
		string server = Path.Combine(root, "server");
		Directory.CreateDirectory(server);
		string input = "owner-name=SecretOwner\nserver-ip=192.168.0.10\nrcon.password=secret\npath=" + server;
		string redacted = Convert.ToString(Invoke("RedactDiagnosticText", new object[] { input, server }));
		if (redacted.Contains("SecretOwner") || redacted.Contains("192.168.0.10") || redacted.Contains("secret") || redacted.Contains(server)) throw new InvalidOperationException("진단 정보 민감값이 남아 있습니다.");
		Pass();
	}

	private static void TestQuickCommandsAndBridge(string root)
	{
		IEnumerable builtIns = (IEnumerable)Invoke("GetBuiltInQuickCommands", new object[0]);
		List<string> templates = new List<string>();
		foreach (object item in builtIns) templates.Add(Convert.ToString(GetField(item, "Template")));
		if (templates.Count < 65 || !templates.Contains("list") || !templates.Contains("save-all flush") || !templates.Contains("whitelist off") || !templates.Contains("give {player} {item} [count=1]") || !templates.Contains("banlist players") || !templates.Contains("ban-ip {address-or-player} [reason]") || !templates.Contains("gamerule playersSleepingPercentage {percentage}") || !templates.Contains("datapack enable {datapack}") || !templates.Contains("reload")) throw new InvalidOperationException("기본 빠른 명령 목록이 완전하지 않습니다.");
		Type pickerLocalizationType = launcher.GetNestedType("Localization", BindingFlags.NonPublic);
		FieldInfo pickerLanguageField = pickerLocalizationType.GetField("CurrentLanguage", BindingFlags.Static | BindingFlags.Public);
		object originalPickerLanguage = pickerLanguageField.GetValue(null);
		try
		{
			pickerLanguageField.SetValue(null, "ko");
			IEnumerable koreanBuiltIns = (IEnumerable)Invoke("GetBuiltInQuickCommands", new object[0]);
			IEnumerable koreanPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { koreanBuiltIns, "paper", "1.21.11" });
			object hardPickerItem = FindQuickCommandPickerItem(koreanPickerItems, "difficulty hard");
			Equal("world", Convert.ToString(GetField(hardPickerItem, "CategoryKey")), "빠른 명령 월드 카테고리");
			Equal("difficulty", Convert.ToString(GetField(hardPickerItem, "GroupKey")), "난이도 명령 그룹화");
			Equal("어려움", Convert.ToString(GetField(hardPickerItem, "LeafName")), "난이도 공식 한글 명칭");
			Equal("월드 › 난이도 › 어려움", Convert.ToString(GetField(hardPickerItem, "CategoryName")) + " › " + Convert.ToString(GetField(hardPickerItem, "GroupName")) + " › " + Convert.ToString(GetField(hardPickerItem, "LeafName")), "빠른 명령 계층 경로");
			int weatherCount = 0;
			int difficultyCount = 0;
			foreach (object pickerItem in koreanPickerItems)
			{
				string group = Convert.ToString(GetField(pickerItem, "GroupKey"));
				if (group == "weather") weatherCount++;
				if (group == "difficulty") difficultyCount++;
			}
			Equal(3, weatherCount, "날씨 명령 그룹 개수");
			Equal(4, difficultyCount, "난이도 명령 그룹 개수");
			Equal(true, QuickCommandPickerContains(koreanPickerItems, "time query daytime"), "1.21.11 낮 시간 조회 표시");
			Equal(true, QuickCommandPickerContains(koreanPickerItems, "gamerule playersSleepingPercentage {percentage}"), "1.21.11 수면 비율 명령 표시");
			IEnumerable legacyPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { koreanBuiltIns, "paper", "1.12.2" });
			Equal(false, QuickCommandPickerContains(legacyPickerItems, "datapack list enabled"), "1.12 데이터팩 명령 숨김");
			IEnumerable sixteenPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { koreanBuiltIns, "paper", "1.16.5" });
			Equal(false, QuickCommandPickerContains(sixteenPickerItems, "gamerule playersSleepingPercentage {percentage}"), "1.16 수면 비율 명령 숨김");
			IEnumerable currentPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { koreanBuiltIns, "paper", "26.2" });
			Equal(false, QuickCommandPickerContains(currentPickerItems, "time query daytime"), "26.2 구형 낮 시간 구문 숨김");
			Equal(true, QuickCommandPickerContains(currentPickerItems, "time query gametime"), "26.2 호환 게임 시간 조회 표시");
			Dictionary<string, object> bridgeMessage = new Dictionary<string, object>();
			bridgeMessage["commands"] = new object[]
			{
				new Dictionary<string, object> { { "name", "home" }, { "usage", "/home" }, { "description", "Return home" }, { "plugin", "EssentialsX" } },
				new Dictionary<string, object> { { "name", "version" }, { "usage", "/version" }, { "description", "Server version" }, { "plugin", "" } }
			};
			IEnumerable parsedBridgeCommands = (IEnumerable)Invoke("ParseBridgeCommands", new object[] { bridgeMessage });
			object homeSuggestion = FindSuggestion(parsedBridgeCommands, "home");
			Equal("EssentialsX", Convert.ToString(GetField(homeSuggestion, "Plugin")), "브리지 플러그인 소유자 보존");
			IEnumerable bridgeDefinitions = (IEnumerable)Invoke("BuildBridgeQuickCommandDefinitions", new object[] { parsedBridgeCommands });
			int bridgeDefinitionCount = 0; foreach (object ignored in bridgeDefinitions) bridgeDefinitionCount++;
			Equal(1, bridgeDefinitionCount, "소유자 없는 서버 명령 플러그인 목록 중복 차단");
			IEnumerable pluginPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { bridgeDefinitions, "paper", "1.21.11" });
			object homePickerItem = FindQuickCommandPickerItem(pluginPickerItems, "home");
			Equal("plugin", Convert.ToString(GetField(homePickerItem, "CategoryKey")), "외부 명령 플러그인 카테고리");
			Equal("EssentialsX", Convert.ToString(GetField(homePickerItem, "GroupName")), "외부 명령 플러그인별 그룹화");
			Equal("플러그인 › EssentialsX › home", Convert.ToString(GetField(homePickerItem, "CategoryName")) + " › " + Convert.ToString(GetField(homePickerItem, "GroupName")) + " › " + Convert.ToString(GetField(homePickerItem, "LeafName")), "외부 명령 계층 경로");
			pickerLanguageField.SetValue(null, "en");
			IEnumerable englishBuiltIns = (IEnumerable)Invoke("GetBuiltInQuickCommands", new object[0]);
			IEnumerable englishPickerItems = (IEnumerable)Invoke("BuildQuickCommandPickerItems", new object[] { englishBuiltIns, "paper", "1.21.11" });
			object englishHardPickerItem = FindQuickCommandPickerItem(englishPickerItems, "difficulty hard");
			Equal("World › Difficulty › Hard", Convert.ToString(GetField(englishHardPickerItem, "CategoryName")) + " › " + Convert.ToString(GetField(englishHardPickerItem, "GroupName")) + " › " + Convert.ToString(GetField(englishHardPickerItem, "LeafName")), "영어 빠른 명령 계층 경로");
		}
		finally
		{
			pickerLanguageField.SetValue(null, originalPickerLanguage);
		}

		Type definitionType = launcher.GetNestedType("QuickCommandDefinition", BindingFlags.NonPublic);
		Type definitionListType = typeof(List<>).MakeGenericType(definitionType);
		IList userCommands = (IList)Activator.CreateInstance(definitionListType);
		object command = Activator.CreateInstance(definitionType, true);
		SetPublic(command, "Id", "test-command");
		SetPublic(command, "Name", "테스트 지급");
		SetPublic(command, "Description", "테스트 설명");
		SetPublic(command, "Category", "user");
		SetPublic(command, "Template", "give {online-player} {item} [count]");
		SetPublic(command, "Confirm", false);
		SetPublic(command, "ServerTypes", new string[] { "paper" });
		SetPublic(command, "MinimumMinecraftVersion", "1.13");
		SetPublic(command, "MaximumMinecraftVersion", "1.21.11");
		Type quickCommandEditorType = launcher.GetNestedType("QuickCommandEditorForm", BindingFlags.NonPublic);
		using (Form editor = (Form)Activator.CreateInstance(quickCommandEditorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { command }, null))
		{
			Equal(AutoScaleMode.Dpi, editor.AutoScaleMode, "사용자 명령 편집기 DPI 배율");
			ComboBox riskBox = (ComboBox)GetPrivateField(quickCommandEditorType, editor, "riskBox");
			Equal("ModernComboBox", riskBox.GetType().Name, "사용자 명령 현대형 위험도 선택");
			Equal(3, riskBox.Items.Count, "사용자 명령 3단계 위험도");
			TextBox minimumVersionBox = (TextBox)GetPrivateField(quickCommandEditorType, editor, "minimumVersionBox");
			TextBox maximumVersionBox = (TextBox)GetPrivateField(quickCommandEditorType, editor, "maximumVersionBox");
			if (string.IsNullOrWhiteSpace(minimumVersionBox.AccessibleName) || string.IsNullOrWhiteSpace(maximumVersionBox.AccessibleName)) throw new InvalidOperationException("Minecraft 버전 범위 입력의 접근성 이름이 없습니다.");
		}
		object[] valid = { command, null };
		Equal(true, Invoke("ValidateUserQuickCommand", valid), "사용자 명령 템플릿 검증");
		userCommands.Add(command);
		string dataRoot = Path.Combine(root, "quick-command-data");
		Invoke("SaveUserQuickCommands", new object[] { dataRoot, userCommands });
		IEnumerable loaded = (IEnumerable)Invoke("LoadUserQuickCommands", new object[] { dataRoot });
		int loadedCount = 0; object loadedCommand = null; foreach (object item in loaded) { loadedCount++; if (loadedCommand == null) loadedCommand = item; }
		Equal(1, loadedCount, "사용자 명령 저장과 로드");
		Equal("1.13", GetField(loadedCommand, "MinimumMinecraftVersion"), "사용자 명령 최소 버전 저장");
		Equal("1.21.11", GetField(loadedCommand, "MaximumMinecraftVersion"), "사용자 명령 최대 버전 저장");
		Equal(true, Invoke("TemplateMatchesCommand", new object[] { "give {player} {item} [count]", "give Alex minecraft:stone" }), "선택 인수 생략 허용");
		Equal(true, Invoke("TemplateMatchesCommand", new object[] { "ban {player} [reason]", "ban Alex repeated griefing" }), "선택 사유 여러 토큰 허용");
		Equal(false, Invoke("TemplateMatchesCommand", new object[] { "give {player} {item} [count]", "give Alex" }), "필수 인수 누락 차단");
		Equal("count", Invoke("GetTemplateArgumentName", new object[] { "[count=1]" }), "기본값 선택 인수 이름 분리");
		Equal("1", Invoke("GetTemplateArgumentDefault", new object[] { "[count=1]" }), "선택 인수 기본값 분리");
		Equal(true, EnumerableContains((IEnumerable)Invoke("GetArgumentCandidates", new object[] { "player", new string[] { "Alex" } }), "@a"), "플레이어 선택자 후보");
		Equal(true, EnumerableContains((IEnumerable)Invoke("GetArgumentCandidates", new object[] { "player", new string[] { "Alex" } }), "Alex"), "온라인 플레이어 후보");
		Equal(true, EnumerableContains((IEnumerable)Invoke("GetArgumentCandidates", new object[] { "difficulty", new string[0] }), "hard"), "난이도 후보");
		Equal(true, EnumerableContains((IEnumerable)Invoke("GetArgumentCandidates", new object[] { "boolean", new string[0] }), "false"), "불리언 후보");
		Equal(true, EnumerableContains((IEnumerable)Invoke("GetArgumentCandidates", new object[] { "x", new string[0] }), "~"), "좌표 추천값");

		object gamemodeDefinition = FindQuickCommandDefinition(builtIns, "gamemode {gamemode} {player}");
		object gamemodeBuilder = Invoke("CreateQuickCommandBuilderState", new object[] { gamemodeDefinition });
		IList gamemodeParts = (IList)GetField(gamemodeBuilder, "Parts");
		Equal("gamemode", GetField(gamemodeParts[1], "Name"), "첫 단계 게임 모드 인수");
		Equal(1, GetField(gamemodeBuilder, "ActivePartIndex"), "첫 필수 인수 자동 활성화");
		InvokeInstance(gamemodeBuilder, "SetValue", new object[] { 1, "creative" });
		Equal(true, InvokeInstance(gamemodeBuilder, "MoveNext", new object[] { true }), "값 확정 후 다음 인수 이동");
		Equal("player", GetField(gamemodeParts[2], "Name"), "다음 플레이어 인수");
		Equal(2, GetField(gamemodeBuilder, "ActivePartIndex"), "플레이어 단계 자동 활성화");
		InvokeInstance(gamemodeBuilder, "SetValue", new object[] { 2, "PlayerName" });
		Equal(false, InvokeInstance(gamemodeBuilder, "MoveNext", new object[] { true }), "마지막 인수 뒤 작성 완료");
		Equal(true, GetProperty(gamemodeBuilder, "IsComplete"), "필수 인수 완료 판정");
		Equal("gamemode creative PlayerName", InvokeInstance(gamemodeBuilder, "BuildCommand", new object[0]), "완성 명령 생성");
		Equal(true, InvokeInstance(gamemodeBuilder, "MovePrevious", new object[0]), "Shift+Tab 이전 인수 이동");
		Equal("creative", GetField(gamemodeParts[1], "Value"), "이전 단계 이동 후 기존 값 유지");
		Equal("PlayerName", GetField(gamemodeParts[2], "Value"), "다른 인수 값 유지");

		object giveDefinition = FindQuickCommandDefinition(builtIns, "give {player} {item} [count=1]");
		object giveBuilder = Invoke("CreateQuickCommandBuilderState", new object[] { giveDefinition });
		IList giveParts = (IList)GetField(giveBuilder, "Parts");
		InvokeInstance(giveBuilder, "SetValue", new object[] { 1, "Alex" });
		InvokeInstance(giveBuilder, "MoveNext", new object[] { true });
		InvokeInstance(giveBuilder, "SetValue", new object[] { 2, "diam" });
		IEnumerable itemSuggestions = (IEnumerable)Invoke("GetQuickCommandBuilderSuggestions", new object[] { giveBuilder, new string[] { "Alex" } });
		Equal(true, SuggestionContains(itemSuggestions, "minecraft:diamond"), "아이템 부분 검색 자동완성");
		InvokeInstance(giveBuilder, "SetValue", new object[] { 2, "minecraft:diamond" });
		InvokeInstance(giveBuilder, "MoveNext", new object[] { true });
		Equal(false, GetField(giveParts[3], "Required"), "수량 선택 인수 구분");
		Equal("1", GetField(giveParts[3], "DefaultValue"), "수량 기본값 유지");
		Equal(true, GetProperty(giveBuilder, "IsComplete"), "선택 인수 미입력 전송 허용");
		Equal("give Alex minecraft:diamond", InvokeInstance(giveBuilder, "BuildCommand", new object[0]), "빈 선택 인수 공백 없이 생략");

		object kickDefinition = FindQuickCommandDefinition(builtIns, "kick {player} [reason]");
		object kickBuilder = Invoke("CreateQuickCommandBuilderState", new object[] { kickDefinition });
		InvokeInstance(kickBuilder, "SetValue", new object[] { 1, "Alex" });
		InvokeInstance(kickBuilder, "MoveNext", new object[] { true });
		Equal(true, GetProperty(kickBuilder, "IsComplete"), "선택 사유 없이 명령 완료");
		Equal("kick Alex", InvokeInstance(kickBuilder, "BuildCommand", new object[0]), "선택 사유 생략 명령");
		InvokeInstance(kickBuilder, "SetValue", new object[] { 2, "repeated griefing" });
		Equal("kick Alex repeated griefing", InvokeInstance(kickBuilder, "BuildCommand", new object[0]), "여러 단어 선택 사유 유지");

		object invalidBuilder = Invoke("CreateQuickCommandBuilderState", new object[] { gamemodeDefinition });
		InvokeInstance(invalidBuilder, "SetValue", new object[] { 1, "invalid-mode" });
		Equal(false, GetProperty(invalidBuilder, "IsComplete"), "잘못된 열거형 인수 차단");
		Equal(true, InvokeInstance(invalidBuilder, "MoveNext", new object[] { true }), "잘못된 인수에서 작성 단계 유지");
		Equal(1, GetField(invalidBuilder, "ActivePartIndex"), "잘못된 인수 자동 건너뛰기 차단");
		InvokeInstance(invalidBuilder, "SetValue", new object[] { 1, "survival" });
		Equal("survival", GetField(((IList)GetField(invalidBuilder, "Parts"))[1], "Value"), "잘못된 인수만 다시 설정");
		SetPublic(command, "Description", "수정된 설명");
		Invoke("SaveUserQuickCommands", new object[] { dataRoot, userCommands });
		userCommands.Clear();
		Invoke("SaveUserQuickCommands", new object[] { dataRoot, userCommands });
		loaded = (IEnumerable)Invoke("LoadUserQuickCommands", new object[] { dataRoot });
		loadedCount = 0; foreach (object ignored in loaded) loadedCount++;
		Equal(0, loadedCount, "사용자 명령 삭제");
		SetPublic(command, "Template", "say {unsupported}");
		object[] invalid = { command, null };
		Equal(false, Invoke("ValidateUserQuickCommand", invalid), "지원하지 않는 템플릿 매개변수 차단");
		SetPublic(command, "Template", "test [reason] fixed");
		invalid = new object[] { command, null };
		Equal(false, Invoke("ValidateUserQuickCommand", invalid), "선택 인수 뒤 고정 문구 차단");

		object parse = Invoke("ParseCommandInput", new object[] { "say \"hello world\"", 10 });
		IList tokens = (IList)GetField(parse, "Tokens");
		Equal(2, tokens.Count, "따옴표 명령 토큰 처리");
		IEnumerable suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "gam", 3, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[] { "Alex" }, new string[0] });
		Equal(true, SuggestionContains(suggestions, "gamemode"), "루트 명령 자동완성");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "gamemode c", 10, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[] { "Alex" }, new string[0] });
		Equal(true, SuggestionContains(suggestions, "creative"), "하위 명령 자동완성");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "op A", 4, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[] { "Alex" }, new string[0] });
		Equal(true, SuggestionContains(suggestions, "Alex"), "온라인 플레이어 후보");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "ban-ip A", 8, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[] { "Alex" }, new string[0] });
		Equal(true, SuggestionContains(suggestions, "Alex"), "IP 차단 플레이어 후보");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "gamerule playersSleepingPercentage ", 39, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[0], new string[0] });
		Equal(true, SuggestionContains(suggestions, "50"), "수면 비율 자동완성");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "time add ", 9, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[0], new string[0] });
		Equal(true, SuggestionContains(suggestions, "10s"), "시간 단위 자동완성");
		suggestions = (IEnumerable)Invoke("GetLocalQuickCommandSuggestions", new object[] { "sa", 2, "paper", "1.21.11", Activator.CreateInstance(definitionListType), new string[0], new string[] { "say custom recent" } });
		object historySuggestion = FindSuggestion(suggestions, "say custom recent");
		Equal(2, Convert.ToInt32(GetField(historySuggestion, "ReplaceLength")), "명령 기록 전체 입력 교체");
		Equal("say hello", Convert.ToString(Invoke("NormalizeCommandForSend", new object[] { "/say hello\r\n" })), "전송 전 슬래시 제거");
		Equal(true, Invoke("RequiresQuickCommandConfirmation", new object[] { "whitelist off", Activator.CreateInstance(definitionListType) }), "위험 명령 확인");
		Equal("Normal", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "banlist players", builtIns })), "조회 명령 일반 위험도");
		Equal("Confirm", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "datapack enable vanilla", builtIns })), "데이터팩 변경 확인 위험도");
		Equal("Dangerous", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "reload", builtIns })), "재시작 없는 다시 불러오기 위험 경고");
		Equal("Dangerous", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "gamerule keepInventory false", builtIns })), "아이템 보존 해제 조건부 위험도");
		Equal("Confirm", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "gamerule keepInventory true", builtIns })), "아이템 보존 활성화 확인 위험도");
		Equal("Normal", Convert.ToString(Invoke("GetQuickCommandRisk", new object[] { "worldborder get", builtIns })), "월드 경계 조회 일반 위험도");
		Equal(false, Invoke("CanSendQuickCommand", new object[] { false, "list" }), "서버 미실행 명령 차단");
		Equal(true, Invoke("IsSuggestionGenerationCurrent", new object[] { 4, 4 }), "최신 자동완성 응답 허용");
		Equal(false, Invoke("IsSuggestionGenerationCurrent", new object[] { 3, 4 }), "오래된 자동완성 응답 무시");
		Equal(1, Invoke("GetNextQuickCommandSuggestionIndex", new object[] { 0, 3, false }), "탭으로 다음 자동완성 후보 이동");
		Equal(0, Invoke("GetNextQuickCommandSuggestionIndex", new object[] { 2, 3, false }), "마지막 후보에서 첫 후보로 순환");
		Equal(2, Invoke("GetNextQuickCommandSuggestionIndex", new object[] { 0, 3, true }), "Shift+Tab으로 이전 후보 이동");
		Equal(-1, Invoke("GetNextQuickCommandSuggestionIndex", new object[] { -1, 0, false }), "빈 자동완성 목록 처리");
		Type suggestionType = launcher.GetNestedType("QuickCommandSuggestion", BindingFlags.NonPublic);
		Type suggestionListType = typeof(List<>).MakeGenericType(suggestionType);
		IList bridgeSuggestions = (IList)Activator.CreateInstance(suggestionListType);
		IList localSuggestions = (IList)Activator.CreateInstance(suggestionListType);
		bridgeSuggestions.Add(Invoke("NewSuggestion", new object[] { "live", "live", "", "bridge", false }));
		localSuggestions.Add(Invoke("NewSuggestion", new object[] { "user", "user", "", "user", false }));
		localSuggestions.Add(Invoke("NewSuggestion", new object[] { "builtin", "builtin", "", "builtin", false }));
		IList mergedSuggestions = (IList)Invoke("MergeQuickCommandSuggestions", new object[] { bridgeSuggestions, localSuggestions, 10 });
		Equal("bridge", Convert.ToString(GetField(mergedSuggestions[0], "Source")), "실시간 후보 우선 정렬");
		Equal("user", Convert.ToString(GetField(mergedSuggestions[1], "Source")), "사용자 후보 다음 정렬");
		object[] historyArguments = { new List<string>(new string[] { "list", "save-all" }), -1, -1, null };
		Equal("save-all", Convert.ToString(Invoke("GetQuickCommandHistoryValue", historyArguments)), "명령 기록 탐색");

		Equal(true, Invoke("IsCommandBridgeSupported", new object[] { "paper", "1.13" }), "Paper 브리지 최소 버전");
		Equal(true, Invoke("IsCommandBridgeSupported", new object[] { "purpur", "26.2" }), "Purpur 브리지 지원");
		Equal(false, Invoke("IsCommandBridgeSupported", new object[] { "vanilla", "1.21.11" }), "지원하지 않는 서버 브리지 제안 차단");
		Equal(true, Invoke("IsMinecraftVersionInBridgeRange", new object[] { "1.21.11", "1.13", "26.2" }), "브리지 Minecraft 호환 범위");
		Equal(false, Invoke("IsMinecraftVersionInBridgeRange", new object[] { "1.12.2", "1.13", "26.2" }), "브리지 최소 버전 차단");
		string profileA = Path.Combine(root, "bridge-a");
		string profileB = Path.Combine(root, "bridge-b");
		Directory.CreateDirectory(profileA); Directory.CreateDirectory(profileB);
		Invoke("WriteBridgeChoice", new object[] { profileA, "install" });
		Invoke("WriteBridgeChoice", new object[] { profileB, "skip" });
		Equal("install", Convert.ToString(GetField(Invoke("ReadBridgeChoice", new object[] { profileA }), "Choice")), "프로필별 브리지 설치 동의");
		Equal("skip", Convert.ToString(GetField(Invoke("ReadBridgeChoice", new object[] { profileB }), "Choice")), "프로필별 브리지 거절 분리");
		Invoke("WriteBridgeDefaultPreference", new object[] { dataRoot, "install" });
		object defaultPreference = Invoke("ReadBridgeDefaultPreference", new object[] { dataRoot });
		Equal(true, GetField(defaultPreference, "HasDefault"), "새 프로필 브리지 기본 선택 저장");
		Equal("install", Convert.ToString(GetField(defaultPreference, "Choice")), "브리지 기본 동의 값");

		string firstJar = Path.Combine(root, "bridge-first.jar");
		string secondJar = Path.Combine(root, "bridge-second.jar");
		CreateFakeBridgeJar(firstJar, "first");
		CreateFakeBridgeJar(secondJar, "second");
		string firstHash = FileSha256(firstJar);
		Equal(false, Invoke("ValidateCommandBridgeArtifact", new object[] { firstJar, new FileInfo(firstJar).Length, new string('0', 64) }), "브리지 JAR 해시 불일치 차단");
		SetStaticField("BridgeArtifactOverridePath", firstJar);
		Invoke("InstallOrUpdateCommandBridge", new object[] { profileA, "paper", "1.21.11" });
		string installedJar = Convert.ToString(Invoke("GetBridgeJarPath", new object[] { profileA }));
		Equal(firstHash, FileSha256(installedJar), "검증된 브리지 설치");
		SetStaticField("BridgeArtifactOverridePath", secondJar);
		File.AppendAllText(installedJar, "changed");
		ExpectFailure(delegate { Invoke("InstallOrUpdateCommandBridge", new object[] { profileA, "paper", "1.21.11" }); }, "사용자가 변경한 관리 JAR 덮어쓰기 차단");
		File.Copy(firstJar, installedJar, true);
		SetStaticField("BridgeInstallFailureAfterBackup", true);
		ExpectFailure(delegate { Invoke("InstallOrUpdateCommandBridge", new object[] { profileA, "paper", "1.21.11" }); }, "브리지 업데이트 실패 주입");
		SetStaticField("BridgeInstallFailureAfterBackup", false);
		Equal(firstHash, FileSha256(installedJar), "업데이트 실패 시 기존 브리지 복구");
		string bridgeData = Path.Combine(profileA, "plugins", "MinecraftServerLauncherCommandBridge");
		Directory.CreateDirectory(bridgeData); File.WriteAllText(Path.Combine(bridgeData, "keep.txt"), "keep");
		Invoke("RemoveManagedCommandBridge", new object[] { profileA, false });
		Equal(true, File.Exists(Path.Combine(bridgeData, "keep.txt")), "브리지 제거 시 사용자 데이터 보존");

		string sessionDirectory = Path.Combine(root, "bridge-session");
		Directory.CreateDirectory(sessionDirectory);
		Type sessionType = launcher.GetNestedType("CommandBridgeSession", BindingFlags.NonPublic);
		object session = Activator.CreateInstance(sessionType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { sessionDirectory, "profile-test" }, null);
		try
		{
			Equal(false, GetMember(session, "MetricsAvailable"), "연결 전 브리지 지표 미지원 상태");
			Dictionary<string, object> metricsMessage = new Dictionary<string, object> { { "type", "metrics-update" }, { "id", "m1" }, { "supported", true }, { "tps1", 20.0 }, { "tps5", 19.9 }, { "tps15", 19.8 }, { "mspt", 12.5 } };
			sessionType.GetMethod("HandleBridgeMessage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, new object[] { metricsMessage });
			Equal(true, GetMember(session, "MetricsAvailable"), "브리지 지표 메시지 처리");
			Equal(12.5, GetMember(session, "Mspt"), "브리지 MSPT 메시지 처리");
			Dictionary<string, object> invalidMetricsMessage = new Dictionary<string, object> { { "type", "metrics-update" }, { "id", "m2" }, { "supported", true }, { "tps1", "NaN" }, { "tps5", 19.9 }, { "tps15", 19.8 }, { "mspt", 12.5 } };
			sessionType.GetMethod("HandleBridgeMessage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, new object[] { invalidMetricsMessage });
			Equal(false, GetMember(session, "MetricsAvailable"), "잘못된 브리지 지표를 추정값으로 표시하지 않음");
			TcpListener listener = (TcpListener)sessionType.GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
			IPEndPoint endpoint = (IPEndPoint)listener.LocalEndpoint;
			Equal(true, IPAddress.IsLoopback(endpoint.Address), "브리지 리스너 루프백 바인딩");
			using (TcpClient client = new TcpClient())
			{
				client.Connect(IPAddress.Loopback, endpoint.Port);
				using (StreamWriter writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false), 1024, true))
				using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 1024, true))
				{
					writer.AutoFlush = true;
					writer.WriteLine("{\"type\":\"hello\",\"id\":\"bad\",\"token\":\"wrong-token\",\"profile\":\"profile-test\",\"protocol\":1}");
					string response = reader.ReadLine();
					if (response == null || response.IndexOf("handshake-rejected", StringComparison.Ordinal) < 0) throw new InvalidOperationException("브리지 토큰 불일치가 거부되지 않았습니다.");
				}
			}
		}
		finally { ((IDisposable)session).Dispose(); }
		ExpectFailure(delegate { Invoke("DeserializeBridgeObject", new object[] { new string('x', 65537) }); }, "과도한 브리지 요청 차단");
		Equal(true, Invoke("BridgeTokenEquals", new object[] { "same-token", "same-token" }), "고정 시간 토큰 비교");
		Equal(false, Invoke("BridgeTokenEquals", new object[] { "same-token", "other-token" }), "토큰 불일치 거부");
		Pass();
	}

	private static bool SuggestionContains(IEnumerable suggestions, string value)
	{
		foreach (object item in suggestions) if (string.Equals(Convert.ToString(GetField(item, "Value")), value, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static bool EnumerableContains(IEnumerable values, string expected)
	{
		foreach (object value in values) if (string.Equals(Convert.ToString(value), expected, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static object FindQuickCommandPickerItem(IEnumerable items, string template)
	{
		foreach (object item in items)
		{
			object definition = GetField(item, "Definition");
			if (string.Equals(Convert.ToString(GetField(definition, "Template")), template, StringComparison.OrdinalIgnoreCase)) return item;
		}
		throw new InvalidOperationException("빠른 명령 선택 항목을 찾지 못했습니다: " + template);
	}

	private static object FindQuickCommandDefinition(IEnumerable definitions, string template)
	{
		foreach (object definition in definitions)
		{
			if (string.Equals(Convert.ToString(GetField(definition, "Template")), template, StringComparison.OrdinalIgnoreCase)) return definition;
		}
		throw new InvalidOperationException("빠른 명령 정의를 찾지 못했습니다: " + template);
	}

	private static bool QuickCommandPickerContains(IEnumerable items, string template)
	{
		foreach (object item in items)
		{
			object definition = GetField(item, "Definition");
			if (string.Equals(Convert.ToString(GetField(definition, "Template")), template, StringComparison.OrdinalIgnoreCase)) return true;
		}
		return false;
	}

	private static object FindSuggestion(IEnumerable suggestions, string value)
	{
		foreach (object item in suggestions) if (string.Equals(Convert.ToString(GetField(item, "Value")), value, StringComparison.OrdinalIgnoreCase)) return item;
		throw new InvalidOperationException("자동완성 후보를 찾지 못했습니다: " + value);
	}

	private static void CollectButtons(Control parent, List<Button> buttons)
	{
		foreach (Control control in parent.Controls)
		{
			Button button = control as Button;
			if (button != null) buttons.Add(button);
			if (control.HasChildren) CollectButtons(control, buttons);
		}
	}

	private static void CreateFakeBridgeJar(string path, string marker)
	{
		if (File.Exists(path)) File.Delete(path);
		using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
		{
			using (StreamWriter writer = new StreamWriter(archive.CreateEntry("plugin.yml").Open())) writer.Write("name: TestBridge\nmain: test.CommandBridgePlugin\n" + marker);
			using (Stream stream = archive.CreateEntry("test/CommandBridgePlugin.class").Open()) { byte[] bytes = Encoding.UTF8.GetBytes(marker); stream.Write(bytes, 0, bytes.Length); }
		}
	}

	private static string FileSha256(string path)
	{
		using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant();
	}

	private static object Invoke(string name, object[] arguments)
	{
		MethodInfo method = launcher.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null) throw new MissingMethodException(name);
		return method.Invoke(null, arguments);
	}

	private static object InvokeInstance(object instance, string name, object[] arguments)
	{
		MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (method == null) throw new MissingMethodException(instance.GetType().FullName, name);
		return method.Invoke(instance, arguments);
	}

	private static object GetProperty(object instance, string name)
	{
		PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property == null) throw new MissingMemberException(instance.GetType().FullName, name);
		return property.GetValue(instance, null);
	}

	private static object GetField(object instance, string name) { return instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance); }
	private static object GetMember(object instance, string name) { PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); return property != null ? property.GetValue(instance, null) : GetField(instance, name); }
	private static void SetMember(object instance, string name, object value) { PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (property != null) property.SetValue(instance, value, null); else instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value); }
	private static object GetPrivateField(Type type, object instance, string name) { return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance); }
	private static object GetStaticField(string name) { return launcher.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); }
	private static void SetStaticField(string name, object value) { launcher.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value); }
	private static void SetPublic(object instance, string name, object value) { instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public).SetValue(instance, value); }
	private static object CreateContentManifestEntry(string id, string projectId, string fileName, string[] dependencies)
	{
		Type type = launcher.GetNestedType("ContentManifestEntry", BindingFlags.NonPublic);
		object entry = Activator.CreateInstance(type, true);
		SetPublic(entry, "Id", id);
		SetPublic(entry, "Kind", "plugin");
		SetPublic(entry, "Source", "modrinth");
		SetPublic(entry, "ProjectId", projectId);
		SetPublic(entry, "VersionId", "test-version");
		SetPublic(entry, "VersionName", "1.0.0");
		SetPublic(entry, "FileName", fileName);
		SetPublic(entry, "RelativePath", "plugins/" + fileName);
		SetPublic(entry, "DisabledRelativePath", ".mineharbor/disabled/plugin/" + id + "-" + fileName);
		SetPublic(entry, "WorldName", string.Empty);
		SetPublic(entry, "Managed", true);
		SetPublic(entry, "Active", true);
		SetPublic(entry, "InstalledUtc", DateTime.UtcNow.ToString("o"));
		SetPublic(entry, "Dependencies", dependencies);
		return entry;
	}
	private static void Pass() { passed++; }
	private static void Equal(object expected, object actual, string name) { if (!object.Equals(expected, actual)) throw new InvalidOperationException(name + ": expected=" + expected + ", actual=" + actual); }
	private static void ExpectFailure(Action action, string name)
	{
		try { action(); }
		catch { return; }
		throw new InvalidOperationException(name + " 검사가 실패를 차단하지 못했습니다.");
	}

	public sealed class FakeMapping
	{
		public int ExternalPort { get; private set; }
		public string Protocol { get; private set; }
		public int InternalPort { get; private set; }
		public string InternalClient { get; private set; }
		public string Description { get; private set; }
		public string ExternalIPAddress { get { return "203.0.113.10"; } }

		public FakeMapping(int externalPort, string protocol, int internalPort, string internalClient, string description)
		{
			ExternalPort = externalPort;
			Protocol = protocol;
			InternalPort = internalPort;
			InternalClient = internalClient;
			Description = description;
		}
	}

	public sealed class FakeMappingCollection : IEnumerable
	{
		private readonly Dictionary<string, FakeMapping> mappings = new Dictionary<string, FakeMapping>(StringComparer.OrdinalIgnoreCase);
		private bool throwAfterAddOnce;
		public int RemoveCount { get; private set; }
		public int AddCount { get; private set; }
		public int Count { get { return mappings.Count; } }
		public FakeMappingCollection(FakeMapping value) : this(value, false) { }
		public FakeMappingCollection(FakeMapping value, bool throwAfterAdd)
		{
			if (value != null) mappings[GetKey(value.ExternalPort, value.Protocol)] = value;
			throwAfterAddOnce = throwAfterAdd;
		}
		public FakeMapping this[int port, string protocol]
		{
			get { FakeMapping mapping; return mappings.TryGetValue(GetKey(port, protocol), out mapping) ? mapping : null; }
		}
		public object Add(int externalPort, string protocol, int internalPort, string internalClient, bool enabled, string description)
		{
			FakeMapping mapping = new FakeMapping(externalPort, protocol, internalPort, internalClient, description);
			mappings[GetKey(externalPort, protocol)] = mapping;
			AddCount++;
			if (throwAfterAddOnce)
			{
				throwAfterAddOnce = false;
				throw new InvalidOperationException("공유기가 매핑 생성 후 응답을 잃었습니다.");
			}
			return mapping;
		}
		public void Remove(int externalPort, string protocol) { if (mappings.Remove(GetKey(externalPort, protocol))) RemoveCount++; }
		public IEnumerator GetEnumerator() { return mappings.Values.GetEnumerator(); }
		private static string GetKey(int port, string protocol) { return port.ToString() + "/" + protocol; }
	}

	private sealed class FakeSoapMapping
	{
		public int InternalPort;
		public string InternalClient;
		public string Description;
	}

	private sealed class FakeUpnpSoapServer : IDisposable
	{
		private readonly TcpListener listener;
		private readonly Thread worker;
		private readonly object sync = new object();
		private readonly Dictionary<string, FakeSoapMapping> mappings = new Dictionary<string, FakeSoapMapping>(StringComparer.OrdinalIgnoreCase);
		private volatile bool stopping;
		public bool DropNextAddResponse;
		public int DelayNextResponseMilliseconds;

		public FakeUpnpSoapServer()
		{
			listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			worker = new Thread(Run);
			worker.IsBackground = true;
			worker.Start();
		}

		public string ControlUrl { get { return "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/control"; } }
		public int Count { get { lock (sync) return mappings.Count; } }

		public void Preload(int externalPort, string protocol, int internalPort, string internalClient, string description)
		{
			lock (sync) mappings[GetKey(externalPort, protocol)] = new FakeSoapMapping { InternalPort = internalPort, InternalClient = internalClient, Description = description };
		}

		private void Run()
		{
			while (!stopping)
			{
				try
				{
					using (TcpClient client = listener.AcceptTcpClient()) Handle(client);
				}
				catch (SocketException) { if (!stopping) throw; }
				catch (ObjectDisposedException) { if (!stopping) throw; }
				catch (Exception ex) { if (!stopping) Console.WriteLine("가짜 UPnP 서버 오류: " + ex.Message); }
			}
		}

		private void Handle(TcpClient client)
		{
			client.ReceiveTimeout = 5000;
			client.SendTimeout = 5000;
			NetworkStream stream = client.GetStream();
			List<byte> headerBytes = new List<byte>();
			int matched = 0;
			while (headerBytes.Count < 65536)
			{
				int value = stream.ReadByte();
				if (value < 0) return;
				headerBytes.Add((byte)value);
				byte expected = new byte[] { 13, 10, 13, 10 }[matched];
				matched = value == expected ? matched + 1 : value == 13 ? 1 : 0;
				if (matched == 4) break;
			}
			string headers = Encoding.ASCII.GetString(headerBytes.ToArray());
			Match lengthMatch = Regex.Match(headers, @"(?im)^Content-Length:\s*(\d+)\s*$");
			int contentLength = lengthMatch.Success ? int.Parse(lengthMatch.Groups[1].Value) : 0;
			if (Regex.IsMatch(headers, @"(?im)^Expect:\s*100-continue\s*$"))
			{
				byte[] continueResponse = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
				stream.Write(continueResponse, 0, continueResponse.Length);
			}
			byte[] bodyBytes = new byte[contentLength];
			int offset = 0;
			while (offset < bodyBytes.Length)
			{
				int read = stream.Read(bodyBytes, offset, bodyBytes.Length - offset);
				if (read <= 0) break;
				offset += read;
			}
			string body = Encoding.UTF8.GetString(bodyBytes, 0, offset);
			int responseDelay = Interlocked.Exchange(ref DelayNextResponseMilliseconds, 0);
			if (responseDelay > 0) Thread.Sleep(responseDelay);

			if (body.IndexOf("<u:GetSpecificPortMappingEntry", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int externalPort = int.Parse(ReadTag(body, "NewExternalPort"));
				string protocol = ReadTag(body, "NewProtocol");
				FakeSoapMapping mapping;
				lock (sync) mappings.TryGetValue(GetKey(externalPort, protocol), out mapping);
				if (mapping == null) { WriteFault(stream, 714); return; }
				string responseBody = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><u:GetSpecificPortMappingEntryResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"><NewInternalPort>" + mapping.InternalPort + "</NewInternalPort><NewInternalClient>" + Escape(mapping.InternalClient) + "</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>" + Escape(mapping.Description) + "</NewPortMappingDescription><NewLeaseDuration>0</NewLeaseDuration></u:GetSpecificPortMappingEntryResponse></s:Body></s:Envelope>";
				WriteResponse(stream, 200, "OK", responseBody);
				return;
			}
			if (body.IndexOf("<u:AddPortMapping", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int externalPort = int.Parse(ReadTag(body, "NewExternalPort"));
				string protocol = ReadTag(body, "NewProtocol");
				int internalPort = int.Parse(ReadTag(body, "NewInternalPort"));
				string internalClient = ReadTag(body, "NewInternalClient");
				string description = ReadTag(body, "NewPortMappingDescription");
				lock (sync)
				{
					FakeSoapMapping existing;
					if (mappings.TryGetValue(GetKey(externalPort, protocol), out existing) && !string.Equals(existing.InternalClient, internalClient, StringComparison.OrdinalIgnoreCase))
					{
						WriteFault(stream, 718);
						return;
					}
					mappings[GetKey(externalPort, protocol)] = new FakeSoapMapping { InternalPort = internalPort, InternalClient = internalClient, Description = description };
				}
				if (DropNextAddResponse) { DropNextAddResponse = false; return; }
				WriteResponse(stream, 200, "OK", "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><u:AddPortMappingResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"/></s:Body></s:Envelope>");
				return;
			}
			if (body.IndexOf("<u:DeletePortMapping", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int externalPort = int.Parse(ReadTag(body, "NewExternalPort"));
				string protocol = ReadTag(body, "NewProtocol");
				lock (sync) mappings.Remove(GetKey(externalPort, protocol));
				WriteResponse(stream, 200, "OK", "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><u:DeletePortMappingResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"/></s:Body></s:Envelope>");
				return;
			}
			WriteResponse(stream, 404, "Not Found", string.Empty);
		}

		private static string ReadTag(string xml, string name)
		{
			Match match = Regex.Match(xml ?? string.Empty, "<" + name + ">(.*?)</" + name + ">", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : string.Empty;
		}

		private static string Escape(string value) { return System.Security.SecurityElement.Escape(value ?? string.Empty); }
		private static string GetKey(int port, string protocol) { return port.ToString() + "/" + protocol; }

		private static void WriteFault(NetworkStream stream, int errorCode)
		{
			string body = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><s:Fault><detail><UPnPError><errorCode>" + errorCode + "</errorCode><errorDescription>Test fault</errorDescription></UPnPError></detail></s:Fault></s:Body></s:Envelope>";
			WriteResponse(stream, 500, "Internal Server Error", body);
		}

		private static void WriteResponse(NetworkStream stream, int statusCode, string reason, string body)
		{
			byte[] payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
			byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 " + statusCode + " " + reason + "\r\nContent-Type: text/xml; charset=utf-8\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
			stream.Write(header, 0, header.Length);
			if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
			stream.Flush();
		}

		public void Dispose()
		{
			stopping = true;
			listener.Stop();
			worker.Join(3000);
		}
	}

	private static object AwaitTaskResult(object taskObject)
	{
		Task task = (Task)taskObject;
		task.GetAwaiter().GetResult();
		PropertyInfo resultProperty = taskObject.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
		return resultProperty == null ? null : resultProperty.GetValue(taskObject, null);
	}

    private static void TestSocketUpnpLocalServer()
    {
        Console.Write("TestSocketUpnpLocalServer: ");
        Type serviceType = launcher.GetNestedType("SocketUpnpPortMappingService", BindingFlags.NonPublic);
        if (serviceType == null) throw new Exception("SocketUpnpPortMappingService not found");
        PropertyInfo implemented = serviceType.GetProperty("MappingImplemented", BindingFlags.NonPublic | BindingFlags.Static);
		Equal(true, implemented.GetValue(null, null), "SSDP/SOAP UPnP 백업 방식 활성화");

		Type serviceInfoType = serviceType.GetNestedType("UpnpServiceInfo", BindingFlags.NonPublic | BindingFlags.Public);
		MethodInfo parseServices = serviceType.GetMethod("ParseUpnpServices", BindingFlags.NonPublic | BindingFlags.Static);
		string descriptionXml = "<root><device><serviceList><service><serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType><controlURL>/control</controlURL></service><service><serviceType>urn:schemas-upnp-org:service:WANPPPConnection:1</serviceType><controlURL>/ppp</controlURL></service></serviceList></device></root>";
		IList parsedServices = (IList)parseServices.Invoke(null, new object[] { descriptionXml, new Uri("http://127.0.0.1:54321/root.xml") });
		Equal(2, parsedServices.Count, "장치 설명에서 WAN 서비스별 제어 URL 연결");

		Type trackerType = launcher.GetNestedType("UpnpMappingOwnershipTracker", BindingFlags.NonPublic | BindingFlags.Static);
		PropertyInfo pathProperty = trackerType.GetProperty("TrackerFilePathOverride", BindingFlags.NonPublic | BindingFlags.Static);
		string oldPath = (string)pathProperty.GetValue(null, null);
		string dummyFile = Path.Combine(Path.GetTempPath(), "test-socket-upnp-mappings-" + Guid.NewGuid().ToString("N") + ".tsv");
		pathProperty.SetValue(null, dummyFile, null);
		object service = null;
		try
		{
			using (FakeUpnpSoapServer server = new FakeUpnpSoapServer())
			{
				service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { null, null, null }, null);
				object serviceInfo = Activator.CreateInstance(serviceInfoType, true);
				SetMember(serviceInfo, "ServiceType", "urn:schemas-upnp-org:service:WANIPConnection:1");
				SetMember(serviceInfo, "ControlUrl", server.ControlUrl);
				SetMember(serviceInfo, "RouterId", "127.0.0.1");
				MethodInfo mapOnService = serviceType.GetMethod("MapPortsOnServiceAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo deleteMappings = serviceType.GetMethod("DeleteMappingsAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				for (int cycle = 0; cycle < 8; cycle++)
				{
					object mappingResult = AwaitTaskResult(mapOnService.Invoke(service, new object[] { serviceInfo, 25600, 25565, "127.0.0.1", true, "MH-" + cycle.ToString("D12"), CancellationToken.None }));
					Equal(true, GetMember(mappingResult, "Success"), "SSDP/SOAP 반복 매핑 생성 " + cycle);
					IList created = (IList)GetMember(mappingResult, "CreatedMappings");
					Equal(2, created.Count, "TCP/UDP 소유 매핑 기록 " + cycle);
					Equal(2, AwaitTaskResult(deleteMappings.Invoke(service, new object[] { created, CancellationToken.None })), "SSDP/SOAP 반복 종료 정리 " + cycle);
					Equal(0, server.Count, "반복 종료 후 공유기 매핑 없음 " + cycle);
				}

				server.DropNextAddResponse = true;
				object recoveredResult = AwaitTaskResult(mapOnService.Invoke(service, new object[] { serviceInfo, 25601, 25565, "127.0.0.1", false, "MH-555555555555", CancellationToken.None }));
				Equal(true, GetMember(recoveredResult, "Success"), "SOAP Add 응답 유실 후 조회로 복구");
				IList recoveredCreated = (IList)GetMember(recoveredResult, "CreatedMappings");
				Equal(1, recoveredCreated.Count, "응답 유실 매핑 소유권 유지");
				AwaitTaskResult(deleteMappings.Invoke(service, new object[] { recoveredCreated, CancellationToken.None }));

				server.Preload(25602, "TCP", 25565, "127.0.0.2", "사용자 매핑");
				MethodInfo mapWithFallback = serviceType.GetMethod("MapPortsOnServiceWithFallbackAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				object fallbackResult = AwaitTaskResult(mapWithFallback.Invoke(service, new object[] { serviceInfo, new int[] { 25602, 25603 }, 25565, "127.0.0.1", false, "MH-444444444444", CancellationToken.None }));
				Equal(true, GetMember(fallbackResult, "Success"), "SOAP 충돌 후 대체 외부 포트 생성");
				Equal(25603, GetMember(fallbackResult, "ExternalPort"), "SOAP 대체 외부 포트 선택");
				IList fallbackCreated = (IList)GetMember(fallbackResult, "CreatedMappings");
				AwaitTaskResult(deleteMappings.Invoke(service, new object[] { fallbackCreated, CancellationToken.None }));
				Equal(1, server.Count, "SOAP 대체 포트 정리 후 사용자 매핑 보존");

				server.DelayNextResponseMilliseconds = 1200;
				bool canceled = false;
				using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
				{
					try { AwaitTaskResult(mapOnService.Invoke(service, new object[] { serviceInfo, 25604, 25565, "127.0.0.1", false, "MH-333333333333", cancellation.Token })); }
					catch (OperationCanceledException) { canceled = true; }
				}
				Equal(true, canceled, "서버 종료 신호로 진행 중인 SOAP 요청 취소");
				Equal(1, server.Count, "취소 후 사용자 매핑 외 추가 매핑 없음");
			}
		}
		finally
		{
			if (service != null) serviceType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public).Invoke(service, null);
			pathProperty.SetValue(null, oldPath, null);
			if (File.Exists(dummyFile)) File.Delete(dummyFile);
		}
        Console.WriteLine("OK");
    }
}
