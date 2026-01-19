using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ProjectAFS.Core.Models.Startup;

namespace ProjectAFS.Core.UI;

public sealed partial class WinSplash : Window
{
	public WinSplash()
	{
		InitializeComponent();
	}
	
	
	public void UpdateProgress(StartupProgressReport report)
	{
		
	}
}