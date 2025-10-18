using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ProjectAFS.Core.Utility.Threading; // adjust namespace if necessary

namespace ProjectAFS.Core.Utility.Threading;

/// <summary>
/// Represents a dispatcher for switching between the main Avalonia UI thread and thread pool threads.
/// Safer defaults: does NOT set a global SynchronizationContext by default (call InstallSafeSynchronizationContext() explicitly if needed).
/// </summary>
public static class AFSDispatcher
{
    private static Dispatcher? _dispatcher;
    private static SafeAvaloniaSyncContext? _ctx;
    private static SynchronizationContext? _previousSyncContext;
    private static readonly object _initLock = new();

    public static bool EnableAllExceptionWrappers { get; set; } = true;

    /// <summary>True when AFSDispatcher.Init(...) has been called.</summary>
    public static bool IsInitialized => _dispatcher != null;

    /// <summary>True if the current thread is the main UI thread (Dispatcher access).</summary>
    public static bool IsMainThread => IsInitialized && _dispatcher!.CheckAccess();

    /// <summary>Event for non-fatal unhandled exceptions captured by dispatcher/context wrappers.</summary>
    public static event EventHandler<SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs>? UnhandledException;

    /// <summary>Event for fatal unhandled exceptions (terminating).</summary>
    public static event EventHandler<SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs>? UnhandledFatalException;

    /// <summary>
    /// Initialize the AFSDispatcher with the given Avalonia Dispatcher.
    /// By default this will NOT install a global SynchronizationContext; to install call InstallSafeSynchronizationContext().
    /// </summary>
    /// <param name="dispatcher">Avalonia dispatcher to use. If null, uses Dispatcher.UIThread.</param>
    public static void Init(Dispatcher? dispatcher = null)
    {
        lock (_initLock)
        {
            if (IsInitialized) return;
            _dispatcher = dispatcher ?? Dispatcher.UIThread ?? throw new InvalidOperationException("No Avalonia Dispatcher available to initialize AFSDispatcher.");
            _ctx = new SafeAvaloniaSyncContext(_dispatcher);
            _ctx.UnhandledException += (s, e) => UnhandledException?.Invoke(s, e);

            if (EnableAllExceptionWrappers)
            {
                EnableAdvancedExceptionWrapping();
            }
        }
    }

    /// <summary>
    /// Optionally install the SafeAvaloniaSyncContext as the global SynchronizationContext.
    /// This is an explicit opt-in because replacing the global context can change behavior of 'await'.
    /// </summary>
    public static void InstallSafeSynchronizationContext()
    {
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher not initialized. Call AFSDispatcher.Init(...) first.");

        // If already installed, do nothing
        if (SynchronizationContext.Current == _ctx) return;

        _previousSyncContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_ctx);
    }

    /// <summary>
    /// Uninstall previously installed SafeAvaloniaSyncContext and restore previous SynchronizationContext (if any).
    /// </summary>
    public static void UninstallSafeSynchronizationContext()
    {
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher not initialized.");

        if (SynchronizationContext.Current == _ctx)
        {
            SynchronizationContext.SetSynchronizationContext(_previousSyncContext);
            _previousSyncContext = null;
        }
    }

    private static void EnableAdvancedExceptionWrapping()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            UnhandledException?.Invoke(s, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(e.ExceptionObject as Exception ?? new Exception("Unknown exception in AppDomain.CurrentDomain.UnhandledException")));
            if (e.IsTerminating)
            {
                UnhandledFatalException?.Invoke(s, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(e.ExceptionObject as Exception ?? new Exception("Unknown fatal exception in AppDomain.CurrentDomain.UnhandledException")));
            }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            UnhandledException?.Invoke(s, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(e.Exception));
            e.SetObserved();
        };
    }

    /// <summary>
    /// Switch to the main Avalonia UI thread. Completes when scheduled work on UI thread has run (posted at Background priority).
    /// </summary>
    public static ValueTask SwitchToMainThread()
    {
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher is not initialized! Call AFSDispatcher.Init(...) first.");

        if (IsMainThread) return ValueTask.CompletedTask;

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher!.Post(() => tcs.TrySetResult(null), DispatcherPriority.Background);
        return new ValueTask(tcs.Task);
    }

    /// <summary>
    /// Switch to a thread pool thread. Use when you want code to continue on a non-UI thread.
    /// </summary>
    public static ValueTask SwitchToThreadPool()
    {
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher is not initialized! Call AFSDispatcher.Init(...) first.");

        if (!IsMainThread) return ValueTask.CompletedTask;

        // Kick off a tiny Task on the thread pool; caller can await to ensure continuation runs on thread-pool.
        return new ValueTask(Task.Run(() => { }));
    }

    /// <summary>
    /// Post an action to be executed on the UI thread. Fire-and-forget; exceptions are forwarded to UnhandledException event.
    /// </summary>
    public static void Post(Action action, DispatcherPriority priority)
    {
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher is not initialized! Call AFSDispatcher.Init(...) first.");
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (IsMainThread)
        {
            try { action(); }
            catch (Exception ex) { UnhandledException?.Invoke(null, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(ex)); }
            return;
        }

        _dispatcher!.Post(() =>
        {
            try { action(); }
            catch (Exception ex) { UnhandledException?.Invoke(null, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(ex)); }
        }, priority);
    }

    /// <summary>
    /// Invoke an async function on the main UI thread and await its completion.
    /// </summary>
    public static Task InvokeAsync(Func<Task> func, DispatcherPriority priority)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher is not initialized! Call AFSDispatcher.Init(...) first.");

        if (IsMainThread)
        {
            // Already on UI thread - run directly and return task
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                // If func throws synchronously, forward
                UnhandledException?.Invoke(null, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(ex));
                return Task.FromException(ex);
            }
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher!.Post(async () =>
        {
            try
            {
                await func().ConfigureAwait(false);
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                UnhandledException?.Invoke(null, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(ex));
                tcs.TrySetException(ex);
            }
        }, priority);

        return tcs.Task;
    }

    /// <summary>
    /// Synchronous convenience: invoke an action on UI thread and wait for completion.
    /// WARNING: This blocks the calling thread. Prefer InvokeAsync or Post to avoid deadlocks.
    /// </summary>
    public static void InvokeAndWait(Action action, DispatcherPriority priority)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (!IsInitialized) throw new InvalidOperationException("AFSDispatcher is not initialized! Call AFSDispatcher.Init(...) first.");

        if (IsMainThread)
        {
            action();
            return;
        }

        // Synchronous wait implemented via Task + GetAwaiter().GetResult() — still blocks the caller thread!
        var task = InvokeAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }, priority);

        try
        {
            task.GetAwaiter().GetResult(); // synchronous wait; exceptions will propagate
        }
        catch (Exception ex)
        {
            // Rethrow after forwarding
            UnhandledException?.Invoke(null, new SafeAvaloniaSyncContext.SafeSyncContextExceptionEventArgs(ex));
            throw;
        }
    }
}