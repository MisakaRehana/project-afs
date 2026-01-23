using System.Windows.Input;

namespace ProjectAFS.Shell.Desktop.MVVM.Bindings.Main;

public sealed class ToolWindowItem
{
	public string Title { get; set; } = string.Empty;
	public ICommand? Command { get; set; }
}