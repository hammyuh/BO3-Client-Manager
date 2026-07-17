using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace T7_Hub;

public static class UpdateManager
{
	private sealed record UpdateInfo(string Tag, string Url);

	private static readonly HttpClient client = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8L)
	};

	public static async Task CheckAndPromptAsync(Window owner)
	{
		try
		{
			Config config = ConfigManager.Load();
			string currentVersion = GetCurrentVersion();
			UpdateInfo? latest = await GetLatestReleaseAsync();
			if (latest == null || !IsNewer(latest.Tag, currentVersion))
			{
				return;
			}
			if (!string.Equals(config.UpdateVersionSeen, latest.Tag, StringComparison.OrdinalIgnoreCase))
			{
				config.UpdateVersionSeen = latest.Tag;
				config.UpdateReminderLaunchesLeft = 0;
				ConfigManager.Save(config);
			}
			if (config.UpdateReminderLaunchesLeft > 0)
			{
				config.UpdateReminderLaunchesLeft--;
				ConfigManager.Save(config);
				return;
			}
			ActivityLogger.Log("Info", "Update available: " + latest.Tag);
			if (owner is MainWindow window)
			{
				window.SetModalOverlayVisible(visible: true);
			}
			bool? dialogResult = new UpdatePromptWindow(latest.Tag)
			{
				Owner = owner
			}.ShowDialog();
			if (owner is MainWindow windowAfter)
			{
				windowAfter.SetModalOverlayVisible(visible: false);
			}
			if (dialogResult == true)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = latest.Url,
					UseShellExecute = true
				});
			}
			else
			{
				config.UpdateReminderLaunchesLeft = 2;
				ConfigManager.Save(config);
			}
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException) ? 1 : 0) != 0)
		{
			ActivityLogger.Log("Warning", "Update check failed: " + ex.Message);
		}
	}

	private static async Task<UpdateInfo?> GetLatestReleaseAsync()
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/hammyuh/T7-Hub/releases/latest");
		request.Headers.UserAgent.Add(new ProductInfoHeaderValue("T7-Hub", "1.0"));
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		using HttpResponseMessage response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			return null;
		}
		using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		string tag = document.RootElement.GetProperty("tag_name").GetString() ?? "";
		string url = document.RootElement.GetProperty("html_url").GetString() ?? "";
		if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(url))
		{
			return null;
		}
		return new UpdateInfo(tag, url);
	}

	private static string GetCurrentVersion()
	{
		Version? version = typeof(UpdateManager).Assembly.GetName().Version;
		if (version != null)
		{
			return version.ToString(3);
		}
		return "0.0.0";
	}

	private static bool IsNewer(string releaseTag, string currentVersion)
	{
		Version? releaseVersion = ParseVersion(releaseTag);
		Version? localVersion = ParseVersion(currentVersion);
		if (releaseVersion != null && localVersion != null)
		{
			return releaseVersion > localVersion;
		}
		return false;
	}

	private static Version? ParseVersion(string value)
	{
		if (!Version.TryParse(value.Trim().TrimStart(new char[2] { 'v', 'V' }), out Version version))
		{
			return null;
		}
		return version;
	}
}
