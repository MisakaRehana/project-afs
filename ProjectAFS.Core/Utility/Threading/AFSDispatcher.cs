using Avalonia.Threading;

namespace ProjectAFS.Core.Utility.Threading;

/// <summary>
/// Represents a dispatcher for switching between the main UI thread and background threads in Avalonia applications.
/// </summary>
public static class AFSDispatcher
{
	public static bool IsMainThread => Dispatcher.UIThread.CheckAccess();

	[Obsolete("Use AFSTask.SwitchToMainThread() instead as it's more efficient and semantically clearer.")]
	public static async AFSTask SwitchToMainThread()
	{
		if (IsMainThread)
		{
			await Task.CompletedTask;
			return;
		}
		
		var tcs = new TaskCompletionSource();
		Dispatcher.UIThread.Post(() =>
		{
			tcs.SetResult();
		});
		
		await tcs.Task;
	}

	[Obsolete("Use AFSTask.SwitchToThreadPool() instead as it's more efficient and semantically clearer.")]
	public static async AFSTask SwitchToThreadPool()
	{
		if (!IsMainThread)
		{
			await Task.CompletedTask;
			return;
		}

		await Task.Run(() => { });
	}

	public static void InvokeOnMainThreadAndWait(Action action)
	{
		if (IsMainThread)
		{
			action();
		}
		else
		{
			var tcs = new TaskCompletionSource();
			Dispatcher.UIThread.InvokeAsync(() =>
			{
				try
				{
					action();
					tcs.SetResult();
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			});
			tcs.Task.GetAwaiter().GetResult();
		}
	}
	
	public static async AFSTask InvokeOnMainThreadAsync(Action action)
	{
		if (IsMainThread)
		{
			action();
			await Task.CompletedTask;
		}
		else
		{
			await Dispatcher.UIThread.InvokeAsync(action);
		}
	}
}