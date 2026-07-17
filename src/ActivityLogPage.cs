using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BO3ClientManager;

public partial class ActivityLogPage : Page, IComponentConnector
{
	public ActivityLogPage()
	{
		InitializeComponent();
		RefreshLog();
	}

	private void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		RefreshLog();
	}

	private void BackButton_Click(object sender, RoutedEventArgs e)
	{
		base.NavigationService.Navigate(new SettingsPage());
	}

	private void RefreshLog()
	{
		LogList.ItemsSource = ActivityLogger.ReadRecent();
	}
}
