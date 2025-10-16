using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace ProjectAFS.Core.Internal.ViewModels.UI;

public class WinMainViewModel : RootDock
{
	public new IFactory Factory { get; set; }

	public WinMainViewModel()
	{
		Factory = new Factory();

		var root = Factory.CreateRootDock();
		
		var docDock = Factory.CreateDocumentDock();
		var doc = new Document() {Id = "MainDoc", Title = "Main Editor", CanClose = true};
		docDock.VisibleDockables = Factory.CreateList<IDockable>(doc);

		var toolDock = Factory.CreateToolDock();
		var toolbox = new Tool() {Id = "Toolbox", Title = "Toolbox", CanPin = true};
		toolDock.VisibleDockables = Factory.CreateList<IDockable>(toolbox);
	}
}