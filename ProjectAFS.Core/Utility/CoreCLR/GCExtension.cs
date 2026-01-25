namespace ProjectAFS.Core.Utility.CoreCLR;

public static class GCExtension
{
	/// <summary>
	/// Disposes of unused objects by well-known garbage collection paradigm.
	/// </summary>
	public static void AggressiveDispose()
	{
		GC.Collect(generation: GC.MaxGeneration, mode: GCCollectionMode.Forced, blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(generation: GC.MaxGeneration, mode: GCCollectionMode.Forced, blocking: true, compacting: true);
	}
}