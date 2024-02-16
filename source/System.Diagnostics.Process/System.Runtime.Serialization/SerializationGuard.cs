using System.Reflection;

namespace System.Runtime.Serialization;

internal static class SerializationGuard
{
	private delegate void ThrowIfDeserializationInProgressWithSwitchDel(string switchName, ref int cachedValue);

	private static readonly ThrowIfDeserializationInProgressWithSwitchDel s_throwIfDeserializationInProgressWithSwitch = CreateThrowIfDeserializationInProgressWithSwitchDelegate();

	private static ThrowIfDeserializationInProgressWithSwitchDel CreateThrowIfDeserializationInProgressWithSwitchDelegate()
	{
		ThrowIfDeserializationInProgressWithSwitchDel result = null;
		MethodInfo method = typeof(SerializationInfo).GetMethod("ThrowIfDeserializationInProgress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
		{
			typeof(string),
			typeof(int).MakeByRefType()
		}, Array.Empty<ParameterModifier>());
		if (method != null)
		{
			result = method.CreateDelegate<ThrowIfDeserializationInProgressWithSwitchDel>();
		}
		return result;
	}

	public static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
	{
		s_throwIfDeserializationInProgressWithSwitch?.Invoke(switchSuffix, ref cachedValue);
	}
}
