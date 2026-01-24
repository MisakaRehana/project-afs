using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Nuke.Common.ProjectModel;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Utility.Xml;

namespace ProjectAFS.Core.Models.Projects;

public record ProjectCreationContext
(
	string ProjectName,
	string ProjectLocation,
	string? SolutionName,
	bool CreateNewSolution, // true will use 'ProjectLocation' as 'SolutionLocation', and ProjectLocation will be changed to '${SolutionLocation}/${ProjectName}'
	Solution? ExistingIDESolution,
	bool PlaceNewSolutionInSameDirectory
);

[Serializable, XmlRoot("Project")]
public abstract class AFSBaseProject : IProject
{
	[XmlAttribute("Sdk")]
	public string Sdk { get; set; } = "ProjectAFS.Sdk.Default"; // this is the SAME as the project template id, default leaving it as ProjectAFS.Sdk.Default
	
	[XmlAnyElement("PropertyGroup")]
	public List<XElement> PropertyGroups { get; set; } = [];

	[XmlIgnore]
	public string Name
	{
		get => GetName();
		set => SetName(value);
	}
	
	[XmlIgnore]
	public abstract string ProjectPath { get; }
	
	[XmlIgnore]
	public abstract IEnumerable<ProjectPlatformType> Platforms { get; }
	
	[XmlIgnore]
	public abstract bool IsBuildable { get; }

	private string GetName()
	{
		return PropertyGroups.SelectMany(pg => pg.Elements("ProjectName"))
			.FirstOrDefault()?.Value ?? "UntitledProject";
	}

	private void SetName(string value)
	{
		var ns = XNamespace.None;
		var element = PropertyGroups.SelectMany(pg => pg.Elements(ns + "ProjectName"))
			.FirstOrDefault();
		if (element != null)
		{
			element.Value = value;
		}
		else
		{
			var pg = PropertyGroups.FirstOrDefault();
			if (pg == null)
			{
				pg = new XElement(ns + "PropertyGroup");
				PropertyGroups.Add(pg);
			}
			
			pg.Add(new XElement(ns + "ProjectName", value));
		}
	}
}