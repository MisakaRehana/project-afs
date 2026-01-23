#pragma warning disable AFS0001 // Avoid direct access or conversion of Application.Current to prevent tight coupling with AFSApp. Use Dependency Injection for type AFSApp to enhance testability and maintainability instead.
using Avalonia;
using Microsoft.Extensions.Logging;

namespace ProjectAFS.Core.Utility.Threading;

/// <summary>
/// Provides extension methods for converting and managing <see cref="AFSTask"/> and <see cref="AFSTask{T}"/> instances.
/// </summary>
public static class AFSTaskExtension
{
	/// <summary>
	/// Converts a <see cref="Task"/> to an <see cref="AFSTask"/>.
	/// </summary>
	/// <param name="task">The <see cref="Task"/> to convert.</param>
	/// <returns>An <see cref="AFSTask"/> representing the same operation.</returns>
	public static AFSTask AsAFSTask(this Task task)
	{
		return new AFSTask(task);
	}
	
	/// <summary>
	/// Converts a <see cref="ValueTask"/> to an <see cref="AFSTask"/>.
	/// </summary>
	/// <param name="valueTask">The <see cref="ValueTask"/> to convert.</param>
	/// <returns>An <see cref="AFSTask"/> representing the same operation.</returns>
	public static AFSTask AsAFSTask(this ValueTask valueTask)
	{
		return new AFSTask(valueTask.AsTask());
	}
	
	/// <summary>
	/// Converts a <see cref="Task{T}"/> with a result to an <see cref="AFSTask{T}"/>.
	/// </summary>
	/// <param name="task">The <see cref="Task{T}"/> to convert.</param>
	/// <typeparam name="T">The type of the result produced by the task.</typeparam>
	/// <returns>An <see cref="AFSTask{T}"/> representing the same operation.</returns>
	public static AFSTask<T> AsAFSTask<T>(this Task<T> task)
	{
		return new AFSTask<T>(task);
	}
	
	/// <summary>
	/// Converts a <see cref="ValueTask{T}"/> with a result to an <see cref="AFSTask{T}"/>.
	/// </summary>
	/// <param name="valueTask">The <see cref="ValueTask{T}"/> to convert.</param>
	/// <typeparam name="T">The type of the result produced by the task.</typeparam>
	/// <returns>An <see cref="AFSTask{T}"/> representing the same operation.</returns>
	public static AFSTask<T> AsAFSTask<T>(this ValueTask<T> valueTask)
	{
		return new AFSTask<T>(valueTask.AsTask());
	}
	
	/// <summary>
	/// Continues an <see cref="AFSTask"/> with a specified action upon its completion.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask"/> to continue.</param>
	/// <param name="continuation">The action to execute upon task completion.</param>
	/// <returns>A new <see cref="AFSTask"/> representing the continuation.</returns>
	public static async AFSTask ContinueWith(this AFSTask task, Action<AFSTask> continuation)
	{
		await task;
		continuation(task);
	}

	/// <summary>
	/// Continues an <see cref="AFSTask{T}"/> with a specified function upon its completion.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask{T}"/> to continue.</param>
	/// <param name="continuation">The function to execute upon task completion.</param>
	/// <typeparam name="T">The type of the result produced by the original task.</typeparam>
	/// <returns>A new <see cref="AFSTask{TResult}"/> representing the continuation.</returns>
	public static async AFSTask<TResult> ContinueWith<T, TResult>(this AFSTask<T> task, Func<T, TResult> continuation)
	{
		var result = await task;
		return continuation(result);
	}

	/// <summary>
	/// Fire-and-forget an <see cref="AFSTask"/>.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask"/> to forget.</param>
	public static async void Forget(this AFSTask task)
	{
		try
		{
			await task;
		}
		catch (Exception ex)
		{
			var logger = (Application.Current as AFSApp)?.FetchService<ILogger<AFSTask>>(); // AFSTaskExtension is a static class and cannot be directly used in generic service fetching
			logger?.LogError(ex, "An unobserved exception occurred in a forgotten AFSTask.");
		}
	}
	
	/// <summary>
	/// Fire-and-forget an <see cref="AFSTask{T}"/> with an optional callback upon completion.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask{T}"/> to forget.</param>
	/// <param name="callback">A callback to invoke with the result upon completion for results reporting.</param>
	/// <typeparam name="T">The type of the result produced by the task.</typeparam>
	public static async void Forget<T>(this AFSTask<T> task, Action<T>? callback = null)
	{
		try
		{
			var result = await task;
			callback?.Invoke(result);
		}
		catch (Exception ex)
		{
			var logger = (Application.Current as AFSApp)?.FetchService<ILogger<AFSTask>>(); // AFSTaskExtension is a static class and cannot be directly used in generic service fetching
			logger?.LogError(ex, "An unobserved exception occurred in a forgotten AFSTask<T>.");
		}
	}
}