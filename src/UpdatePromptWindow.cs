using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace T7_Hub;

public partial class UpdatePromptWindow : Window, IComponentConnector
{
	public UpdatePromptWindow(string version)
	{
		InitializeComponent();
		MessageText.Text = "A new version is available: " + version + "\n\nOpen the latest release page now?";
		base.Loaded += delegate
		{
			BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180L)));
		};
	}

	private void OpenRelease_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
		Close();
	}

	private void RemindLater_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}
}
