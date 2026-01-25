namespace ProjectAFS.Core.Utility.Threading;

public readonly partial struct AFSTask // AFSTask Expand Utility -- Provides static methods for creating and managing AFSTask instances.
{
	public static AFSTask Run(Action action)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask(Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler));
	}
	
	public static AFSTask<T> Run<T>(Func<T> function)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask<T>(Task.Factory.StartNew(function, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler));
	}
	
	public static AFSTask Create(Func<AFSTask> factory)
	{
		return factory();
	}
	
	public static AFSTask<T> Create<T>(Func<AFSTask<T>> factory)
	{
		return factory();
	}
	
	public static AFSTask Delay(int millisecondsDelay)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask(Task.Delay(millisecondsDelay).ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, scheduler));
	}
	
	public static AFSTask Delay(TimeSpan delay)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask(Task.Delay(delay).ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, scheduler));
	}
	
	public static AFSTask Delay(int millisecondsDelay, CancellationToken cancellationToken)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask(Task.Delay(millisecondsDelay, cancellationToken).ContinueWith(_ => { }, cancellationToken, TaskContinuationOptions.DenyChildAttach, scheduler));
	}
	
	public static AFSTask Delay(TimeSpan delay, CancellationToken cancellationToken)
	{
		bool isMainThreadTask = AFSDispatcher.IsMainThread;
		var scheduler = isMainThreadTask ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
		return new AFSTask(Task.Delay(delay, cancellationToken).ContinueWith(_ => { }, cancellationToken, TaskContinuationOptions.DenyChildAttach, scheduler));
	}
	
	public static async AFSTask SwitchToMainThread()
	{
#pragma warning disable 618
		await AFSDispatcher.SwitchToMainThread();
#pragma warning restore 618
	}
	
	public static async AFSTask SwitchToThreadPool()
	{
#pragma warning disable 618
		await AFSDispatcher.SwitchToThreadPool();
#pragma warning restore 618
	}
	
	public static async AFSTask WhenAll(IEnumerable<AFSTask> tasks)
	{
		ArgumentNullException.ThrowIfNull(tasks);
		var exceptions = new List<Exception>();

		foreach (var task in tasks)
		{
			try
			{
				await task; // We cannot use Task.WhenAll because AFSTask is not guaranteed to run on the same context.
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		}
		
		if (exceptions.Count > 0)
		{
			throw new AggregateException(exceptions);
		}
	}

	public static async AFSTask WhenAll(params AFSTask[] tasks)
	{
		await WhenAll((IEnumerable<AFSTask>) tasks);
	}
	
	public static async AFSTask<T[]> WhenAll<T>(IEnumerable<AFSTask<T>> tasks)
	{
		ArgumentNullException.ThrowIfNull(tasks);
		var results = new List<T>();
		var exceptions = new List<Exception>();

		foreach (var task in tasks)
		{
			try
			{
				results.Add(await task);
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		}

		if (exceptions.Count > 0)
		{
			throw new AggregateException(exceptions);
		}
		
		return results.ToArray();
	}
	
	public static async AFSTask<T[]> WhenAll<T>(params AFSTask<T>[] tasks)
	{
		return await WhenAll((IEnumerable<AFSTask<T>>) tasks);
	}


	public static async AFSTask<AFSTask> WhenAny(params AFSTask[] tasks)
	{
		return await WhenAny((IEnumerable<AFSTask>) tasks);
	}

	public static async AFSTask<AFSTask> WhenAny(IEnumerable<AFSTask> tasks)
	{
		ArgumentNullException.ThrowIfNull(tasks);
		var taskList = tasks.ToList();
		if (taskList.Count == 0) throw new ArgumentException("The tasks collection must contain at least one task.", nameof(tasks));
		var tcs = new TaskCompletionSource<AFSTask>();
		foreach (var task in taskList)
		{
			_ = SignalCompletion(task, tcs);
		}

		return await tcs.Task;

		static async Task SignalCompletion(AFSTask t, TaskCompletionSource<AFSTask> tcs)
		{
			try
			{
				await (Task) t;
			}
			catch
			{
				// ignore exceptions because we need this Task which is entered Faulted state
			}
			tcs.TrySetResult(t);
		}
	}
	
	public static async AFSTask<AFSTask<T>> WhenAny<T>(params AFSTask<T>[] tasks)
	{
		return await WhenAny((IEnumerable<AFSTask<T>>) tasks);
	}
	
	
	public static async AFSTask<AFSTask<T>> WhenAny<T>(IEnumerable<AFSTask<T>> tasks)
	{
		ArgumentNullException.ThrowIfNull(tasks);
		var taskList = tasks.ToList();
		if (taskList.Count == 0) throw new ArgumentException("The tasks collection must contain at least one task.", nameof(tasks));
		
		var tcs = new TaskCompletionSource<AFSTask<T>>();
		
		foreach (var task in taskList)
		{
			_ = SignalCompletion(task, tcs);
		}
		
		return await tcs.Task;
		
		static async Task SignalCompletion(AFSTask<T> t, TaskCompletionSource<AFSTask<T>> tcs)
		{
			try
			{
				await (Task<T>) t;
			}
			catch
			{
				// ignore exceptions because we need this Task which is entered Faulted state
			}
			tcs.TrySetResult(t);
		}
	}
}