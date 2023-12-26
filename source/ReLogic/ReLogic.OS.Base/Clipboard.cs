using System.Text;

namespace ReLogic.OS.Base;

internal abstract class Clipboard : IClipboard
{
	public string Value
	{
		get
		{
			return SanitizeClipboardText(GetClipboard(), allowNewLine: false);
		}
		set
		{
			SetClipboard(value);
		}
	}

	public string MultiLineValue => SanitizeClipboardText(GetClipboard(), allowNewLine: true);

	private static string SanitizeClipboardText(string clipboardText, bool allowNewLine)
	{
		StringBuilder stringBuilder = new StringBuilder(clipboardText.Length);
		for (int i = 0; i < clipboardText.Length; i++)
		{
			if ((clipboardText[i] >= ' ' && clipboardText[i] != '\u007f') || (allowNewLine && clipboardText[i] == '\n'))
			{
				stringBuilder.Append(clipboardText[i]);
			}
		}
		return stringBuilder.ToString();
	}

	protected abstract string GetClipboard();

	protected abstract void SetClipboard(string text);
}
