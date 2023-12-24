using SteelSeries.GameSense;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public interface IGameSenseDevice : IGameSenseUpdater
{
	void CollectEventsToTrack(Bind_Event[] bindEvents, ARgbGameValueTracker[] miscEvents);
}
