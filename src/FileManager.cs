using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BO3ClientManager;

public static class FileManager
{
	public static readonly string standbyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BO3 Client Manager", "Standby");

	public static readonly string[] Clients = new string[7] { "Stock BO3", "T7 Patch", "BOIII Community", "Ezz BOIII", "T7x", "CleanOps T7", "Old BO3 Exe" };

	public static readonly Dictionary<string, string[]> RequiredFiles = new Dictionary<string, string[]>
	{
		["BOIII Community"] = new string[1] { "boiii.exe" },
		["T7 Patch"] = new string[4] { "dsound.dll", "t7patch.conf", "t7patch.dll", "t7patchloader.dll" },
		["Ezz BOIII"] = new string[1] { "boiii.exe" },
		["CleanOps T7"] = new string[1] { "d3d11.dll" },
		["T7x"] = new string[1] { "t7x.exe" }
	};

	public static void Load()
	{
		Directory.CreateDirectory(standbyPath);
	}

	public static void CreateStandbyFolders()
	{
		Directory.CreateDirectory(standbyPath);
		string[] clients = Clients;
		foreach (string client in clients)
		{
			Directory.CreateDirectory(Path.Combine(standbyPath, client));
		}
	}

	public static bool CheckClient(string client)
	{
		if (RequiredFiles.TryGetValue(client, out var files))
		{
			return files.All((string file) => File.Exists(Path.Combine(standbyPath, client, file)));
		}
		return false;
	}

	public static bool CheckStockBO3()
	{
		return ExeChecker.CheckBO3(Path.Combine(standbyPath, "Stock BO3", "BlackOps3.exe")) == "New";
	}

	public static bool CheckOldExe()
	{
		return ExeChecker.CheckBO3(Path.Combine(standbyPath, "Old BO3 Exe", "BlackOps3.exe")) == "Old";
	}

	public static bool TrySwapClient(string client, string gamePath, out string error)
	{
		error = "";
		if (!Clients.Contains(client) || client == "Old BO3 Exe")
		{
			error = "The selected profile is not supported.";
			return false;
		}
		if (!Directory.Exists(gamePath) || !File.Exists(Path.Combine(gamePath, "BlackOps3.exe")))
		{
			error = "The Black Ops III folder is missing or invalid.";
			return false;
		}
		if (!TryRequiresOldExe(client, out var requiresOldExe))
		{
			error = "Could not verify executable requirements. Check your connection and try again.";
			return false;
		}
		string exeSource = Path.Combine(standbyPath, requiresOldExe ? "Old BO3 Exe" : "Stock BO3", "BlackOps3.exe");
		string expectedStatus = (requiresOldExe ? "Old" : "New");
		if (ExeChecker.CheckBO3(exeSource) != expectedStatus)
		{
			error = (requiresOldExe ? "A verified old Black Ops III executable is required." : "A verified stock Black Ops III executable is required.");
			return false;
		}
		if (client != "Stock BO3" && !CheckClient(client))
		{
			error = "The selected client is incomplete.";
			return false;
		}
		Config config = ConfigManager.Load();
		string[] previousFiles;
		string[] oldFiles = RequiredFiles.TryGetValue(config.appliedClient, out previousFiles) ? previousFiles ?? Array.Empty<string>() : Array.Empty<string>();
		string[] newFiles = IsStandaloneClient(client) ? Array.Empty<string>() : (RequiredFiles.TryGetValue(client, out var selectedFiles) ? selectedFiles ?? Array.Empty<string>() : Array.Empty<string>());
		string[] affectedFiles = oldFiles.Concat(newFiles).Append("BlackOps3.exe").Distinct<string>(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		string transactionPath = Path.Combine(Path.GetTempPath(), "BO3ClientManager", Guid.NewGuid().ToString("N"));
		try
		{
			BackupManager.CreateRecoveryBackup(gamePath, affectedFiles);
			Directory.CreateDirectory(transactionPath);
			string[] array = affectedFiles;
			foreach (string file in array)
			{
				string destination = Path.Combine(gamePath, file);
				if (File.Exists(destination))
				{
					File.Copy(destination, Path.Combine(transactionPath, file), overwrite: true);
				}
			}
			foreach (string file2 in oldFiles.Except<string>(newFiles, StringComparer.OrdinalIgnoreCase))
			{
				string destination2 = Path.Combine(gamePath, file2);
				if (File.Exists(destination2))
				{
					File.Delete(destination2);
				}
			}
			File.Copy(exeSource, Path.Combine(gamePath, "BlackOps3.exe"), overwrite: true);
			array = newFiles;
			foreach (string file3 in array)
			{
				File.Copy(Path.Combine(standbyPath, client, file3), Path.Combine(gamePath, file3), overwrite: true);
			}
			config.appliedClient = client;
			if (!ConfigManager.Save(config))
			{
				throw new IOException("The selected profile was applied, but its configuration could not be saved.");
			}
			ActivityLogger.Log("Success", "Applied profile: " + client);
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			RestoreTransaction(gamePath, affectedFiles, transactionPath);
			error = "Could not apply " + client + ". Close Black Ops III and any related tools, then try again. " + ex.Message;
			ActivityLogger.Log("Error", "Failed to apply " + client + ": " + ex.Message);
			return false;
		}
		finally
		{
			try
			{
				if (Directory.Exists(transactionPath))
				{
					Directory.Delete(transactionPath, recursive: true);
				}
			}
			catch (Exception ex2) when (((ex2 is IOException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
			{
			}
		}
	}

	public static string DetectAppliedClient(string gamePath)
	{
		if (!Directory.Exists(gamePath))
		{
			return "Unknown";
		}
		foreach (string client in Clients.Where((string text) => !(text == "Stock BO3") && !(text == "Old BO3 Exe")))
		{
			if (IsClientApplied(client, gamePath))
			{
				return client;
			}
		}
		if (!(ExeChecker.CheckBO3(Path.Combine(gamePath, "BlackOps3.exe")) == "New"))
		{
			return "Unknown";
		}
		return "Stock BO3";
	}

	public static bool IsClientApplied(string client, string gamePath)
	{
		if (!RequiredFiles.TryGetValue(client, out string[] files))
		{
			return false;
		}
		string clientPath = (IsStandaloneClient(client) ? Path.Combine(standbyPath, client) : gamePath);
		if (!files.All((string file) => File.Exists(Path.Combine(clientPath, file))))
		{
			return false;
		}
		if (!TryRequiresOldExe(client, out var requiresOldExe))
		{
			return false;
		}
		return ExeChecker.CheckBO3(Path.Combine(gamePath, "BlackOps3.exe")) == (requiresOldExe ? "Old" : "New");
	}

	private static bool TryRequiresOldExe(string client, out bool requiresOldExe)
	{
		requiresOldExe = false;
		string? json = HashUpdater.GetHashes();
		if (!HashUpdater.IsValid(json))
		{
			return false;
		}
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			if (!document.RootElement.GetProperty("clients").TryGetProperty(client, out var clientData))
			{
				return client == "Stock BO3";
			}
			if (!clientData.TryGetProperty("requiresOldExe", out var value) || (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
			{
				return false;
			}
			requiresOldExe = value.GetBoolean();
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	public static bool IsStandaloneClient(string client)
	{
		switch (client)
		{
		case "BOIII Community":
		case "Ezz BOIII":
		case "T7x":
			return true;
		default:
			return false;
		}
	}

	private static void RestoreTransaction(string gamePath, IEnumerable<string> files, string transactionPath)
	{
		foreach (string file in files)
		{
			string destination = Path.Combine(gamePath, file);
			string backup = Path.Combine(transactionPath, file);
			try
			{
				if (File.Exists(backup))
				{
					File.Copy(backup, destination, overwrite: true);
				}
				else if (File.Exists(destination))
				{
					File.Delete(destination);
				}
			}
			catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
			{
			}
		}
	}
}
