using ProjectAFS.Core.Models.Bindings.Welcome;

namespace ProjectAFS.Core.Models.ViewModelBindings;

public class WelcomeViewModel
{
	public List<RecentProjectItem> RecentProjects { get; set; } = [];
}

public sealed class DesignWelcomeViewModel : WelcomeViewModel
{
	public DesignWelcomeViewModel()
	{
		RecentProjects =
		[
			new RecentProjectItem() {Title = "Sample Project 1", SolutionFilePath = @"C:\Path\To\SampleProject1.afsln"},
			new RecentProjectItem() {Title = "Sample Project 2", SolutionFilePath = @"C:\Path\To\SampleProject2.afsln"},
			new RecentProjectItem() {Title = "Sample Project 3", SolutionFilePath = @"C:\Path\To\SampleProject3.afsln"},
			new RecentProjectItem() {Title = "Sample Project 4", SolutionFilePath = @"C:\Path\To\SampleProject4.afsln"}
		];
	}
}