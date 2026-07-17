using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace T7_Hub;

public class GameLocator
{
	public static string? GetSteamPath()
	{
		try
		{
			return Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null) as string;
		}
		catch (Exception ex) when (((ex is UnauthorizedAccessException || ex is SecurityException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	public static List<string> GetSteamLibraries()
	{
		List<string> libraries = new List<string>();
		string steamPath = GetSteamPath();
		if (string.IsNullOrWhiteSpace(steamPath))
		{
			return libraries;
		}
		string libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
		if (!File.Exists(libraryFile))
		{
			return libraries;
		}
		try
		{
			foreach (string line in File.ReadLines(libraryFile))
			{
				if (!line.Contains("\"path\""))
				{
					continue;
				}
				string[] parts = line.Split('"');
				if (parts.Length >= 4)
				{
					string path = parts[3].Replace("\\\\", "\\");
					if (Directory.Exists(path))
					{
						libraries.Add(path);
					}
				}
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
		}
		return libraries;
	}

	public static string? FindBO3()
	{
		foreach (string steamLibrary in GetSteamLibraries())
		{
			string bo3Path = Path.Combine(steamLibrary, "steamapps", "common", "Call of Duty Black Ops III");
			if (File.Exists(Path.Combine(bo3Path, "BlackOps3.exe")))
			{
				return bo3Path;
			}
		}
		return null;
	}
}
