using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Utility.Enumerable;

namespace ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

public sealed partial class ProjectTemplateItem : ObservableObject
{
	public IProjectTemplate Template { get; }

	[ObservableProperty] private string name;
	[ObservableProperty] private string description;
	[ObservableProperty] private ObservableCollection<string> platforms;

	public ProjectTemplateItem(IProjectTemplate template)
	{
		name = template.Name.ToPreferredString();
		description = template.Description.ToPreferredString();
		platforms = new ObservableCollection<string>(template.SupportedPlatforms.Select(p => p.GetDescription()));
		Template = template;
	}
}