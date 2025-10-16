using ProjectAFS.Core.Abstractions.Plugins;
using ProjectAFS.Extensibility.Abstractions.Plugins;

namespace ProjectAFS.Core.Internal.Plugins;

public class PluginContext
{
	public PluginInfo Info { get; }
	public string EntryAssembly { get; }
	public PluginAssemblyLoadContext? LoadContext { get; private set; }
	
	public bool IsLoaded => LoadContext != null;
	
	public AFSBasePlugin? Instance { get; set; }
	
	public PluginContext(PluginInfo info, string entryAssembly)
	{
		Info = info;
		EntryAssembly = entryAssembly;
	}

	public void SetStatus(PluginStatus status) => Info.Status = status;
	
	public void SetLoadContext(PluginAssemblyLoadContext alc) => LoadContext = alc;
	
	public void RegisterInstance(AFSBasePlugin instance) => Instance = instance;

	public void Unload()
	{
		LoadContext?.Unload();
		LoadContext = null;
	}
}