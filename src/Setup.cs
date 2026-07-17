using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace T7_Hub;

public partial class Setup : Page, IComponentConnector
{
	private readonly DispatcherTimer clientTimer;

	private readonly Config config;

	public Setup()
	{
		InitializeComponent();
		config = ConfigManager.Load();
		base.Loaded += Setup_Loaded;
		FileManager.CreateStandbyFolders();
		clientTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(3L)
		};
		clientTimer.Tick += ClientTimer_Tick;
		clientTimer.Start();
		base.Unloaded += delegate
		{
			clientTimer.Stop();
		};
	}

	private void Setup_Loaded(object sender, RoutedEventArgs e)
	{
		string? path = (string.IsNullOrWhiteSpace(config.GamePath) || !ValidateBO3Path(config.GamePath, showErrors: false)) ? GameLocator.FindBO3() : config.GamePath;
		if (!string.IsNullOrWhiteSpace(path))
		{
			BO3PathTB.Text = path;
			SaveGamePath(path);
		}
		else
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				MessageBox.Show("Could not locate Black Ops III installation. Please locate manually.");
			}, DispatcherPriority.ApplicationIdle);
		}
		UpdateClientStatus();
	}

	private void ClientTimer_Tick(object? sender, EventArgs e)
	{
		UpdateClientStatus();
	}

	private void UpdateClientStatus()
	{
		ClientList.Children.Clear();
		string[] array = new string[7] { "T7 Patch", "BOIII Community", "Ezz BOIII", "T7x", "CleanOps T7", "Stock BO3", "Old BO3 Exe" };
		foreach (string client in array)
		{
			bool installed = client == "Stock BO3" ? FileManager.CheckStockBO3() : (client != "Old BO3 Exe" ? FileManager.CheckClient(client) : FileManager.CheckOldExe());
			TextBlock text = new TextBlock
			{
				Text = installed ? ("✓ " + client) : ("✗ " + client),
				Foreground = installed ? Brushes.LimeGreen : Brushes.Gray,
				FontSize = 14.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(5.0)
			};
			ClientList.Children.Add(text);
		}
	}

	private void SaveGamePath(string path)
	{
		config.GamePath = path;
		ConfigManager.Save(config);
	}

	private void BrowseButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog dialog = new OpenFolderDialog();
		if (dialog.ShowDialog() == true)
		{
			string path = dialog.FolderName ?? "";
			if (ValidateBO3Path(path, showErrors: true))
			{
				BO3PathTB.Text = path;
				SaveGamePath(path);
			}
		}
	}

	private void LocateButton_Click(object sender, RoutedEventArgs e)
	{
		string? path = GameLocator.FindBO3();
		if (!string.IsNullOrWhiteSpace(path))
		{
			BO3PathTB.Text = path;
			SaveGamePath(path);
		}
		else
		{
			MessageBox.Show("Could not locate Black Ops III installation. Please locate manually.");
		}
	}

	private bool ValidateBO3Path(string path, bool showErrors)
	{
		if (!Directory.Exists(path))
		{
			if (showErrors)
			{
				MessageBox.Show("The selected folder does not exist.");
			}
			return false;
		}
		if (!File.Exists(Path.Combine(path, "BlackOps3.exe")))
		{
			if (showErrors)
			{
				MessageBox.Show("This does not appear to be a Black Ops III installation.");
			}
			return false;
		}
		return true;
	}

	private void ContinueButton_Click(object sender, RoutedEventArgs e)
	{
		if (!HashUpdater.CanProceed)
		{
			MessageBox.Show("Verified executable metadata is missing.\n\nPlease connect to the internet and reopen the app so it can fetch the hashes first.");
			return;
		}
		List<string> missing = new List<string>();
		if (HashUpdater.IsValid(HashUpdater.GetHashes()) && !FileManager.CheckStockBO3())
		{
			missing.Add("Stock BO3 executable");
		}
		if (FileManager.CheckClient("Ezz BOIII") && !FileManager.CheckOldExe())
		{
			missing.Add("Old BO3 executable (required for Ezz BOIII)");
		}
		if (missing.Count > 0)
		{
			MessageBox.Show("Missing required files:\n\n" + string.Join("\n", missing));
			return;
		}
		config.HasCompletedSetup = true;
		ConfigManager.Save(config);
		base.NavigationService.Navigate(new HomePage());
	}

	private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Filter = "ZIP archive (*.zip)|*.zip"
		};
		if (dialog.ShowDialog() == true)
		{
			string gamePath = ValidateBO3Path(BO3PathTB.Text, showErrors: false) ? BO3PathTB.Text : config.GamePath;
			if (BackupManager.RestoreFromZip(dialog.FileName, gamePath, out string error))
			{
				MessageBox.Show("Latest backup restored successfully.", "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				UpdateClientStatus();
			}
			else
			{
				MessageBox.Show(error, "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7 Hub", "Standby");
		Directory.CreateDirectory(path);
		Process.Start("explorer.exe", path);
	}
}
