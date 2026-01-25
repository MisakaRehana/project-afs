using System.Runtime.CompilerServices;

namespace ProjectAFS.Core.Utility.Threading;

/// <summary>
/// Represents an awaitable task that runs on Avalonia's synchronization context.
/// </summary>
[AsyncMethodBuilder(typeof(AFSTaskMethodBuilder))] // make available to 'async AFSTask'
public readonly partial struct AFSTask
{
	public static AFSTask CompletedTask => new(Task.CompletedTask);
	private readonly Task _task;

	internal AFSTask(Task task)
	{
		_task = task;
	}

	public AFSTaskAwaiter GetAwaiter() // make available to 'await AFSTask'
	{
		return new AFSTaskAwaiter(_task);
	}
	
	/// <summary>
	/// Converts an <see cref="AFSTask"/> to its underlying <see cref="Task"/>.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask"/> to convert.</param>
	/// <returns>The underlying <see cref="Task"/>.</returns>
	public static implicit operator Task(AFSTask task) => task._task;
	
	/// <summary>
	/// Converts an <see cref="AFSTask"/> to a <see cref="ValueTask"/>.
	/// </summary>
	/// <param name="task">The <see cref="AFSTask"/> to convert.</param>
	/// <returns>A <see cref="ValueTask"/> representing the same operation.</returns>
	public static implicit operator ValueTask(AFSTask task) => new(task._task);

	public readonly struct AFSTaskAwaiter : INotifyCompletion
	{
		public bool IsCompleted => _task.IsCompleted;
		private readonly Task _task;

		public AFSTaskAwaiter(Task task)
		{
			_task = task;
		}

		public void OnCompleted(Action continuation)
		{
			_task.GetAwaiter().OnCompleted(continuation);
		}

		public void GetResult()
		{
			_task.GetAwaiter().GetResult();
		}
	}
	
	
	public static bool operator ==(AFSTask left, AFSTask right) => Equals(left._task, right._task);
	public static bool operator !=(AFSTask left, AFSTask right) => !Equals(left._task, right._task);

	public override bool Equals(object? obj) => obj is AFSTask other && Equals(other);
	public bool Equals(AFSTask other) => Equals(_task, other._task);
	public override int GetHashCode() => _task != null ? _task.GetHashCode() : 0;
}