using System.Collections.Generic;
using System.IO;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Models.Projects;
using ProjectAFS.Core.Utility.Strings;

namespace ProjectAFS.Template.StandardFanmadeProject.Data;

public sealed class StandardFanmadeProjectTemplate : IProjectTemplate
{
	#region Constants
	private const string NameEnglish = "Standard Fanmade Project";
	private const string NameJapanese = "標準的な自作プロジェクト";
	private const string NameCHS = "标准自制项目";
	private const string NameCHT = "標準自製項目";
	private const string DescriptionEnglish = "A project template for creating standard Arc fanmade game that can run on Android and iOS.";
	private const string DescriptionJapanese = "AndroidおよびiOSで動作する標準的なArc自作ゲームを作成するためのプロジェクトテンプレート。";
	private const string DescriptionCHS = "用于创建可在 Android 和 iOS 上运行的标准Arc自制游戏的项目模板。";
	private const string DescriptionCHT = "用於創建可在 Android 和 iOS 上運行的標準Arc自製遊戲的項目模板。";
	#endregion

	public ILocalizedString Name => new LocalizedString()
	{
		English = NameEnglish, Japanese = NameJapanese, ChineseSimplified = NameCHS, ChineseTraditional = NameCHT
	};
	public ILocalizedString Description => new LocalizedString()
	{
		English = DescriptionEnglish, Japanese = DescriptionJapanese, ChineseSimplified = DescriptionCHS, ChineseTraditional = DescriptionCHT
	};
	public string Identifier => "ProjectAFS.Template.StandardFanmadeProject";
	public IEnumerable<ProjectPlatformType> SupportedPlatforms => [ProjectPlatformType.Client, ProjectPlatformType.Android, ProjectPlatformType.IOS];

	public ProjectCreationContext GetDefaultCreationContext()
	{
		return new ProjectCreationContext(
			ProjectName: "FanmadeGame{Number}",
			ProjectLocation: "{DefaultProjectLocation}",
			ExistingIDESolution: null,
			CreateNewSolution: true,
			SolutionName: "FanmadeGame{Number}",
			PlaceNewSolutionInSameDirectory: false);
	}

	public IProject CreateNewProject(ProjectCreationContext context)
	{
		string safeName = context.ProjectName.ToSafeIDString();
		var project = new StandardFanmadeProject(
			name: context.ProjectName,
			projectPath: Path.Combine(context.ProjectLocation, context.ProjectName + ".afsproj"),
			platforms: SupportedPlatforms);
		project.GenerateIDEStructure(context, safeName);

		return project;
	}
}