using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace T7_Hub;

public static class LauncherBackend
{
	public static bool TryLaunch(string client, out string error)
	{
		Config config = ConfigManager.Load();
		try
		{
			switch (client)
			{
			case "Stock BO3":
			case "T7 Patch":
			case "CleanOps T7":
				Process.Start(new ProcessStartInfo
				{
					FileName = "steam://rungameid/311210",
					UseShellExecute = true
				});
				break;
			case "BOIII Community":
				StartClient(config.GamePath, Path.Combine(FileManager.standbyPath, "BOIII Community", "boiii.exe"));
				break;
			case "Ezz BOIII":
				StartClient(config.GamePath, Path.Combine(FileManager.standbyPath, "Ezz BOIII", "boiii.exe"));
				break;
			case "T7x":
				StartClient(config.GamePath, Path.Combine(FileManager.standbyPath, "T7x", "t7x.exe"));
				break;
			default:
				error = "The selected profile is not supported.";
				return false;
			}
			ActivityLogger.Log("Success", "Launched profile: " + client);
			error = "";
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is Win32Exception || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			error = "Could not launch " + client + ". " + ex.Message;
			ActivityLogger.Log("Error", "Failed to launch " + client + ": " + ex.Message);
			return false;
		}
	}

	private static void StartClient(string gamePath, string executablePath)
	{
		if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
		{
			throw new DirectoryNotFoundException("Set a valid Black Ops III folder in Settings.");
		}
		if (!File.Exists(executablePath))
		{
			throw new FileNotFoundException(Path.GetFileName(executablePath) + " is not installed in its T7 Hub Standby folder.", executablePath);
		}
		Process.Start(new ProcessStartInfo
		{
			FileName = executablePath,
			WorkingDirectory = gamePath,
			UseShellExecute = true
		});
	}
}
