using System.ComponentModel;

namespace ProjectAFS.Core.Models.Projects;

[Flags]
public enum ProjectPlatformType
{
	Unknown = 0,
	[Description("Android")] Android = 1 << 0,
	[Description("iOS")] IOS = 1 << 1,
	[Description("Desktop")] Desktop = 1 << 2,
	[Description("Client")] Client = Android | IOS,
	[Description("Server")] Server = Desktop,
	All = Client | Server
}