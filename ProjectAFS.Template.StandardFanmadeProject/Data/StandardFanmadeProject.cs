using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProjectAFS.Core.Models.Projects;
using ProjectAFS.Core.Utility.Xml;

namespace ProjectAFS.Template.StandardFanmadeProject.Data;

public sealed class StandardFanmadeProject : AFSBaseProject
{
	public override string ProjectPath { get; }
	public override IEnumerable<ProjectPlatformType> Platforms { get; }
	public override bool IsBuildable => true;
	
	public StandardFanmadeProject(string name, string projectPath, IEnumerable<ProjectPlatformType> platforms)
	{
		Name = name;
		ProjectPath = projectPath;
		Platforms = platforms;
	}
	
	public void GenerateIDEStructure(ProjectCreationContext context, string safeProjectName)
	{
		string projectFolder = Path.GetDirectoryName(ProjectPath)
			?? throw new InvalidOperationException("Failed to determine project folder.");
		Directory.CreateDirectory(projectFolder);
		using var fs = new FileStream(ProjectPath, FileMode.Create, FileAccess.Write);
		fs.Write(new UTF8Encoding(false).GetBytes(XmlConvert.SerializeObject(this)));
		fs.Close();
	}
}