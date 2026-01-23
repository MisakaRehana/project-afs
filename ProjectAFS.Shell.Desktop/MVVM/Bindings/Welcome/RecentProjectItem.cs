using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

public sealed partial class RecentProjectItem : ObservableObject
{
	[ObservableProperty]
	private string _title = string.Empty;
	
	[ObservableProperty]
	private string _solutionFilePath = string.Empty;
}