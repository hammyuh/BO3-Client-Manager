using System.Diagnostics;

namespace T7_Hub;

public static class ProcessChecker
{
	private static readonly string[] processNames = new string[3] { "BlackOps3", "boiii", "t7x" };

	public static bool IsGameOrClientRunning()
	{
		string[] array = processNames;
		for (int i = 0; i < array.Length; i++)
		{
			Process[] processes = Process.GetProcessesByName(array[i]);
			try
			{
				if (processes.Length != 0)
				{
					return true;
				}
			}
			finally
			{
				Process[] array2 = processes;
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].Dispose();
				}
			}
		}
		return false;
	}
}
