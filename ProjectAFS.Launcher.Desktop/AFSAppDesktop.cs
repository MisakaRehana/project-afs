using System.Linq;
using Avalonia;

namespace ProjectAFS.Launcher.Desktop;

public static class AFSAppDesktop
{
	public static int RunDesktop(this AppBuilder builder, string[] args)
	{
		builder = builder.UseSkia();
		string[] avaloniaArgs = args.Where(arg => !arg.StartsWith("--H")).ToArray();
		return builder.StartWithClassicDesktopLifetime(avaloniaArgs);
	}
}