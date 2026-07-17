using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace T7_Hub;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly Config config;

	private readonly DispatcherTimer hashRetryTimer;

	private bool appStarted;

	private bool hashRetryNotified;

	public MainWindow()
	{
		InitializeComponent();
		VersionLabel.Text = "Release v" + (typeof(MainWindow).Assembly.GetName().Version?.ToString(2) ?? "1.2");
		FileManager.Load();
		FileManager.CreateStandbyFolders();
		config = ConfigManager.Load();
		hashRetryTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1L)
		};
		hashRetryTimer.Tick += HashRetryTimer_Tick;
		base.Loaded += MainWindow_Loaded;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		HashUpdater.PrimeSessionState();
		bool hashesUpdated = await HashUpdater.UpdateHashes();
		if (string.IsNullOrWhiteSpace(config.GamePath))
		{
			string gamePath = GameLocator.FindBO3();
			if (gamePath != null)
			{
				config.GamePath = gamePath;
				ConfigManager.Save(config);
			}
		}
		if (!HashUpdater.HasCachedHashesAtStartup && !hashesUpdated)
		{
			MessageBox.Show("T7 Hub could not fetch the executable hashes at startup and no cached hashes file exists, so the app must close. Please connect to the internet and relaunch the app.", "Hash Update Failed", MessageBoxButton.OK, MessageBoxImage.Hand);
			Application.Current.Shutdown();
			return;
		}
		if (HashUpdater.HasCachedHashesAtStartup && !hashesUpdated)
		{
			hashRetryTimer.Start();
			ActivityLogger.Log("Warning", "Could not fetch the latest executable hashes from GitHub.");
			MessageBox.Show("T7 Hub could not fetch the latest executable hashes from GitHub right now.\n\nThe app will keep running because a cached hashes file already exists.", "Hash Update Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		StartApp();
	}

	private async void HashRetryTimer_Tick(object? sender, EventArgs e)
	{
		if (await HashUpdater.UpdateHashes())
		{
			hashRetryTimer.Stop();
			if (!hashRetryNotified)
			{
				hashRetryNotified = true;
				ActivityLogger.Log("Success", "Fetched the latest executable hashes after reconnecting.");
				MessageBox.Show("T7 Hub fetched the latest executable hashes after reconnecting to the internet.", "Hash Update Complete", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				StartApp();
			}
		}
	}

	private void StartApp()
	{
		if (!appStarted)
		{
			appStarted = true;
			MainFrame.Navigate(config.HasCompletedSetup ? ((object)new HomePage()) : ((object)new Setup()));
			_ = UpdateManager.CheckAndPromptAsync(this);
		}
	}

	public void SetModalOverlayVisible(bool visible)
	{
		if (visible)
		{
			ModalOverlay.Visibility = Visibility.Visible;
			ModalOverlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180L)));
			return;
		}
		DoubleAnimation fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(140L));
		fadeOut.Completed += delegate
		{
			ModalOverlay.Visibility = Visibility.Collapsed;
		};
		ModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
	}
}
