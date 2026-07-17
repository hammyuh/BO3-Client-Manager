using System;
using System.IO;

namespace T7_Hub;

public static class ExeManager
{
	private static string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7 Hub", "Standby", "Backup");

	private static string standbyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7 Hub", "Standby");

	private static string newExe => Path.Combine(standbyPath, "Stock BO3", "BlackOps3.exe");

	public static void UseOldExe(string gamePath)
	{
		string source = Path.Combine(FileManager.standbyPath, "Old BO3 Exe", "BlackOps3.exe");
		string destination = Path.Combine(gamePath, "BlackOps3.exe");
		if (File.Exists(source))
		{
			File.Copy(source, destination, overwrite: true);
		}
	}

	public static void UseNewExe(string gamePath)
	{
		string source = Path.Combine(FileManager.standbyPath, "Stock BO3", "BlackOps3.exe");
		string destination = Path.Combine(gamePath, "BlackOps3.exe");
		if (File.Exists(source))
		{
			File.Copy(source, destination, overwrite: true);
		}
	}

	public static void BackupCurrentExe(string gamePath)
	{
		Directory.CreateDirectory(backupPath);
		string current = Path.Combine(gamePath, "BlackOps3.exe");
		string backup = Path.Combine(backupPath, "BlackOps3.exe");
		if (File.Exists(current))
		{
			File.Copy(current, backup, overwrite: true);
		}
	}
}
