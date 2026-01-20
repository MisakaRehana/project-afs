using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.AFSChan;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.AFSChan;

/// <summary>
/// AFS-Chan -- The virtual cute assistant of project-afs. (<b>Must be started</b> before <see cref="II18nService" /> to apply override action patches)
/// </summary>
public sealed class AFSChanService : IHostedService, IAFSChanService
{
	public bool IsAFSChanEnabled => FetchIfAFSChanEnabled();
	private readonly ILogger<IAFSChanService> _logger;
	private readonly IAFSConfiguration _config;
	private (Harmony, MethodInfo)? _patch;
	
	public AFSChanService(ILogger<IAFSChanService> logger, IAFSConfiguration config) // Dependency Injection
	{
		_logger = logger;
		_config = config;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_patch = EnableAFSChanPatching();
		await AFSTask.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		DisableAFSChanPatching(); // through this method called, it is required to restart the whole project-afs IDE to completely disable AFS-Chan features.
		await AFSTask.CompletedTask;
	}

	private bool FetchIfAFSChanEnabled()
	{
		return Convert.ToBoolean(_config.GetSection("Core.UI.AFSChan").TryGetValue("Enabled", "false"));
	}

	public (Harmony, MethodInfo)? EnableAFSChanPatching()
	{
		if (!IsAFSChanEnabled) return null;
		var harmony = new Harmony("moe.misakacastle.projectafs.core.ui.afschan");
		harmony.Patch(original: AccessTools.Method(typeof(IValue), nameof(IValue.ToString)),
			prefix: new HarmonyMethod(typeof(AFSChanService), nameof(ToStringPrefix)),
			postfix: null);
		_logger.LogInformation("AFS-Chan patch enabled: IValue.ToString() is patched to support ValueCute.");
		return _patch = (harmony, AccessTools.Method(typeof(IValue), nameof(IValue.ToString)));
	}

	public void DisableAFSChanPatching()
	{
		if (!_patch.HasValue) return;
		var harmony = _patch.Value.Item1;
		var method = _patch.Value.Item2;
		harmony.Unpatch(method, HarmonyPatchType.Prefix, harmony.Id);
		_logger.LogInformation("AFS-Chan patch disabled: IValue.ToString() is restored to its original behavior.");
	}
	
	private bool ToStringPrefix(IValue __instance, ref string __result)
	{
		if (IsAFSChanEnabled && !string.IsNullOrEmpty(__instance.ValueCute))
		{
			__result = __instance.ValueCute!;
			return false; // skip original method
		}
		return true; // continue to original method (__result = __instance.Value)
	}
}