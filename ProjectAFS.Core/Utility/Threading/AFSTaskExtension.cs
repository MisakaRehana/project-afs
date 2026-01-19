namespace ProjectAFS.Core.Utility.Threading;

public static class AFSTaskExtension
{
	public static AFSTask AsAFSTask(this Task task)
	{
		return new AFSTask(task);
	}
	
	public static AFSTask AsAFSTask(this ValueTask valueTask)
	{
		return new AFSTask(valueTask.AsTask());
	}
	
	public static AFSTask<T> AsAFSTask<T>(this Task<T> task)
	{
		return new AFSTask<T>(task);
	}
	
	public static AFSTask<T> AsAFSTask<T>(this ValueTask<T> valueTask)
	{
		return new AFSTask<T>(valueTask.AsTask());
	}
	
	public static async AFSTask ContinueWith(this AFSTask task, Action<AFSTask> continuation)
	{
		await task;
		continuation(task);
	}

	public static async AFSTask<TResult> ContinueWith<T, TResult>(this AFSTask<T> task, Func<T, TResult> continuation)
	{
		var result = await task;
		return continuation(result);
	}
}