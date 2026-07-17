namespace T7_Hub;

public class ActivityLogEntry
{
	public string Timestamp { get; set; } = "";

	public string Level { get; set; } = "Info";

	public string Message { get; set; } = "";

	public string Raw => Timestamp + "  " + Message;
}
