using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace BO3ClientManager;

public static class ExeChecker
{
	private static readonly Dictionary<string, (long Length, DateTime LastWrite, DateTime HashVersion, string Status)> cache = new Dictionary<string, (long, DateTime, DateTime, string)>(StringComparer.OrdinalIgnoreCase);

	public static string CheckBO3(string exePath)
	{
		try
		{
			if (!File.Exists(exePath))
			{
				return "Missing";
			}
			FileInfo file = new FileInfo(exePath);
			DateTime hashVersion = HashUpdater.GetVersion();
			lock (cache)
			{
				if (cache.TryGetValue(exePath, out (long, DateTime, DateTime, string) cached) && cached.Item1 == file.Length && cached.Item2 == file.LastWriteTimeUtc && cached.Item3 == hashVersion)
				{
					return cached.Item4;
				}
			}
			string? json = HashUpdater.GetHashes();
			if (!HashUpdater.IsValid(json))
			{
				return "Unknown";
			}
			string hash = GetHash(exePath);
			using JsonDocument document = JsonDocument.Parse(json!);
			JsonElement blackOps3 = document.RootElement.GetProperty("blackops3");
			string status = (ContainsHash(blackOps3.GetProperty("old"), hash) ? "Old" : (ContainsHash(blackOps3.GetProperty("new"), hash) ? "New" : "Unknown"));
			lock (cache)
			{
				cache[exePath] = (file.Length, file.LastWriteTimeUtc, hashVersion, status);
			}
			return status;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException || ex is JsonException) ? 1 : 0) != 0)
		{
			return "Unknown";
		}
	}

	private static string GetHash(string file)
	{
		using FileStream stream = File.OpenRead(file);
		return Convert.ToHexString(SHA256.HashData(stream));
	}

	private static bool ContainsHash(JsonElement hashes, string hash)
	{
		foreach (JsonElement item2 in hashes.EnumerateArray())
		{
			if (string.Equals(hash, item2.GetString(), StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
