using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ProjectAFS.Core.Utility.Threading;

public struct AFSTaskMethodBuilder
{
	/// <summary>
	/// The return value of <c>async AFSTask</c>.
	/// </summary>
	public AFSTask Task => new(_tcs.Task);
	
	private TaskCompletionSource _tcs;

	/// <summary>
	/// Compiler entry point for <c>async AFSTask</c>.
	/// </summary>
	public static AFSTaskMethodBuilder Create()
	{
		return new AFSTaskMethodBuilder()
		{
			_tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
		};
	}

	public void SetResult()
	{
		_tcs.TrySetResult();
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
	
	[UsedImplicitly]
	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		// No implementation needed because AFSTask is a value type.
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