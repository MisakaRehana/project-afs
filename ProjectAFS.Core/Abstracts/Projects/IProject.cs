using Avalonia.Controls;
using ProjectAFS.Core.Models.Projects;

namespace ProjectAFS.Core.Abstracts.Projects;

public interface IProject
{
	string Name { get; }
	string ProjectPath { get; }
	IEnumerable<ProjectPlatformType> Platforms { get; }
	
	bool IsBuildable { get; }
}

public interface IProjectTemplateProvider
{
	IEnumerable<IProjectTemplate> GetAvailableProjectTemplates();
}