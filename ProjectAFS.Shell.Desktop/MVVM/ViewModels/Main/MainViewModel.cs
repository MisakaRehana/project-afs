// ReSharper disable MemberCanBeProtected.Global
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Main;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Main;

public partial class MainViewModel : ObservableObject
{
	private readonly AFSApp _app;
	
	[ObservableProperty]
	private IRootDock? _workspaceLayout;
	
	public ObservableCollection<ToolWindowItem> ToolWindows { get; } = [];
	public ObservableCollection<ToolItem> Tools { get; } = [];
	
	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(SaveFileCommand), nameof(SaveFileAsCommand))]
	private bool _canSaveCurrentFile;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(SaveSolutionCommand))]
	private bool _canSaveSolution;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	private bool _canUndo;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	private bool _canRedo;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(CutCommand))]
	private bool _canCut;
	
	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(CopyCommand))]
	private bool _canCopy;
	
	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(PasteCommand))]
	private bool _canPaste;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand), nameof(RebuildSolutionCommand), nameof(CleanSolutionCommand))]
	private bool _canBuildSolution;
	
	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(BuildProjectCommand))]
	private bool _canBuildProject;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(CloseAllDocumentsCommand))]
	private bool _canCloseAllDocuments;
	
	[ActivatorUtilitiesConstructor]
	public MainViewModel(AFSApp app)
	{
		_app = app;
		_statusMessage = "Ready";
	}

	[RelayCommand]
	protected virtual void NewSolution()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanSaveCurrentFile))]
	protected virtual void SaveFile()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanSaveCurrentFile))]
	protected virtual void SaveFileAs()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanSaveSolution))]
	protected virtual void SaveSolution()
	{
		
	}
	
	[RelayCommand]
	protected virtual void OpenSettings()
	{
		
	}
	
	[RelayCommand]
	protected virtual void ExitApplication()
	{
		if (!TryExitApplication()) return;
		_app.Shutdown();
	}
	
	public virtual bool TryExitApplication()
	{
		// TODO: Implement checks for unsaved changes, prompt user, etc.
		return true;
	}
	
	[RelayCommand(CanExecute = nameof(CanUndo))]
	protected virtual void Undo()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanRedo))]
	protected virtual void Redo()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanCut))]
	protected virtual void Cut()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanCopy))]
	protected virtual void Copy()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanPaste))]
	protected virtual void Paste()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanBuildSolution))]
	protected virtual void BuildSolution()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanBuildSolution))]
	protected virtual void RebuildSolution()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanBuildSolution))]
	protected virtual void CleanSolution()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanBuildProject))]
	protected virtual void BuildProject()
	{
		
	}
	
	[RelayCommand(CanExecute = nameof(CanCloseAllDocuments))]
	protected virtual void CloseAllDocuments()
	{
		
	}
	
	[RelayCommand]
	protected virtual void ReportBug()
	{
		
	}
	
	[RelayCommand]
	protected virtual void CheckForUpdates()
	{
		
	}
	
	[RelayCommand]
	protected virtual void ShowAboutDialog()
	{
		
	}
}

public sealed class DesignMainViewModel : MainViewModel
{
	public DesignMainViewModel() : base(app: null!)
	{
		StatusMessage = "Ready";
		CanSaveCurrentFile = true;
		CanSaveSolution = true;
		CanUndo = true;
		CanRedo = true;
		CanCut = true;
		CanCopy = true;
		CanPaste = true;
		CanBuildSolution = true;
		CanBuildProject = true;
		CanCloseAllDocuments = true;
	}

	protected override void NewSolution() { }
	protected override void SaveFile() { }
	protected override void SaveFileAs() { }
	protected override void SaveSolution() { }
	protected override void OpenSettings() { }
	protected override void ExitApplication() { }
	public override bool TryExitApplication() => true;
	protected override void Undo() { }
	protected override void Redo() { }
	protected override void Cut() { }
	protected override void Copy() { }
	protected override void Paste() { }
	protected override void BuildSolution() { }
	protected override void RebuildSolution() { }
	protected override void CleanSolution() { }
	protected override void BuildProject() { }
	protected override void CloseAllDocuments() { }
	protected override void ReportBug() { }
	protected override void CheckForUpdates() { }
	protected override void ShowAboutDialog() { }
}