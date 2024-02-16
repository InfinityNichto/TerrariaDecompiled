using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework;

internal class GameClock
{
	private long baseRealTime;

	private long lastRealTime;

	private bool lastRealTimeValid;

	private int suspendCount;

	private long suspendStartTime;

	private long timeLostToSuspension;

	private long lastRealTimeCandidate;

	private TimeSpan currentTimeOffset;

	private TimeSpan currentTimeBase;

	private TimeSpan elapsedTime;

	private TimeSpan elapsedAdjustedTime;

	internal TimeSpan CurrentTime => currentTimeBase + currentTimeOffset;

	internal TimeSpan ElapsedTime => elapsedTime;

	internal TimeSpan ElapsedAdjustedTime => elapsedAdjustedTime;

	internal static long Counter => Stopwatch.GetTimestamp();

	internal static long Frequency => Stopwatch.Frequency;

	public GameClock()
	{
		Reset();
	}

	internal void Reset()
	{
		currentTimeBase = TimeSpan.Zero;
		currentTimeOffset = TimeSpan.Zero;
		baseRealTime = Counter;
		lastRealTimeValid = false;
	}

	internal void UpdateElapsedTime()
	{
		long counter = Counter;
		if (!lastRealTimeValid)
		{
			lastRealTime = counter;
			lastRealTimeValid = true;
		}
		try
		{
			currentTimeOffset = CounterToTimeSpan(counter - baseRealTime);
		}
		catch (OverflowException)
		{
			currentTimeBase += currentTimeOffset;
			baseRealTime = lastRealTime;
			try
			{
				currentTimeOffset = CounterToTimeSpan(counter - baseRealTime);
			}
			catch (OverflowException)
			{
				baseRealTime = counter;
				currentTimeOffset = TimeSpan.Zero;
			}
		}
		try
		{
			elapsedTime = CounterToTimeSpan(counter - lastRealTime);
		}
		catch (OverflowException)
		{
			elapsedTime = TimeSpan.Zero;
		}
		try
		{
			long num = lastRealTime + timeLostToSuspension;
			elapsedAdjustedTime = CounterToTimeSpan(counter - num);
		}
		catch (OverflowException)
		{
			elapsedAdjustedTime = TimeSpan.Zero;
		}
		lastRealTimeCandidate = counter;
	}

	internal void AdvanceFrameTime()
	{
		lastRealTime = lastRealTimeCandidate;
		timeLostToSuspension = 0L;
	}

	internal void Suspend()
	{
		suspendCount++;
		if (suspendCount == 1)
		{
			suspendStartTime = Counter;
		}
	}

	internal void Resume()
	{
		suspendCount--;
		if (suspendCount <= 0)
		{
			long counter = Counter;
			timeLostToSuspension += counter - suspendStartTime;
			suspendStartTime = 0L;
		}
	}

	private static TimeSpan CounterToTimeSpan(long delta)
	{
		long num = 10000000L;
		long value = checked(delta * num) / Frequency;
		return TimeSpan.FromTicks(value);
	}
}
