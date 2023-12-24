using System;
using Newtonsoft.Json.Linq;

namespace ReLogic.Peripherals.RGB;

public abstract class ARgbGameValueTracker
{
	public string EventName;

	protected bool _needsToSendMessage;

	public bool IsVisible;

	public JObject TryGettingRequest()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if (!_needsToSendMessage)
		{
			return null;
		}
		_needsToSendMessage = false;
		JObject jObject = new JObject();
		WriteValueToData(jObject);
		JObject val = new JObject();
		val.Add("event", JToken.op_Implicit(EventName));
		val.Add("data", (JToken)(object)jObject);
		return val;
	}

	protected abstract void WriteValueToData(JObject data);
}
public abstract class ARgbGameValueTracker<TValueType> : ARgbGameValueTracker where TValueType : IComparable
{
	private const int TimesToDenyIdenticalValues = 30;

	protected TValueType _currentValue;

	private int _timesDeniedRepeat;

	public void Update(TValueType value, bool isVisible)
	{
		IsVisible = isVisible;
		if (_currentValue.Equals(value) && _timesDeniedRepeat < 30)
		{
			_timesDeniedRepeat++;
			return;
		}
		_timesDeniedRepeat = 0;
		_currentValue = value;
		_needsToSendMessage = true;
	}
}
