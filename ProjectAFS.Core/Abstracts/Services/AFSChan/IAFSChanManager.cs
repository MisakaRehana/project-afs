using System.Reflection;
using HarmonyLib;

namespace ProjectAFS.Core.Abstracts.Services.AFSChan;

public interface IAFSChanService
{
	public bool IsAFSChanEnabled { get; }
	
	public (Harmony, MethodInfo)? EnableAFSChanPatching();
	
	public void DisableAFSChanPatching();
}