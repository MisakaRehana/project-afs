// ReSharper disable MemberCanBeProtected.Global
using System.Diagnostics;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Abstracts.Services.Storage;
using ProjectAFS.Core.Models.Projects;
using ProjectAFS.Core.Services.Configuration;
using ProjectAFS.Core.Utility.Enumerable;
using ProjectAFS.Core.Utility.Hosting;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;
using ProjectAFS.Shell.Desktop.MVVM.Models.Welcome;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class PgeConfigProjectViewModel : PageViewModelBase
{
	public string FinalPathPreview => GetFinalPathPreview();
	public ProjectCreationActionType Action { get; }
	public bool IsCreateProject => Action.HasFlag(ProjectCreationActionType.CreateProject);
	public bool IsCreateSolution => Action.HasFlag(ProjectCreationActionType.CreateSolution);
	public bool ShowSolutionSettings => IsCreateProject && IsCreateSolution;
	
	private readonly IAFSConfiguration _config;
	private readonly IStorageService _storage;
	private readonly ILogger<PgeConfigProjectViewModel> _logger;
	private readonly II18nService _i18n;

	[ObservableProperty]
	private string _projectNameTitle;
	
	[ObservableProperty]
	private string _templateName;
	
	[ObservableProperty]
	private string[] _templatePlatforms;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(FinalPathPreview))]
	private string _projectName;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(FinalPathPreview))]
	private string _projectLocation;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(FinalPathPreview))]
	private string? _solutionName;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(FinalPathPreview))]
	private bool _placeInSameDirectory;
	
	[ObservableProperty]
	private bool _isValidLocation = true;
	
	private bool _hasUserEditedSolutionName;
	
	public PgeConfigProjectViewModel(IConfigService config, IStorageService storage, ILogger<PgeConfigProjectViewModel> logger, II18nService i18n,
		WelcomeViewModel frame, IProjectTemplate template, ProjectCreationActionType action, string? existingSolutionPath = null) : base(frame)
	{
		_config = config.Configuration;
		_storage = storage;
		_logger = logger;
		_i18n = i18n;
		Action = action;

		_templateName = template.Name.ToPreferredString();
		_templatePlatforms = template.SupportedPlatforms.Select(p => p.GetDescription()).ToArray();

		var context = template.GetDefaultCreationContext();
		context = PopulateContextByState(context, existingSolutionPath);

		if (Design.IsDesignMode)
		{
			_projectNameTitle = IsCreateProject ? "Project Name (&J)" : "Solution Name (&N)";
		}
		else
		{
			_projectNameTitle = IsCreateProject
				? _i18n["welcome.config_project.project_name"].ToString()
				: _i18n["welcome.config_project.solution_name"].ToString();
		}
		
		_projectName = context.ProjectName;
		_projectLocation = context.ProjectLocation;
		_solutionName = context.SolutionName;
		_placeInSameDirectory = context.PlaceNewSolutionInSameDirectory;
		
		_hasUserEditedSolutionName = action.HasFlag(ProjectCreationActionType.UseExistingSolution); // if adding to existing solution, consider solution name as locked
	}

	partial void OnProjectNameChanged(string value)
	{
		if (Action == ProjectCreationActionType.ActCreateEmptySolution || !_hasUserEditedSolutionName)
		{
			SolutionName = value;
		}
		UpdateNextEnablement();
	}
	
	partial void OnSolutionNameChanged(string? value)
	{
		if (_hasUserEditedSolutionName) return;
		if (value != ProjectName)
		{
			_hasUserEditedSolutionName = true;
		}
		UpdateNextEnablement();
	}

	private string GetFinalPathPreview()
	{
		if (string.IsNullOrWhiteSpace(ProjectLocation))
		{
			IsValidLocation = false;
			return string.Empty;
		}

		string templateKey;
		bool isSolutionOnly = Action == ProjectCreationActionType.ActCreateEmptySolution;

		if (Design.IsDesignMode)
		{
			templateKey = "Project will be created in \"{location}\"";
		}
		else
		{
			templateKey = isSolutionOnly ? "welcome.config_project.create_hint.solution" : "welcome.config_project.create_hint";
			templateKey = _i18n[templateKey].ToString();
		}

		try
		{
			string preview;
			if (Action.HasFlag(ProjectCreationActionType.UseExistingSolution))
			{
				preview = Path.Combine(ProjectLocation, ProjectName);
			}
			else if (PlaceInSameDirectory || isSolutionOnly)
			{
				preview = Path.Combine(ProjectLocation, isSolutionOnly ? SolutionName! : ProjectName);
			}
			else
			{
				preview = Path.Combine(ProjectLocation, SolutionName ?? ProjectName, ProjectName);
			}

			IsValidLocation = true;
			return StringExtension.AdvancedFormat(templateKey, preview);
		}
		catch
		{
			IsValidLocation = false;
			return StringExtension.AdvancedFormat(templateKey, string.Empty);
		}
	}

	private ProjectCreationContext PopulateContextByState(ProjectCreationContext source, string? solutionPath)
	{
		string defProjectLocation = _config.GetSection("Shell.Projects").TryGetValue("DefaultProjectLocation",
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Misaka Castle", "ProjectAFS Projects"));
		string resolvedBaseLocation = source.ProjectLocation.Replace("{DefaultProjectLocation}", defProjectLocation);

		if (Action.HasFlag(ProjectCreationActionType.UseExistingSolution) && !string.IsNullOrEmpty(solutionPath))
		{
			resolvedBaseLocation = Path.GetDirectoryName(solutionPath) ?? resolvedBaseLocation;
		}

		bool isCreatingSolution = Action.HasFlag(ProjectCreationActionType.CreateSolution);
		bool isCreatingProject = Action.HasFlag(ProjectCreationActionType.CreateProject);

		string finalPName = source.ProjectName;
		string finalSName = source.SolutionName ?? finalPName;
		int counter = 1;
		while (true)
		{
			string numStr = counter.ToString();
			string testPName = finalPName.Replace("{Number}", numStr);
			string testSName = finalSName.Replace("{Number}", numStr);
			
			string checkPath = resolvedBaseLocation;
			if (isCreatingSolution && !source.PlaceNewSolutionInSameDirectory)
			{
				checkPath = Path.Combine(checkPath, testSName);
			}

			if (isCreatingProject)
			{
				checkPath = Path.Combine(checkPath, testPName);
			}

			if (!Directory.Exists(checkPath))
			{
				finalPName = testPName;
				finalSName = testSName;
				break;
			}

			if (++counter > 999)
			{
				throw new InvalidOperationException("Unable to find a valid project/solution name after limited attempts.");
			}
		}

		return source with
		{
			ProjectName = isCreatingProject ? finalPName : string.Empty,
			ProjectLocation = resolvedBaseLocation,
			SolutionName = isCreatingSolution ? finalSName : null,
			CreateNewSolution = isCreatingSolution,
			PlaceNewSolutionInSameDirectory = isCreatingSolution && source.PlaceNewSolutionInSameDirectory
		};
	}
	
	[RelayCommand]
	protected virtual async Task BrowseLocation()
	{
		var result = await _storage.SelectFolderAsync(ProjectLocation, "Select Project Location");
		foreach (string location in result)
		{
			ProjectLocation = location;
			UpdateNextEnablement();
			break;
		}
	}

	private void UpdateNextEnablement()
	{
		bool projectValid = !Action.HasFlag(ProjectCreationActionType.CreateProject) || !string.IsNullOrWhiteSpace(ProjectName);
		bool solutionValid = !Action.HasFlag(ProjectCreationActionType.CreateSolution) || !string.IsNullOrWhiteSpace(SolutionName);
		
		CanGoNext = IsValidLocation && projectValid && solutionValid;
		NextCommand.NotifyCanExecuteChanged();
	}

	protected override void Prev()
	{
		NavigateToPage<PgeNewProjectViewModel>();
	}

	protected override void Next()
	{
		if (!IsValidLocation) return;
		_logger.LogDebug("Ready to create project '{ProjectName}' at location '{ProjectLocation}' (Solution: '{SolutionName}', PlaceInSameDirectory: {PlaceInSameDirectory})" +
		                 "with template '{TemplateName}'", ProjectName, ProjectLocation, SolutionName, PlaceInSameDirectory, TemplateName);
	}
}

public sealed class DesignPgeConfigProjectViewModel : PgeConfigProjectViewModel
{
	private readonly static DesignProjectTemplate _template = new("template1", "Standard Fanmade Project", "A project template for creating standard Arc fanmade game that can run on Android and iOS.",
		ProjectPlatformType.Client, ProjectPlatformType.Android, ProjectPlatformType.IOS);
	private readonly static ILogger<DesignPgeConfigProjectViewModel> _dummyLogger = LoggerExtension.CreateDummyLogger<DesignPgeConfigProjectViewModel>();
	
	public DesignPgeConfigProjectViewModel() : base(new DesignConfigService(), null!, _dummyLogger, null!, null!, _template, ProjectCreationActionType.ActCreateProjectAndSolution)
	{
	}

	protected override async Task BrowseLocation()
	{
		Debug.WriteLine("[Design Mode] No operation for BrowseLocation command as there is no IStorageService available in design-time.");
		await Task.CompletedTask;
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