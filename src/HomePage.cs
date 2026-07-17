using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace T7_Hub;

public partial class HomePage : Page, IComponentConnector
{
	private readonly Config config;

	private readonly DispatcherTimer processTimer;

	private readonly Dictionary<string, Brush> exeStatusColors = new Dictionary<string, Brush>
	{
		["Old"] = Brushes.Yellow,
		["New"] = Brushes.LimeGreen,
		["Unknown"] = Brushes.Red,
		["Missing"] = Brushes.Gray
	};

	private bool profileNeedsApplying;

	private bool isLaunching;

	private bool wasRunning;

	private int statusRequestId;

	public HomePage()
	{
		InitializeComponent();
		BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250L)));
		config = ConfigManager.Load();
		processTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1L)
		};
		processTimer.Tick += delegate
		{
			UpdateActionButtons();
		};
		processTimer.Start();
		base.Unloaded += delegate
		{
			processTimer.Stop();
		};
		UpdateClientAvailability();
		LoadProfile();
		UpdateActionButtons();
		UpdateCleanOpsWarning();
		base.Loaded += HomePage_Loaded;
	}

	private async void HomePage_Loaded(object sender, RoutedEventArgs e)
	{
		await Task.Yield();
		await DetectCurrentProfileAsync();
		LoadProfile();
		await UpdateExeStatusAsync();
	}

	private async Task DetectCurrentProfileAsync()
	{
		if (string.IsNullOrWhiteSpace(config.GamePath) || !Directory.Exists(config.GamePath))
		{
			return;
		}
		string configuredClient = config.appliedClient;
		string gamePath = config.GamePath;
		if (!(await Task.Run(() => FileManager.IsStandaloneClient(configuredClient) && FileManager.IsClientApplied(configuredClient, gamePath))))
		{
			string detected = await Task.Run(() => FileManager.DetectAppliedClient(gamePath));
			if (detected != "Unknown")
			{
				config.appliedClient = detected;
				ConfigManager.Save(config);
			}
		}
	}

	private void LoadProfile()
	{
		foreach (ComboBoxItem item in (IEnumerable)ProfileSelector.Items)
		{
			if (GetClient(item) == config.appliedClient)
			{
				ProfileSelector.SelectedItem = item;
				return;
			}
		}
		ProfileSelector.SelectedIndex = 0;
	}

	private void UpdateClientAvailability()
	{
		foreach (ComboBoxItem item in (IEnumerable)ProfileSelector.Items)
		{
			string client = GetClient(item);
			item.IsEnabled = client == "Stock BO3" || FileManager.CheckClient(client);
		}
	}

	private void SettingsButton_Click(object sender, RoutedEventArgs e)
	{
		base.NavigationService.Navigate(new SettingsPage());
	}

	private bool ApplySelectedClient()
	{
		if (ProcessChecker.IsGameOrClientRunning())
		{
			MessageBox.Show("Close Black Ops III, BOIII, or T7x before applying a profile.", "Game Running", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		if (!(ProfileSelector.SelectedItem is ComboBoxItem item))
		{
			return false;
		}
		string client = GetClient(item);
		if (string.IsNullOrWhiteSpace(config.GamePath))
		{
			MessageBox.Show("Please set your Black Ops III folder in Settings.");
			return false;
		}
		if (client != "Stock BO3" && !FileManager.CheckClient(client))
		{
			MessageBox.Show("This client is not installed.");
			return false;
		}
		SetActionStatus("Applying " + client + "...", Brushes.LightSkyBlue);
		if (!FileManager.TrySwapClient(client, config.GamePath, out string error))
		{
			MessageBox.Show(error, "Unable to Apply Profile", MessageBoxButton.OK, MessageBoxImage.Hand);
			SetActionStatus("Could not apply the selected profile.", Brushes.IndianRed);
			return false;
		}
		config.appliedClient = client;
		profileNeedsApplying = false;
		UpdateActionButtons();
		_ = UpdateExeStatusAsync();
		SetActionStatus(client + " applied successfully.", Brushes.LightGreen);
		return true;
	}

	private async void LaunchButton_Click(object sender, RoutedEventArgs e)
	{
		if (!(ProfileSelector.SelectedItem is ComboBoxItem item))
		{
			return;
		}
		string client = GetClient(item);
		if (!(client != config.appliedClient) || ApplySelectedClient())
		{
			isLaunching = true;
			LaunchButton.Content = "LAUNCHING...";
			UpdateActionButtons();
			SetActionStatus("Launching " + client + "...", Brushes.LightSkyBlue);
			if (!LauncherBackend.TryLaunch(client, out string error))
			{
				SetActionStatus(error, Brushes.IndianRed);
			}
			else
			{
				await Task.Delay(1200);
				SetActionStatus(client + " launched.", Brushes.LightGreen);
			}
			isLaunching = false;
			LaunchButton.Content = "LAUNCH";
			UpdateActionButtons();
		}
	}

	private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ProfileSelector.SelectedItem is ComboBoxItem item)
		{
			string selectedClient = GetClient(item);
			ProfileSelector.Foreground = item.Foreground;
			profileNeedsApplying = string.IsNullOrWhiteSpace(config.GamePath) || config.appliedClient != selectedClient;
			UpdateActionButtons();
			UpdateCleanOpsWarning();
		}
	}

	private void ApplyButton_Click(object sender, RoutedEventArgs e)
	{
		ApplySelectedClient();
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		RefreshButton.IsEnabled = false;
		SetActionStatus("Refreshing installed profiles...", Brushes.LightSkyBlue);
		UpdateClientAvailability();
		await DetectCurrentProfileAsync();
		LoadProfile();
		await UpdateExeStatusAsync();
		SetActionStatus("Profiles refreshed.", Brushes.LightGreen);
		RefreshButton.IsEnabled = true;
	}

	private async Task UpdateExeStatusAsync()
	{
		if (string.IsNullOrWhiteSpace(config.GamePath))
		{
			ExeStatusText.Text = "No BO3 path set";
			ExeStatusText.Foreground = Brushes.Gray;
			return;
		}
		int requestId = ++statusRequestId;
		string exePath = Path.Combine(config.GamePath, "BlackOps3.exe");
		ExeStatusText.Text = "Checking executable...";
		ExeStatusText.Foreground = Brushes.Gray;
		string status = await Task.Run(() => ExeChecker.CheckBO3(exePath));
		if (requestId == statusRequestId)
		{
			TextBlock exeStatusText = ExeStatusText;
			exeStatusText.Text = status switch
			{
				"Old" => "Old BO3 executable", 
				"New" => "New BO3 executable", 
				"Unknown" => "Unknown executable", 
				"Missing" => "Missing executable", 
				_ => "Unknown", 
			};
			ExeStatusText.Foreground = (exeStatusColors.TryGetValue(status, out Brush brush) ? brush : Brushes.Gray);
		}
	}

	private void UpdateCleanOpsWarning()
	{
		CleanOpsWarning.Visibility = ((!(ProfileSelector.SelectedItem is ComboBoxItem item) || !(GetClient(item) == "CleanOps T7")) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateActionButtons()
	{
		bool isRunning = ProcessChecker.IsGameOrClientRunning();
		LaunchButton.IsEnabled = !isRunning && !isLaunching;
		ApplyButton.IsEnabled = !isRunning && profileNeedsApplying;
		if (isRunning && !wasRunning)
		{
			SetActionStatus("Close Black Ops III, BOIII, or T7x to switch profiles.", Brushes.Orange);
		}
		else if (!isRunning && wasRunning && !isLaunching)
		{
			SetActionStatus("Ready", Brushes.LightGray);
		}
		wasRunning = isRunning;
	}

	private static string GetClient(ComboBoxItem item)
	{
		return item.Tag?.ToString() ?? "";
	}

	private void SetActionStatus(string message, Brush brush)
	{
		ActionStatusText.Text = message;
		ActionStatusText.Foreground = brush;
	}
}
