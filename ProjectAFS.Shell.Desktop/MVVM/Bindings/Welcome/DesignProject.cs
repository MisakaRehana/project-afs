using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Models.Projects;

namespace ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

internal sealed class DesignProject : IProject
{
	public string Name { get; }
	public string ProjectPath { get; }
	public IEnumerable<ProjectPlatformType> Platforms { get; }
	public bool IsBuildable => false;
	
	public DesignProject(string name, string path, params ProjectPlatformType[] platforms)
	{
		Name = name;
		ProjectPath = path;
		Platforms = platforms;
	}
}