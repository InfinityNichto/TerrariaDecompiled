using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public interface IGameSenseUpdater
{
	List<JObject> TryGetEventUpdateRequest();
}
