using System.Globalization;
using System.IO;
using System.Text;

namespace ReLogic.Text;

public class WrappedTextBuilder
{
	private struct NonBreakingText
	{
		public readonly string Text;

		public readonly float Width;

		public readonly float WidthOnNewLine;

		public readonly bool IsWhitespace;

		private IFontMetrics _font;

		public NonBreakingText(IFontMetrics font, string text)
		{
			Text = text;
			IsWhitespace = true;
			float num = 0f;
			float num2 = 0f;
			_font = font;
			for (int i = 0; i < text.Length; i++)
			{
				GlyphMetrics characterMetrics = font.GetCharacterMetrics(text[i]);
				if (i == 0)
				{
					num2 = characterMetrics.KernedWidthOnNewLine - characterMetrics.KernedWidth;
				}
				else
				{
					num += font.CharacterSpacing;
				}
				num += characterMetrics.KernedWidth;
				if (text[i] != ' ')
				{
					IsWhitespace = false;
				}
			}
			Width = num;
			WidthOnNewLine = num + num2;
		}

		public string GetAsWrappedText(float maxWidth)
		{
			float num = 0f;
			StringBuilder stringBuilder = new StringBuilder(Text.Length);
			for (int i = 0; i < Text.Length; i++)
			{
				GlyphMetrics characterMetrics = _font.GetCharacterMetrics(Text[i]);
				num = ((i != 0) ? (num + (_font.CharacterSpacing + characterMetrics.KernedWidth)) : (num + characterMetrics.KernedWidthOnNewLine));
				if (num > maxWidth)
				{
					num = characterMetrics.KernedWidthOnNewLine;
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(Text[i]);
			}
			return stringBuilder.ToString();
		}
	}

	private readonly IFontMetrics _font;

	private readonly CultureInfo _culture;

	private readonly float _maxWidth;

	private readonly StringBuilder _completedText = new StringBuilder();

	private readonly StringBuilder _workingLine = new StringBuilder();

	private float _workingLineWidth;

	public WrappedTextBuilder(IFontMetrics font, float maxWidth, CultureInfo culture)
	{
		_font = font;
		_maxWidth = maxWidth;
		_culture = culture;
		_workingLineWidth = 0f;
	}

	public void CommitWorkingLine()
	{
		if (!_completedText.IsEmpty())
		{
			_completedText.Append('\n');
		}
		_workingLineWidth = 0f;
		_completedText.Append(_workingLine);
		_workingLine.Clear();
	}

	private void Append(NonBreakingText textToken)
	{
		float num = ((!_workingLine.IsEmpty()) ? (_workingLineWidth + _font.CharacterSpacing + textToken.Width) : textToken.WidthOnNewLine);
		if (textToken.WidthOnNewLine > _maxWidth)
		{
			if (!_workingLine.IsEmpty())
			{
				CommitWorkingLine();
			}
			if (textToken.Text.Length == 1)
			{
				_workingLineWidth = num;
				_workingLine.Append(textToken.Text);
			}
			else
			{
				Append(textToken.GetAsWrappedText(_maxWidth));
			}
		}
		else if (num <= _maxWidth)
		{
			_workingLineWidth = num;
			_workingLine.Append(textToken.Text);
		}
		else if (_workingLine.IsEmpty())
		{
			_completedText.Append(textToken.Text);
			_workingLine.Clear();
			_workingLineWidth = 0f;
		}
		else
		{
			CommitWorkingLine();
			if (!textToken.IsWhitespace)
			{
				_workingLine.Append(textToken.Text);
				_workingLineWidth = textToken.WidthOnNewLine;
			}
		}
	}

	public void Append(string text)
	{
		StringReader stringReader = new StringReader(text);
		_completedText.EnsureCapacity(_completedText.Capacity + text.Length);
		while (stringReader.Peek() > 0)
		{
			if ((ushort)stringReader.Peek() == 10)
			{
				stringReader.Read();
				CommitWorkingLine();
			}
			else
			{
				string text2 = stringReader.ReadUntilBreakable(_culture);
				Append(new NonBreakingText(_font, text2));
			}
		}
	}

	public override string ToString()
	{
		if (_completedText.IsEmpty())
		{
			return _workingLine.ToString();
		}
		return _completedText.ToString() + "\n" + _workingLine;
	}
}
