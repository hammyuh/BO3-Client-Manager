using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BO3ClientManager;

public static class HashUpdater
{
	private static readonly string hashPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BO3 Client Manager", "hashes.json");

	private static readonly SemaphoreSlim updateLock = new SemaphoreSlim(1, 1);

	private const string HashUrl = "https://raw.githubusercontent.com/hammyuh/BO3-Client-Manager/refs/heads/main/hashes.json";

	public static bool CanProceed { get; private set; }

	public static bool HasCachedHashesAtStartup { get; private set; }

	public static void PrimeSessionState()
	{
		HasCachedHashesAtStartup = IsValid(GetHashes());
		CanProceed = HasCachedHashesAtStartup;
	}

	public static async Task<bool> UpdateHashes()
	{
		await updateLock.WaitAsync();
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(5L)
			};
			string json = await client.GetStringAsync("https://raw.githubusercontent.com/hammyuh/BO3-Client-Manager/refs/heads/main/hashes.json");
			if (!IsValid(json))
			{
				return false;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(hashPath));
			string text = hashPath + ".tmp";
			File.WriteAllText(text, json);
			File.Move(text, hashPath, overwrite: true);
			CanProceed = true;
			return true;
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is IOException || ex is UnauthorizedAccessException || ex is JsonException) ? 1 : 0) != 0)
		{
			return false;
		}
		finally
		{
			string tempPath = hashPath + ".tmp";
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
			updateLock.Release();
		}
	}

	public static string? GetHashes()
	{
		try
		{
			return File.Exists(hashPath) ? File.ReadAllText(hashPath) : null;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	public static DateTime GetVersion()
	{
		try
		{
			return File.Exists(hashPath) ? File.GetLastWriteTimeUtc(hashPath) : DateTime.MinValue;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return DateTime.MinValue;
		}
	}

	public static bool IsValid(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			if (!document.RootElement.TryGetProperty("blackops3", out var blackOps3))
			{
				return false;
			}
			JsonElement oldHashes;
			JsonElement newHashes;
			JsonElement clients;
			return blackOps3.TryGetProperty("old", out oldHashes) && oldHashes.ValueKind == JsonValueKind.Array && blackOps3.TryGetProperty("new", out newHashes) && newHashes.ValueKind == JsonValueKind.Array && document.RootElement.TryGetProperty("clients", out clients) && clients.ValueKind == JsonValueKind.Object;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}
