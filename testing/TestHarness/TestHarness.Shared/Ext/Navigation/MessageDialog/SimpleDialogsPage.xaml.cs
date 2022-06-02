﻿
namespace TestHarness.Ext.Navigation.MessageDialog;

public sealed partial class SimpleDialogsPage : Page
{
	public SimpleDialogsPage()
	{
		Console.WriteLine("******* Page ");

		this.InitializeComponent();
	}

	private async void MessageDialogCodebehindClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await this.Navigator()!.ShowMessageDialogAsync<string>(this, content: "This is Content", title: "This is title");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}

	private async void MessageDialogCodebehindRouteClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await this.Navigator()!.ShowMessageDialogAsync<string>(this, route: "LocalizedConfirm");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}
	private async void MessageDialogCodebehindRouteOverrideClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await this.Navigator()!.ShowMessageDialogAsync<string>(this, route: "LocalizedConfirm", content: "Override content", title: "Override title");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}
	private async void MessageDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
	{
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var messageDialogResult = await this.Navigator()!.ShowMessageDialogAsync<string>(this, content: "This is Content", title: "This is title", cancellation: cancelSource.Token);
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}

	public void CloseAllMessageDialogs()//object sender, RoutedEventArgs args)
	{
		var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
		foreach (var popup in popups)
		{
			popup.IsOpen = false;
		}

	}

	private void MessageDialogCloseChecked(object sender, RoutedEventArgs e)
	{
		(sender as ToggleButton)!.IsChecked = false;
		CloseAllMessageDialogs();
	}
}
