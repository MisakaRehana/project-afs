// ReSharper disable MemberCanBeProtected.Global
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Models.Projects;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;
using ProjectAFS.Shell.Desktop.MVVM.Models.Welcome;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class PgeNewProjectViewModel : PageViewModelBase
{
	[ObservableProperty]
	private ObservableCollection<ProjectTemplateItem> recentTemplates = [];
	
	[ObservableProperty]
	private string searchText = string.Empty;
	
	[ObservableProperty]
	private ObservableCollection<ProjectTemplateItem> templates = [];
	
	[ObservableProperty]
	private ProjectTemplateItem? selectedTemplate;

	private readonly bool _isAddProject;

	public PgeNewProjectViewModel(WelcomeViewModel frame, IPluginService? plugins, bool isAddProject = false) : base(frame)
	{
		_isAddProject = isAddProject;
		if (plugins != null)
		{
			var projectTemplates = plugins.GetProjectTemplatesFromPlugins(); // from AEF (project-afs Extensibility Framework) upper layer (DIP guaranteed)
			Templates = new ObservableCollection<ProjectTemplateItem>(projectTemplates.Select(t => new ProjectTemplateItem(t)));
		}
	}

	partial void OnSelectedTemplateChanged(ProjectTemplateItem? value)
	{
		CanGoNext = value is not null;
		
		NextCommand.NotifyCanExecuteChanged();
	}

	protected override void Prev()
	{
		NavigateToPage<PgeHomeViewModel>();
	}

	protected override void Next()
	{
		if (SelectedTemplate == null) return;
		bool isIntegratedSolutionOnly = SelectedTemplate.Template.Identifier == ProjectTemplateIdentifiers.IntegratedEmptySolution;
		var action = isIntegratedSolutionOnly ? ProjectCreationActionType.ActCreateEmptySolution
			: _isAddProject ? ProjectCreationActionType.ActAddNewProjectToExistingSolution
			: ProjectCreationActionType.ActCreateProjectAndSolution;
		NavigateToPage<PgeConfigProjectViewModel>(cache: false, SelectedTemplate.Template, action);
	}
}

public sealed class DesignPgeNewProjectViewModel : PgeNewProjectViewModel
{
	public DesignPgeNewProjectViewModel() : base(null!, null!)
	{
		var designTemplates = new IProjectTemplate[]
		{
			new DesignProjectTemplate("template1", "Fanmade Mobile Project 1", "A sample fanmade game project template.", ProjectPlatformType.Client, ProjectPlatformType.Android, ProjectPlatformType.IOS),
			new DesignProjectTemplate("template2", "Fanmade Server Project 2", "A sample fanmade server project template.", ProjectPlatformType.Server, ProjectPlatformType.Desktop),
			new DesignProjectTemplate("template3", "Standard Fanmade Project", "A project template for creating standard Arc fanmade game that can run on Android and iOS.", ProjectPlatformType.Client, ProjectPlatformType.Server,
				ProjectPlatformType.Android, ProjectPlatformType.IOS, ProjectPlatformType.Desktop)
		};
		Templates = new ObservableCollection<ProjectTemplateItem>(designTemplates.Select(t => new ProjectTemplateItem(t)));
	}
	
	
	protected override void Prev()
	{
		Debug.WriteLine("[Design Mode] No operation for Prev command in design-time.");
	}
	
	protected override void Next()
	{
		Debug.WriteLine("[Design Mode] No operation for Next command in design-time.");
	}
}