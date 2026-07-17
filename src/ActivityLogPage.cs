using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace T7_Hub;

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
