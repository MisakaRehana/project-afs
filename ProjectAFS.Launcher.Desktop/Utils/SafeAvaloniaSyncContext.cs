using Avalonia.Threading;

namespace ProjectAFS.Launcher.Desktop.Utils;

public class SafeAvaloniaSyncContext : SynchronizationContext
{
	private readonly Dispatcher _dispatcher;
	public event EventHandler<SafeSyncContextExceptionEventArgs>? UnhandledException;

	public SafeAvaloniaSyncContext(Dispatcher dispatcher)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
	}

	public override void Post(SendOrPostCallback d, object? state)
	{
        ArgumentNullException.ThrowIfNull(d);
        _dispatcher.Post(() =>
        {
	        try
	        {
		        d(state);
	        }
	        catch (Exception ex)
	        {
		        UnhandledException?.Invoke(this, new SafeSyncContextExceptionEventArgs(ex));
	        }
        }, DispatcherPriority.Background);
	}

	public override void Send(SendOrPostCallback d, object? state)
	{
		ArgumentNullException.ThrowIfNull(d);

		if (_dispatcher.CheckAccess())
		{
			try
			{
				d(state);
			}
			catch (Exception ex)
			{
				UnhandledException?.Invoke(this, new SafeSyncContextExceptionEventArgs(ex));
			}

			return;
		}
		
		var done = new ManualResetEventSlim(false);
		Exception? captured = null;
		_dispatcher.Post(() =>
		{
			try
			{
				d(state);
			}
			catch (Exception ex)
			{
				captured = ex;
			}
			finally
			{
				done.Set();
			}
		}, DispatcherPriority.Send);
		
		done.Wait();
		if (captured != null)
		{
			UnhandledException?.Invoke(this, new SafeSyncContextExceptionEventArgs(captured));
		}
	}

	public class SafeSyncContextExceptionEventArgs : EventArgs
	{
		public Exception Exception { get; }

		public SafeSyncContextExceptionEventArgs(Exception exception)
		{
			Exception = exception ?? throw new ArgumentNullException(nameof(exception));
		}
	}
}