namespace System.Xml.Linq;

internal static class XHelper
{
	internal static bool IsInstanceOfType(object o, Type type)
	{
		if (o != null)
		{
			return type.IsAssignableFrom(o.GetType());
		}
		return false;
	}
}
