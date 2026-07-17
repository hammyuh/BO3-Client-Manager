using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace T7_Hub;

public partial class SettingsPage : Page, IComponentConnector
{
	private readonly Config config;

	public SettingsPage()
	{
		InitializeComponent();
		config = ConfigManager.Load();
		BO3PathTB.Text = config.GamePath;
		base.Opacity = 0.0;
		base.Loaded += delegate
		{
			BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250L)));
		};
	}

	private bool ValidateBO3Path()
	{
		string path = BO3PathTB.Text;
		if (string.IsNullOrWhiteSpace(path))
		{
			MessageBox.Show("Please select your Black Ops III folder.");
			return false;
		}
		if (!Directory.Exists(path))
		{
			MessageBox.Show("The selected folder does not exist.");
			return false;
		}
		if (!File.Exists(Path.Combine(path, "BlackOps3.exe")))
		{
			MessageBox.Show("This does not appear to be a Black Ops III installation.");
			return false;
		}
		return true;
	}

	private bool SaveSettings()
	{
		if (!ValidateBO3Path())
		{
			return false;
		}
		config.GamePath = BO3PathTB.Text;
		ConfigManager.Save(config);
		return true;
	}

	private void BrowseButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog dialog = new OpenFolderDialog();
		if (dialog.ShowDialog() == true)
		{
			BO3PathTB.Text = dialog.FolderName ?? "";
			if (!ValidateBO3Path())
			{
				BO3PathTB.Text = config.GamePath;
			}
		}
	}

	private void LocateButton_Click(object sender, RoutedEventArgs e)
	{
		string? path = GameLocator.FindBO3();
		if (!string.IsNullOrWhiteSpace(path))
		{
			BO3PathTB.Text = path;
		}
		else
		{
			MessageBox.Show("Could not locate Black Ops III installation. Please locate manually.");
		}
	}

	private void OKButton_Click(object sender, RoutedEventArgs e)
	{
		if (SaveSettings())
		{
			base.NavigationService.Navigate(new HomePage());
		}
	}

	private void ApplyButton_Click(object sender, RoutedEventArgs e)
	{
		if (SaveSettings())
		{
			ApplyButton.IsEnabled = false;
		}
	}

	private void ActivityLogButton_Click(object sender, RoutedEventArgs e)
	{
		base.NavigationService.Navigate(new ActivityLogPage());
	}

	private void ExportBackupButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog dialog = new SaveFileDialog
		{
			Filter = "ZIP archive (*.zip)|*.zip",
			FileName = $"T7 Hub Backup {DateTime.Now:yyyy-MM-dd HH-mm}.zip"
		};
		if (dialog.ShowDialog() == true)
		{
			if (BackupManager.Export(dialog.FileName, out string error))
			{
				MessageBox.Show("Backup exported successfully.", "Export Backup", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				ActivityLogger.Log("Success", "Exported backup to " + dialog.FileName);
			}
			else
			{
				MessageBox.Show("Could not export backup.\n\n" + error, "Export Backup", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Filter = "ZIP archive (*.zip)|*.zip"
		};
		if (dialog.ShowDialog() == true && ValidateBO3Path())
		{
			if (BackupManager.RestoreFromZip(dialog.FileName, config.GamePath, out string error))
			{
				MessageBox.Show("Backup restored successfully.", "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			else
			{
				MessageBox.Show("Could not restore backup.\n\n" + error, "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private void BO3PathTB_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyButton.IsEnabled = config.GamePath != BO3PathTB.Text;
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		base.NavigationService.Navigate(new HomePage());
	}

	private void FactoryResetButton_Click(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("This will delete all stored clients, backups, and T7 Hub configuration.\n\nYour Black Ops III installation will NOT be deleted.\n\nContinue?", "Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
		{
			return;
		}
		string hubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7 Hub");
		try
		{
			if (Directory.Exists(hubPath))
			{
				Directory.Delete(hubPath, recursive: true);
			}
			MessageBox.Show("T7 Hub has been reset. The application will restart.", "Factory Reset Complete", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			string? processPath = Environment.ProcessPath;
			if (!string.IsNullOrWhiteSpace(processPath))
			{
				Process.Start(processPath);
			}
			Application.Current.Shutdown();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Factory reset failed:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}
}
