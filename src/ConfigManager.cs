using System;
using System.IO;
using System.Text.Json;

namespace T7_Hub;

public static class ConfigManager
{
	private static readonly string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7 Hub", "config.json");

	private static readonly JsonSerializerOptions options = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static string ConfigPath => configPath;

	public static Config Load()
	{
		try
		{
			if (File.Exists(configPath))
			{
				Config? config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
				if (config != null)
				{
					Config config2 = config;
					if (config2.GamePath == null)
					{
						config2.GamePath = "";
					}
					config2 = config;
					if (config2.appliedClient == null)
					{
						config2.appliedClient = "Stock BO3";
					}
					return config;
				}
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is JsonException) ? 1 : 0) != 0)
		{
		}
		Config config3 = new Config();
		Save(config3);
		return config3;
	}

	public static bool Save(Config config)
	{
		string? directory = Path.GetDirectoryName(configPath);
		if (string.IsNullOrWhiteSpace(directory))
		{
			return false;
		}
		string tempPath = configPath + ".tmp";
		try
		{
			Directory.CreateDirectory(directory);
			File.WriteAllText(tempPath, JsonSerializer.Serialize(config, options));
			File.Move(tempPath, configPath, overwrite: true);
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return false;
		}
		finally
		{
			try
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
			catch (Exception ex2) when (((ex2 is IOException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
			{
			}
		}
	}
}
