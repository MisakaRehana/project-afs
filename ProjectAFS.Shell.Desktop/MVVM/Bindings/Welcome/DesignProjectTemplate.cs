using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Models.Projects;

namespace ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

internal sealed class DesignProjectTemplate : IProjectTemplate
{
	public ILocalizedString Name { get; }
	public ILocalizedString Description { get; }
	public string Identifier { get; }
	public IEnumerable<ProjectPlatformType> SupportedPlatforms { get; }
	
	public DesignProjectTemplate(string id, string name, string description, params ProjectPlatformType[] platforms)
	{
		Identifier = id;
		Name = new LocalizedString()
		{
			English = name
		};
		Description = new LocalizedString()
		{
			English = description
		};
		SupportedPlatforms = platforms;
	}
	
	public ProjectCreationContext GetDefaultCreationContext()
	{
		return new ProjectCreationContext(
			ProjectName: "DesignProject",
			ProjectLocation: @"C:\path\to\project",
			ExistingIDESolution: null,
			CreateNewSolution: true,
			SolutionName: "DesignSolution",
			PlaceNewSolutionInSameDirectory: false
		);
	}

	public IProject CreateNewProject(ProjectCreationContext context)
	{
		return new DesignProject(
			name: context.ProjectName,
			path: Path.Combine(context.ProjectLocation, context.ProjectName + ".afsproj"),
			platforms: SupportedPlatforms.ToArray());
	}
}