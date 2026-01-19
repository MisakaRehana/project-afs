using System.Runtime.CompilerServices;

namespace ProjectAFS.Core.Utility.Threading;

/// <summary>
/// Represents an awaitable task that returns a value and runs on Avalonia's synchronization context.
/// </summary>
/// <typeparam name="T">The type of the result produced by the task.</typeparam>
[AsyncMethodBuilder(typeof(AFSTaskMethodBuilder<>))] // make available to 'async AFSTask<T>'
public readonly struct AFSTask<T>
{
	private readonly Task<T> _task;
	
	internal AFSTask(Task<T> task)
	{
		_task = task;
	}
	
	public AFSTaskAwaiter GetAwaiter() // make available to 'await AFSTask<T>'
	{
		return new AFSTaskAwaiter(_task);
	}
	
	/// <summary>
	/// Converts an generic <see cref="AFSTask"/> to its underlying <see cref="Task"/>.
	/// </summary>
	/// <param name="task">The generic <see cref="AFSTask"/> to convert.</param>
	/// <returns>The underlying generic <see cref="Task"/>.</returns>
	public static implicit operator Task<T>(AFSTask<T> task) => task._task;
	
	/// <summary>
	/// Converts an generic <see cref="AFSTask"/> to a <see cref="ValueTask"/>.
	/// </summary>
	/// <param name="task">The generic <see cref="AFSTask"/> to convert.</param>
	/// <returns>A generic <see cref="ValueTask"/> representing the same operation.</returns>
	public static implicit operator ValueTask<T>(AFSTask<T> task) => new(task._task);
	
	public readonly struct AFSTaskAwaiter : INotifyCompletion
	{
		public bool IsCompleted => _task.IsCompleted;
		private readonly Task<T> _task;
		public AFSTaskAwaiter(Task<T> task)
		{
			_task = task;
		}
		public void OnCompleted(Action continuation)
		{
			_task.GetAwaiter().OnCompleted(continuation);
		}
		public T GetResult()
		{
			return _task.GetAwaiter().GetResult();
		}
	}
}