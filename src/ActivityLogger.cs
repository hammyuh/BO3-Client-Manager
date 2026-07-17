using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BO3ClientManager;

public static class ActivityLogger
{
	private static readonly object sync = new object();

	private static readonly string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BO3 Client Manager", "activity.log");

	public static void Log(string message)
	{
		Log("Info", message);
	}

	public static void Log(string level, string message)
	{
		try
		{
			lock (sync)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
				File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{level}|{message}{Environment.NewLine}");
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
		}
	}

	public static IReadOnlyList<ActivityLogEntry> ReadRecent(int count = 200)
	{
		try
		{
			lock (sync)
			{
				if (!File.Exists(logPath))
				{
					return Array.Empty<ActivityLogEntry>();
				}
				return File.ReadLines(logPath).Reverse().Take(count)
					.Select(Parse)
					.ToArray();
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return Array.Empty<ActivityLogEntry>();
		}
	}

	private static ActivityLogEntry Parse(string line)
	{
		string[] parts = line.Split('|', 3);
		if (parts.Length == 3)
		{
			return new ActivityLogEntry
			{
				Timestamp = parts[0],
				Level = parts[1],
				Message = parts[2]
			};
		}
		return new ActivityLogEntry
		{
			Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			Level = "Info",
			Message = line
		};
	}
}
