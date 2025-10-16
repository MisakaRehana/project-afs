using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using ICSharpCode.SharpZipLib.Zip;

namespace ProjectAFS.Core.Internal.Plugins;

public class PluginAssemblyLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver _resolver;
	private readonly string _pluginPath;
	private readonly ZipFile _zipFile;
	
	public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
	{
		_pluginPath = pluginPath;
		_resolver = new AssemblyDependencyResolver(pluginPath);
		_zipFile = new ZipFile(pluginPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		// prefer to load from plugin private dependencies first
		var entryPath = $"lib/{assemblyName.Name}.dll";
		var entry = _zipFile.GetEntry(entryPath);

		if (entry != null)
		{
			using var ms = new MemoryStream();
			_zipFile.GetInputStream(entry).CopyTo(ms);
			ms.Position = 0;
			return LoadFromStream(ms);
		}
		
		return null; // try to resolve via default context (shared libraries)
	}
}