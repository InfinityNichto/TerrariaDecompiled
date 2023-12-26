using System.Text;

namespace ReLogic.Text;

internal static class StringBuilderExtension
{
	internal static bool IsEmpty(this StringBuilder stringBuilder)
	{
		return stringBuilder.Length == 0;
	}
}
