using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace BO3ClientManager;

public static class BackupManager
{
	public static string BackupRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BO3 Client Manager", "Backups");

	public static void CreateRecoveryBackup(string gamePath, IEnumerable<string> files)
	{
		try
		{
			string backupPath = Path.Combine(BackupRoot, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
			Directory.CreateDirectory(backupPath);
			if (File.Exists(ConfigManager.ConfigPath))
			{
				File.Copy(ConfigManager.ConfigPath, Path.Combine(backupPath, "config.json"), overwrite: true);
			}
			foreach (string file in files.Distinct<string>(StringComparer.OrdinalIgnoreCase))
			{
				string source = Path.Combine(gamePath, file);
				if (File.Exists(source))
				{
					File.Copy(source, Path.Combine(backupPath, file), overwrite: true);
				}
			}
			foreach (DirectoryInfo item in (from directory in new DirectoryInfo(BackupRoot).GetDirectories()
				orderby directory.CreationTimeUtc descending
				select directory).Skip(3))
			{
				item.Delete(recursive: true);
			}
			ActivityLogger.Log("Info", "Created automatic profile recovery backup");
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			ActivityLogger.Log("Warning", "Could not create automatic recovery backup: " + ex.Message);
		}
	}

	public static bool RestoreFromZip(string zipPath, string? targetGamePath, out string error)
	{
		error = "";
		if (!File.Exists(zipPath))
		{
			error = "The selected backup file does not exist.";
			return false;
		}
		string extractPath = Path.Combine(Path.GetTempPath(), "BO3ClientManagerRestore", Guid.NewGuid().ToString("N"));
		try
		{
			if (!IsValidBackupZip(zipPath, out error))
			{
				return false;
			}
			Directory.CreateDirectory(extractPath);
			ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
			return RestoreFromFolder(extractPath, targetGamePath, out error);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) ? 1 : 0) != 0)
		{
			error = ex.Message;
			ActivityLogger.Log("Error", "Backup restore failed: " + ex.Message);
			return false;
		}
		finally
		{
			try
			{
				if (Directory.Exists(extractPath))
				{
					Directory.Delete(extractPath, recursive: true);
				}
			}
			catch (Exception ex2) when (((ex2 is IOException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
			{
			}
		}
	}

	public static bool Export(string destinationPath, out string error)
	{
		try
		{
			if (File.Exists(destinationPath))
			{
				File.Delete(destinationPath);
			}
			using FileStream stream = File.Create(destinationPath);
			using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create);
			if (File.Exists(ConfigManager.ConfigPath))
			{
				archive.CreateEntryFromFile(ConfigManager.ConfigPath, "config.json", CompressionLevel.Optimal);
			}
			if (Directory.Exists(FileManager.standbyPath))
			{
				foreach (string file in Directory.EnumerateFiles(FileManager.standbyPath, "*", SearchOption.AllDirectories))
				{
					string entryName = Path.Combine("Standby", Path.GetRelativePath(FileManager.standbyPath, file)).Replace('\\', '/');
					archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
				}
			}
			ActivityLogger.Log("Info", "Exported backup to " + destinationPath);
			error = "";
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) ? 1 : 0) != 0)
		{
			error = ex.Message;
			ActivityLogger.Log("Error", "Backup export failed: " + ex.Message);
			return false;
		}
	}

	private static bool RestoreFromFolder(string backupPath, string? targetGamePath, out string error)
	{
		error = "";
		try
		{
			Config backupConfig = new Config();
			string configPath = Path.Combine(backupPath, "config.json");
			if (File.Exists(configPath))
			{
				Config? loaded = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
				if (loaded != null)
				{
					backupConfig = loaded;
				}
			}
			string gamePath = !string.IsNullOrWhiteSpace(targetGamePath) ? targetGamePath : backupConfig.GamePath;
			if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
			{
				error = "Set a valid Black Ops III folder first, then try restore again.";
				return false;
			}
			int restoredFiles = 0;
			foreach (string source in Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories))
			{
				if (!Path.GetFileName(source).Equals("config.json", StringComparison.OrdinalIgnoreCase))
				{
					string relative = Path.GetRelativePath(backupPath, source);
					string obj = ((relative.StartsWith($"Standby{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("Standby/", StringComparison.OrdinalIgnoreCase)) ? FileManager.standbyPath : gamePath);
					if (obj == FileManager.standbyPath)
					{
						relative = Path.GetRelativePath(Path.Combine(backupPath, "Standby"), source);
					}
					string destination = Path.Combine(obj, relative);
					string? directory = Path.GetDirectoryName(destination);
					if (!string.IsNullOrWhiteSpace(directory))
					{
						Directory.CreateDirectory(directory);
					}
					File.Copy(source, destination, overwrite: true);
					restoredFiles++;
				}
			}
			if (restoredFiles == 0)
			{
				error = "The selected backup did not contain any restorable files.";
				return false;
			}
			backupConfig.GamePath = gamePath;
			backupConfig.HasCompletedSetup = true;
			ConfigManager.Save(backupConfig);
			ActivityLogger.Log("Success", $"Restored {restoredFiles} files from {backupPath}");
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is JsonException) ? 1 : 0) != 0)
		{
			error = ex.Message;
			ActivityLogger.Log("Error", "Backup restore failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsValidBackupZip(string zipPath, out string error)
	{
		error = "";
		try
		{
			using ZipArchive archive = ZipFile.OpenRead(zipPath);
			if (archive.Entries.Count == 0)
			{
				error = "The selected zip is empty.";
				return false;
			}
			bool hasConfig = false;
			bool hasKnownBackupFile = false;
			HashSet<string> knownFiles = GetKnownBackupFiles();
			foreach (ZipArchiveEntry entry in archive.Entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Name))
				{
					continue;
				}
				if (entry.FullName.Replace('\\', '/').Equals("config.json", StringComparison.OrdinalIgnoreCase))
				{
					hasConfig = true;
					continue;
				}
				string entryName = entry.Name;
				if (knownFiles.Contains(entryName))
				{
					hasKnownBackupFile = true;
				}
				if (entry.FullName.Replace('\\', '/').StartsWith("Standby/", StringComparison.OrdinalIgnoreCase))
				{
					hasKnownBackupFile = true;
				}
			}
			if (!hasConfig)
			{
				error = "This zip does not contain a BO3 Client Manager backup config.";
				return false;
			}
			if (!hasKnownBackupFile)
			{
				error = "This zip does not contain any recognized backup files.";
				return false;
			}
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) ? 1 : 0) != 0)
		{
			error = ex.Message;
			return false;
		}
	}

	private static HashSet<string> GetKnownBackupFiles()
	{
		HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BlackOps3.exe" };
		foreach (string[] value in FileManager.RequiredFiles.Values)
		{
			foreach (string file in value)
			{
				files.Add(file);
			}
		}
		return files;
	}
}
