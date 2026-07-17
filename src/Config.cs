namespace T7_Hub;

public class Config
{
	public string GamePath { get; set; } = "";

	public string appliedClient { get; set; } = "Stock BO3";

	public bool HasCompletedSetup { get; set; }

	public string UpdateVersionSeen { get; set; } = "";

	public int UpdateReminderLaunchesLeft { get; set; }
}
