using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public class SteelSeriesEventRelay : IGameSenseUpdater
{
	private List<JObject> _requestList = new List<JObject>();

	private ARgbGameValueTracker _trackedEvent;

	public SteelSeriesEventRelay(ARgbGameValueTracker theEvent)
	{
		_trackedEvent = theEvent;
	}

	public List<JObject> TryGetEventUpdateRequest()
	{
		_requestList.Clear();
		JObject jObject = _trackedEvent.TryGettingRequest();
		if (jObject != null)
		{
			_requestList.Add(jObject);
		}
		return _requestList;
	}
}
