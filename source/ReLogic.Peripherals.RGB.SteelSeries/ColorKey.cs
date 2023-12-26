using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace ReLogic.Peripherals.RGB.SteelSeries;

internal class ColorKey
{
	private const int TimesToDenyIdenticalColors = 30;

	public string EventName;

	public string TriggerName;

	private Color _colorToShow;

	private bool _needsToSendMessage;

	public bool IsVisible;

	private int _timesDeniedColorRepeats;

	public void UpdateColor(Color color, bool isVisible)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		IsVisible = isVisible;
		if (_colorToShow == color && _timesDeniedColorRepeats < 30)
		{
			_timesDeniedColorRepeats++;
			return;
		}
		_timesDeniedColorRepeats = 0;
		_colorToShow = color;
		_needsToSendMessage = true;
	}

	public JObject TryGettingRequest()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		if (!_needsToSendMessage)
		{
			return null;
		}
		_needsToSendMessage = false;
		JObject jObject = new JObject();
		jObject.Add("red", JToken.op_Implicit(((Color)(ref _colorToShow)).R));
		jObject.Add("green", JToken.op_Implicit(((Color)(ref _colorToShow)).G));
		jObject.Add("blue", JToken.op_Implicit(((Color)(ref _colorToShow)).B));
		JObject jObject2 = new JObject();
		jObject2.Add(TriggerName, (JToken)(object)jObject);
		JObject jObject3 = new JObject();
		jObject3.Add("frame", (JToken)(object)jObject2);
		JObject val = new JObject();
		val.Add("event", JToken.op_Implicit(EventName));
		val.Add("data", (JToken)(object)jObject3);
		return val;
	}
}
