using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Threading;

[DebuggerDisplay("Participant Count={ParticipantCount},Participants Remaining={ParticipantsRemaining}")]
public class Barrier : IDisposable
{
	private volatile int _currentTotalCount;

	private long _currentPhase;

	private bool _disposed;

	private readonly ManualResetEventSlim _oddEvent;

	private readonly ManualResetEventSlim _evenEvent;

	private readonly ExecutionContext _ownerThreadContext;

	private static ContextCallback s_invokePostPhaseAction;

	private readonly Action<Barrier> _postPhaseAction;

	private Exception _exception;

	private int _actionCallerID;

	public int ParticipantsRemaining
	{
		get
		{
			int currentTotalCount = _currentTotalCount;
			int num = currentTotalCount & 0x7FFF;
			int num2 = (currentTotalCount & 0x7FFF0000) >> 16;
			return num - num2;
		}
	}

	public int ParticipantCount => _currentTotalCount & 0x7FFF;

	public long CurrentPhaseNumber
	{
		get
		{
			return Volatile.Read(ref _currentPhase);
		}
		internal set
		{
			Volatile.Write(ref _currentPhase, value);
		}
	}

	public Barrier(int participantCount)
		: this(participantCount, null)
	{
	}

	public Barrier(int participantCount, Action<Barrier>? postPhaseAction)
	{
		if (participantCount < 0 || participantCount > 32767)
		{
			throw new ArgumentOutOfRangeException("participantCount", participantCount, System.SR.Barrier_ctor_ArgumentOutOfRange);
		}
		_currentTotalCount = participantCount;
		_postPhaseAction = postPhaseAction;
		_oddEvent = new ManualResetEventSlim(initialState: true);
		_evenEvent = new ManualResetEventSlim(initialState: false);
		if (postPhaseAction != null)
		{
			_ownerThreadContext = ExecutionContext.Capture();
		}
		_actionCallerID = 0;
	}

	private void GetCurrentTotal(int currentTotal, out int current, out int total, out bool sense)
	{
		total = currentTotal & 0x7FFF;
		current = (currentTotal & 0x7FFF0000) >> 16;
		sense = (currentTotal & int.MinValue) == 0;
	}

	private bool SetCurrentTotal(int currentTotal, int current, int total, bool sense)
	{
		int num = (current << 16) | total;
		if (!sense)
		{
			num |= int.MinValue;
		}
		return Interlocked.CompareExchange(ref _currentTotalCount, num, currentTotal) == currentTotal;
	}

	[UnsupportedOSPlatform("browser")]
	public long AddParticipant()
	{
		try
		{
			return AddParticipants(1);
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new InvalidOperationException(System.SR.Barrier_AddParticipants_Overflow_ArgumentOutOfRange);
		}
	}

	[UnsupportedOSPlatform("browser")]
	public long AddParticipants(int participantCount)
	{
		ThrowIfDisposed();
		if (participantCount < 1)
		{
			throw new ArgumentOutOfRangeException("participantCount", participantCount, System.SR.Barrier_AddParticipants_NonPositive_ArgumentOutOfRange);
		}
		if (participantCount > 32767)
		{
			throw new ArgumentOutOfRangeException("participantCount", System.SR.Barrier_AddParticipants_Overflow_ArgumentOutOfRange);
		}
		if (_actionCallerID != 0 && Environment.CurrentManagedThreadId == _actionCallerID)
		{
			throw new InvalidOperationException(System.SR.Barrier_InvalidOperation_CalledFromPHA);
		}
		SpinWait spinWait = default(SpinWait);
		long num = 0L;
		bool sense;
		while (true)
		{
			int currentTotalCount = _currentTotalCount;
			GetCurrentTotal(currentTotalCount, out var current, out var total, out sense);
			if (participantCount + total > 32767)
			{
				throw new ArgumentOutOfRangeException("participantCount", System.SR.Barrier_AddParticipants_Overflow_ArgumentOutOfRange);
			}
			if (SetCurrentTotal(currentTotalCount, current, total + participantCount, sense))
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
		long currentPhaseNumber = CurrentPhaseNumber;
		num = ((sense != (currentPhaseNumber % 2 == 0)) ? (currentPhaseNumber + 1) : currentPhaseNumber);
		if (num != currentPhaseNumber)
		{
			if (sense)
			{
				_oddEvent.Wait();
			}
			else
			{
				_evenEvent.Wait();
			}
		}
		else if (sense && _evenEvent.IsSet)
		{
			_evenEvent.Reset();
		}
		else if (!sense && _oddEvent.IsSet)
		{
			_oddEvent.Reset();
		}
		return num;
	}

	public void RemoveParticipant()
	{
		RemoveParticipants(1);
	}

	public void RemoveParticipants(int participantCount)
	{
		ThrowIfDisposed();
		if (participantCount < 1)
		{
			throw new ArgumentOutOfRangeException("participantCount", participantCount, System.SR.Barrier_RemoveParticipants_NonPositive_ArgumentOutOfRange);
		}
		if (_actionCallerID != 0 && Environment.CurrentManagedThreadId == _actionCallerID)
		{
			throw new InvalidOperationException(System.SR.Barrier_InvalidOperation_CalledFromPHA);
		}
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int currentTotalCount = _currentTotalCount;
			GetCurrentTotal(currentTotalCount, out var current, out var total, out var sense);
			if (total < participantCount)
			{
				throw new ArgumentOutOfRangeException("participantCount", System.SR.Barrier_RemoveParticipants_ArgumentOutOfRange);
			}
			if (total - participantCount < current)
			{
				throw new InvalidOperationException(System.SR.Barrier_RemoveParticipants_InvalidOperation);
			}
			int num = total - participantCount;
			if (num > 0 && current == num)
			{
				if (SetCurrentTotal(currentTotalCount, 0, total - participantCount, !sense))
				{
					FinishPhase(sense);
					break;
				}
			}
			else if (SetCurrentTotal(currentTotalCount, current, total - participantCount, sense))
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
	}

	[UnsupportedOSPlatform("browser")]
	public void SignalAndWait()
	{
		SignalAndWait(CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public void SignalAndWait(CancellationToken cancellationToken)
	{
		SignalAndWait(-1, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool SignalAndWait(TimeSpan timeout)
	{
		return SignalAndWait(timeout, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool SignalAndWait(TimeSpan timeout, CancellationToken cancellationToken)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, System.SR.Barrier_SignalAndWait_ArgumentOutOfRange);
		}
		return SignalAndWait((int)timeout.TotalMilliseconds, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool SignalAndWait(int millisecondsTimeout)
	{
		return SignalAndWait(millisecondsTimeout, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool SignalAndWait(int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		cancellationToken.ThrowIfCancellationRequested();
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, System.SR.Barrier_SignalAndWait_ArgumentOutOfRange);
		}
		if (_actionCallerID != 0 && Environment.CurrentManagedThreadId == _actionCallerID)
		{
			throw new InvalidOperationException(System.SR.Barrier_InvalidOperation_CalledFromPHA);
		}
		SpinWait spinWait = default(SpinWait);
		int current;
		int total;
		bool sense;
		long currentPhaseNumber;
		while (true)
		{
			int currentTotalCount = _currentTotalCount;
			GetCurrentTotal(currentTotalCount, out current, out total, out sense);
			currentPhaseNumber = CurrentPhaseNumber;
			if (total == 0)
			{
				throw new InvalidOperationException(System.SR.Barrier_SignalAndWait_InvalidOperation_ZeroTotal);
			}
			if (current == 0 && sense != (CurrentPhaseNumber % 2 == 0))
			{
				throw new InvalidOperationException(System.SR.Barrier_SignalAndWait_InvalidOperation_ThreadsExceeded);
			}
			if (current + 1 == total)
			{
				if (SetCurrentTotal(currentTotalCount, 0, total, !sense))
				{
					if (CdsSyncEtwBCLProvider.Log.IsEnabled())
					{
						CdsSyncEtwBCLProvider.Log.Barrier_PhaseFinished(sense, CurrentPhaseNumber);
					}
					FinishPhase(sense);
					return true;
				}
			}
			else if (SetCurrentTotal(currentTotalCount, current + 1, total, sense))
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
		ManualResetEventSlim currentPhaseEvent = (sense ? _evenEvent : _oddEvent);
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag2 = DiscontinuousWait(currentPhaseEvent, millisecondsTimeout, cancellationToken, currentPhaseNumber);
		}
		catch (OperationCanceledException)
		{
			flag = true;
		}
		catch (ObjectDisposedException)
		{
			if (currentPhaseNumber >= CurrentPhaseNumber)
			{
				throw;
			}
			flag2 = true;
		}
		if (!flag2)
		{
			spinWait.Reset();
			while (true)
			{
				int currentTotalCount = _currentTotalCount;
				GetCurrentTotal(currentTotalCount, out current, out total, out var sense2);
				if (currentPhaseNumber < CurrentPhaseNumber || sense != sense2)
				{
					break;
				}
				if (SetCurrentTotal(currentTotalCount, current - 1, total, sense))
				{
					if (flag)
					{
						throw new OperationCanceledException(System.SR.Common_OperationCanceled, cancellationToken);
					}
					return false;
				}
				spinWait.SpinOnce(-1);
			}
			WaitCurrentPhase(currentPhaseEvent, currentPhaseNumber);
		}
		if (_exception != null)
		{
			throw new BarrierPostPhaseException(_exception);
		}
		return true;
	}

	private void FinishPhase(bool observedSense)
	{
		if (_postPhaseAction != null)
		{
			try
			{
				_actionCallerID = Environment.CurrentManagedThreadId;
				if (_ownerThreadContext != null)
				{
					ContextCallback callback = InvokePostPhaseAction;
					ExecutionContext.Run(_ownerThreadContext, callback, this);
				}
				else
				{
					_postPhaseAction(this);
				}
				_exception = null;
				return;
			}
			catch (Exception exception)
			{
				_exception = exception;
				return;
			}
			finally
			{
				_actionCallerID = 0;
				SetResetEvents(observedSense);
				if (_exception != null)
				{
					throw new BarrierPostPhaseException(_exception);
				}
			}
		}
		SetResetEvents(observedSense);
	}

	private static void InvokePostPhaseAction(object obj)
	{
		Barrier barrier = (Barrier)obj;
		barrier._postPhaseAction(barrier);
	}

	private void SetResetEvents(bool observedSense)
	{
		CurrentPhaseNumber++;
		if (observedSense)
		{
			_oddEvent.Reset();
			_evenEvent.Set();
		}
		else
		{
			_evenEvent.Reset();
			_oddEvent.Set();
		}
	}

	private void WaitCurrentPhase(ManualResetEventSlim currentPhaseEvent, long observedPhase)
	{
		SpinWait spinWait = default(SpinWait);
		while (!currentPhaseEvent.IsSet && CurrentPhaseNumber - observedPhase <= 1)
		{
			spinWait.SpinOnce();
		}
	}

	[UnsupportedOSPlatform("browser")]
	private bool DiscontinuousWait(ManualResetEventSlim currentPhaseEvent, int totalTimeout, CancellationToken token, long observedPhase)
	{
		int num = 100;
		int num2 = 10000;
		while (observedPhase == CurrentPhaseNumber)
		{
			int num3 = ((totalTimeout == -1) ? num : Math.Min(num, totalTimeout));
			if (currentPhaseEvent.Wait(num3, token))
			{
				return true;
			}
			if (totalTimeout != -1)
			{
				totalTimeout -= num3;
				if (totalTimeout <= 0)
				{
					return false;
				}
			}
			num = ((num >= num2) ? num2 : Math.Min(num << 1, num2));
		}
		WaitCurrentPhase(currentPhaseEvent, observedPhase);
		return true;
	}

	public void Dispose()
	{
		if (_actionCallerID != 0 && Environment.CurrentManagedThreadId == _actionCallerID)
		{
			throw new InvalidOperationException(System.SR.Barrier_InvalidOperation_CalledFromPHA);
		}
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_oddEvent.Dispose();
				_evenEvent.Dispose();
			}
			_disposed = true;
		}
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Barrier", System.SR.Barrier_Dispose);
		}
	}
}
