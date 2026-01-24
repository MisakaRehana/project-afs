using Microsoft.Build.Evaluation;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Projects;

namespace ProjectAFS.Core.Abstracts.Projects;

public interface IProjectTemplate
{
	ILocalizedString Name { get; }
	ILocalizedString Description { get; }
	string Identifier { get; }
	IEnumerable<ProjectPlatformType> SupportedPlatforms { get; }
	
	ProjectCreationContext GetDefaultCreationContext();

	IProject CreateNewProject(ProjectCreationContext context);
}