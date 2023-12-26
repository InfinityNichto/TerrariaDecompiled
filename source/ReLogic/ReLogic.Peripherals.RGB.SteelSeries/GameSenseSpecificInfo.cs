using System.Collections.Generic;
using SteelSeries.GameSense;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public class GameSenseSpecificInfo
{
	public List<Bind_Event> EventsToBind;

	public List<ARgbGameValueTracker> MiscEvents;
}
