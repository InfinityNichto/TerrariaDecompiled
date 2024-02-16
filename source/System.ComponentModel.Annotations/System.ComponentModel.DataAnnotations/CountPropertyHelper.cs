using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

internal static class CountPropertyHelper
{
	[RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
	public static bool TryGetCount(object value, out int count)
	{
		if (value is ICollection collection)
		{
			count = collection.Count;
			return true;
		}
		PropertyInfo runtimeProperty = value.GetType().GetRuntimeProperty("Count");
		if (runtimeProperty != null && runtimeProperty.CanRead && runtimeProperty.PropertyType == typeof(int))
		{
			count = (int)runtimeProperty.GetValue(value);
			return true;
		}
		count = -1;
		return false;
	}
}
