using Avalonia.Threading;
using static ProjectAFS.Launcher.Desktop.Utils.SafeAvaloniaSyncContext;

namespace ProjectAFS.Launcher.Desktop.Utils;

/// <summary>
/// Represents a dispatcher for switching between the main Avalonia UI thread and thread pool threads.
/// </summary>
public static class AFSDispatcher
{
	public static bool EnableAllExceptionWrappers { get; set; } = true;
	public static bool IsInitialized => _dispatcher != null;
	public static bool IsMainThread => IsInitialized && _dispatcher!.CheckAccess();
	public static event EventHandler<SafeSyncContextExceptionEventArgs>? UnhandledException;
	public static event EventHandler<SafeSyncContextExceptionEventArgs>? UnhandledFatalException;
	
	private static Dispatcher? _dispatcher;
	private static SafeAvaloniaSyncContext _ctx = null!;

	/// <summary>
	/// Initialize the <see cref="AFSDispatcher"/> with the given Avalonia <see cref="Dispatcher"/>, representing the main UI thread.
 	/// </summary>
	/// <param name="dispatcher">The Avalonia Dispatcher to use. If null, will try to use <see cref="Dispatcher.UIThread"/>.</param>
	/// <exception cref="InvalidOperationException">Thrown if no Avalonia Dispatcher is available.</exception>
	public static void Init(Dispatcher? dispatcher = null)
	{
		_dispatcher = dispatcher ?? Dispatcher.UIThread ?? throw new InvalidOperationException("No Avalonia Dispatcher available to initialize AFSDispatcher.");
		_ctx = new SafeAvaloniaSyncContext(_dispatcher);
		_ctx.UnhandledException += (s, e) => UnhandledException?.Invoke(s, e);
		SynchronizationContext.SetSynchronizationContext(_ctx);
		if (EnableAllExceptionWrappers)
		{
			EnableAdvancedExceptionWrapping();
		}
	}
	private static void EnableAdvancedExceptionWrapping()
	{
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			// Forward to the SafeAvaloniaSyncContext unhandled exception event.
			UnhandledException?.Invoke(s, new SafeSyncContextExceptionEventArgs(e.ExceptionObject as Exception ?? new Exception("Unknown exception in AppDomain.CurrentDomain.UnhandledException")));
			if (e.IsTerminating)
			{
				UnhandledFatalException?.Invoke(s, new SafeSyncContextExceptionEventArgs(e.ExceptionObject as Exception ?? new Exception("Unknown fatal exception in AppDomain.CurrentDomain.UnhandledException")));
			}
		};
		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			// Forward to the SafeAvaloniaSyncContext unhandled exception event.
			UnhandledException?.Invoke(s, new SafeSyncContextExceptionEventArgs(e.Exception));
			e.SetObserved();
		};
	}

	/// <summary>
	/// Switch to the main Avalonia UI thread.
	/// </summary>
	/// <returns>>A <see cref="ValueTask"/> that completes when the switch is done.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the dispatcher is not initialized.</exception>
	public static ValueTask SwitchToMainThread()
	{
		if (!IsInitialized)
		{
			throw new InvalidOperationException("AFSDispatcher is not initialized! Did you call AFSDispatcher.Init() on App.OnFrameworkInitializationCompleted() before calling this method?");
		}
		if (IsMainThread)
		{
			return ValueTask.CompletedTask;
		}
		
		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_dispatcher!.Post(() => tcs.TrySetResult(null), DispatcherPriority.Background);
		return new ValueTask(tcs.Task);
	}

	/// <summary>
	/// Switch to a thread pool thread.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that completes when the switch is done.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the dispatcher is not initialized.</exception>
	public static ValueTask SwitchToThreadPool()
	{
		if (!IsInitialized)
		{
			throw new InvalidOperationException("AFSDispatcher is not initialized! Did you call AFSDispatcher.Init() on App.OnFrameworkInitializationCompleted() before calling this method?");
		}

		if (!IsMainThread)
		{
			return ValueTask.CompletedTask;
		}
		
		return new ValueTask(Task.Run(async () => await Task.Yield()));
	}
	
	/// <summary>
	/// Switch to a thread pool thread.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that completes when the switch is done.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the dispatcher is not initialized.</exception>
	[Obsolete("This method is deprecated, please use SwitchToThreadPool() instead.")]
	public static ValueTask SwitchToTaskPool() => SwitchToThreadPool();
	
	/// <summary>
	/// Invoke an action on the main Avalonia UI thread.
	/// </summary>
	/// <param name="action">The action to invoke.</param>
	/// <exception cref="InvalidOperationException">Thrown if the dispatcher is not initialized.</exception>
	[Obsolete("A much more modern alternative is to use 'await AFSDispatcher.SwitchToMainThread();' instead, which also works in async methods.")]
	public static void Invoke(Action action)
	{
		if (!IsInitialized)
		{
			throw new InvalidOperationException("AFSDispatcher is not initialized! Did you call AFSDispatcher.Init() on App.OnFrameworkInitializationCompleted() before calling this method?");
		}

		if (IsMainThread)
		{
			action();
			return;
		}
		
		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_dispatcher!.Post(() =>
		{
			try
			{
				action();
				tcs.TrySetResult(null);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}, DispatcherPriority.Background);
	}
	
	/// <summary>
	/// Invoke an async function on the main Avalonia UI thread.
	/// </summary>
	/// <param name="func">The async function to invoke.</param>
	/// <exception cref="InvalidOperationException">Thrown if the dispatcher is not initialized.</exception>
	[Obsolete("A much more modern alternative is to use 'await AFSDispatcher.SwitchToMainThread();' instead, which also works in async methods.")]
	public static async Task InvokeAsync(Func<Task> func)
	{
		if (!IsInitialized)
		{
			throw new InvalidOperationException("AFSDispatcher is not initialized! Did you call AFSDispatcher.Init() on App.OnFrameworkInitializationCompleted() before calling this method?");
		}

		if (IsMainThread)
		{
			await func();
			return;
		}
		
		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_dispatcher!.Post(async () =>
		{
			try
			{
				await func();
				tcs.TrySetResult(null);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}, DispatcherPriority.Background);
		
		await tcs.Task;
	}
}