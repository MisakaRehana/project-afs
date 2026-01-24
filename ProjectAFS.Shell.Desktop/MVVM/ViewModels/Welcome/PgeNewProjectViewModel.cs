// ReSharper disable MemberCanBeProtected.Global
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Models.Projects;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

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

	public PgeNewProjectViewModel(WelcomeViewModel frame, IPluginService? plugins = null) : base(frame)
	{
		if (plugins != null)
		{
			var projectTemplates = plugins.GetProjectTemplatesFromPlugins(); // from AEF (project-afs Extensibility Framework) upper layer (DIP guaranteed)
			Templates = new ObservableCollection<ProjectTemplateItem>(projectTemplates.Select(t => new ProjectTemplateItem(t)));
		}
	}

	partial void OnSelectedTemplateChanged(ProjectTemplateItem? value)
	{
		CanGoNext = value is not null;
	}
	
	public override void Prev()
	{
		NavigateToPage<PgeHomeViewModel>();
	}
	
	public override void Next()
	{
		if (SelectedTemplate == null) return;
	}
}

public sealed class DesignPgeNewProjectViewModel : PgeNewProjectViewModel
{
	public DesignPgeNewProjectViewModel() : base(null!)
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
}