using Newtonsoft.Json.Linq;

namespace ReLogic.Peripherals.RGB;

public class IntRgbGameValueTracker : ARgbGameValueTracker<int>
{
	protected override void WriteValueToData(JObject data)
	{
		data.Add("value", JToken.op_Implicit(_currentValue));
	}
}
