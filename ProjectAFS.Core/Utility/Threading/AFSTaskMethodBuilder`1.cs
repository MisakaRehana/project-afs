using System.Runtime.CompilerServices;

namespace ProjectAFS.Core.Utility.Threading;

public struct AFSTaskMethodBuilder<T>
{
	/// <summary>
	/// The return value of <c>async AFSTask&lt;T&gt;</c>.
	/// </summary>
	public AFSTask<T> Task => new(_tcs.Task);
	
	private TaskCompletionSource<T> _tcs;
	
	/// <summary>
	/// Compiler entry point for <c>async AFSTask&lt;T&gt;</c>.
	/// </summary>
	public static AFSTaskMethodBuilder<T> Create()
	{
		return new AFSTaskMethodBuilder<T>()
		{
			_tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously)
		};
	}
	
	public void SetResult(T result)
	{
		_tcs.TrySetResult(result);
	}
	
	public void SetException(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception);
		_tcs.TrySetException(exception);
	}
	
	public void Start<TStateMachine>(ref TStateMachine stateMachine)
		where TStateMachine : IAsyncStateMachine
	{
		stateMachine.MoveNext();
	}
	
	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		// No implementation needed because AFSTask<> is a value type.
	}
	
	/// <summary>
	/// <c>await AFSTask / Task / ValueTask</c>.
	/// </summary>
	public void AwaitOnCompleted<TAwaiter, TStateMachine>(
		ref TAwaiter awaiter,
		ref TStateMachine stateMachine)
		where TAwaiter : INotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		awaiter.OnCompleted(stateMachine.MoveNext);
	}
	
	/// <summary>
	/// <c>await AFSTask / Task / ValueTask</c> with unsafe continuation.
	/// </summary>
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
		ref TAwaiter awaiter,
		ref TStateMachine stateMachine)
		where TAwaiter : ICriticalNotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
	}
}