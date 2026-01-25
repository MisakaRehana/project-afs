namespace ProjectAFS.Shell.Desktop.MVVM.Models.Welcome;

[Flags]
public enum ProjectCreationActionType
{
	None = 0,
	
	CreateSolution = 1 << 0,
	CreateProject = 1 << 1,
	UseExistingSolution = 1 << 2,
	
	ActCreateEmptySolution = CreateSolution,
	ActCreateProjectAndSolution = CreateProject | CreateSolution,
	ActAddNewProjectToExistingSolution = CreateProject | UseExistingSolution,
}