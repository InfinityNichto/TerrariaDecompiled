using System;

namespace ReLogic.Peripherals.RGB;

public class ChromaEngineUpdateEventArgs : EventArgs
{
	public readonly float TimeElapsed;

	public ChromaEngineUpdateEventArgs(float timeElapsed)
	{
		TimeElapsed = timeElapsed;
	}
}
