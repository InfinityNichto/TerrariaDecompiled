using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Drawing;

internal static class ColorTable
{
	private static readonly Lazy<Dictionary<string, Color>> s_colorConstants = new Lazy<Dictionary<string, Color>>(GetColors);

	internal static Dictionary<string, Color> Colors => s_colorConstants.Value;

	private static Dictionary<string, Color> GetColors()
	{
		Dictionary<string, Color> dictionary = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
		FillWithProperties(dictionary, typeof(Color));
		FillWithProperties(dictionary, typeof(SystemColors));
		return dictionary;
	}

	private static void FillWithProperties(Dictionary<string, Color> dictionary, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type typeWithColors)
	{
		PropertyInfo[] properties = typeWithColors.GetProperties(BindingFlags.Static | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.PropertyType == typeof(Color))
			{
				dictionary[propertyInfo.Name] = (Color)propertyInfo.GetValue(null, null);
			}
		}
	}

	internal static bool TryGetNamedColor(string name, out Color result)
	{
		return Colors.TryGetValue(name, out result);
	}
}
