using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Text;

namespace ReLogic.Graphics;

public class DynamicSpriteFont : IFontMetrics
{
	private struct SpriteCharacterData
	{
		public readonly Texture2D Texture;

		public readonly Rectangle Glyph;

		public readonly Rectangle Padding;

		public readonly Vector3 Kerning;

		public SpriteCharacterData(Texture2D texture, Rectangle glyph, Rectangle padding, Vector3 kerning)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			Texture = texture;
			Glyph = glyph;
			Padding = padding;
			Kerning = kerning;
		}

		public GlyphMetrics ToGlyphMetric()
		{
			return GlyphMetrics.FromKerningData(Kerning.X, Kerning.Y, Kerning.Z);
		}
	}

	private Dictionary<char, SpriteCharacterData> _spriteCharacters = new Dictionary<char, SpriteCharacterData>();

	private SpriteCharacterData _defaultCharacterData;

	public readonly char DefaultCharacter;

	private readonly float _characterSpacing;

	private readonly int _lineSpacing;

	public float CharacterSpacing => _characterSpacing;

	public int LineSpacing => _lineSpacing;

	public DynamicSpriteFont(float spacing, int lineSpacing, char defaultCharacter)
	{
		_characterSpacing = spacing;
		_lineSpacing = lineSpacing;
		DefaultCharacter = defaultCharacter;
	}

	public bool IsCharacterSupported(char character)
	{
		if (character != '\n' && character != '\r')
		{
			return _spriteCharacters.ContainsKey(character);
		}
		return true;
	}

	public bool AreCharactersSupported(IEnumerable<char> characters)
	{
		foreach (char character in characters)
		{
			if (!IsCharacterSupported(character))
			{
				return false;
			}
		}
		return true;
	}

	internal void SetPages(FontPage[] pages)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		FontPage[] array = pages;
		foreach (FontPage fontPage in array)
		{
			num += fontPage.Characters.Count;
		}
		_spriteCharacters = new Dictionary<char, SpriteCharacterData>(num);
		array = pages;
		foreach (FontPage fontPage2 in array)
		{
			for (int i = 0; i < fontPage2.Characters.Count; i++)
			{
				_spriteCharacters.Add(fontPage2.Characters[i], new SpriteCharacterData(fontPage2.Texture, fontPage2.Glyphs[i], fontPage2.Padding[i], fontPage2.Kerning[i]));
				if (fontPage2.Characters[i] == DefaultCharacter)
				{
					_defaultCharacterData = _spriteCharacters[fontPage2.Characters[i]];
				}
			}
		}
	}

	internal void InternalDraw(string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		Matrix matrix = Matrix.CreateTranslation((0f - origin.X) * scale.X, (0f - origin.Y) * scale.Y, 0f) * Matrix.CreateRotationZ(rotation);
		Vector2 zero = Vector2.Zero;
		Vector2 one = Vector2.One;
		bool flag = true;
		float x = 0f;
		if ((int)spriteEffects != 0)
		{
			Vector2 vector = MeasureString(text);
			if (((Enum)spriteEffects).HasFlag((Enum)(object)(SpriteEffects)1))
			{
				x = vector.X * scale.X;
				one.X = -1f;
			}
			if (((Enum)spriteEffects).HasFlag((Enum)(object)(SpriteEffects)2))
			{
				zero.Y = (vector.Y - (float)LineSpacing) * scale.Y;
				one.Y = -1f;
			}
		}
		zero.X = x;
		foreach (char c in text)
		{
			switch (c)
			{
			case '\n':
				zero.X = x;
				zero.Y += (float)LineSpacing * scale.Y * one.Y;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			SpriteCharacterData characterData = GetCharacterData(c);
			Vector3 kerning = characterData.Kerning;
			Rectangle padding = characterData.Padding;
			if (((Enum)spriteEffects).HasFlag((Enum)(object)(SpriteEffects)1))
			{
				padding.X -= padding.Width;
			}
			if (((Enum)spriteEffects).HasFlag((Enum)(object)(SpriteEffects)2))
			{
				padding.Y = LineSpacing - characterData.Glyph.Height - padding.Y;
			}
			if (flag)
			{
				kerning.X = Math.Max(kerning.X, 0f);
			}
			else
			{
				zero.X += CharacterSpacing * scale.X * one.X;
			}
			zero.X += kerning.X * scale.X * one.X;
			Vector2 position = zero;
			position.X += (float)padding.X * scale.X;
			position.Y += (float)padding.Y * scale.Y;
			Vector2.Transform(ref position, ref matrix, ref position);
			position += startPosition;
			spriteBatch.Draw(characterData.Texture, position, (Rectangle?)characterData.Glyph, color, rotation, Vector2.Zero, scale, spriteEffects, depth);
			zero.X += (kerning.Y + kerning.Z) * scale.X * one.X;
			flag = false;
		}
	}

	private SpriteCharacterData GetCharacterData(char character)
	{
		if (!_spriteCharacters.TryGetValue(character, out var value))
		{
			return _defaultCharacterData;
		}
		return value;
	}

	public Vector2 MeasureString(string text)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		if (text.Length == 0)
		{
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		zero.Y = LineSpacing;
		float val = 0f;
		int num = 0;
		float num2 = 0f;
		bool flag = true;
		foreach (char c in text)
		{
			switch (c)
			{
			case '\n':
				val = Math.Max(zero.X + Math.Max(num2, 0f), val);
				num2 = 0f;
				zero = Vector2.Zero;
				zero.Y = LineSpacing;
				flag = true;
				num++;
				continue;
			case '\r':
				continue;
			}
			SpriteCharacterData characterData = GetCharacterData(c);
			Vector3 kerning = characterData.Kerning;
			if (flag)
			{
				kerning.X = Math.Max(kerning.X, 0f);
			}
			else
			{
				zero.X += CharacterSpacing + num2;
			}
			zero.X += kerning.X + kerning.Y;
			num2 = kerning.Z;
			zero.Y = Math.Max(zero.Y, characterData.Padding.Height);
			flag = false;
		}
		zero.X += Math.Max(num2, 0f);
		zero.Y += num * LineSpacing;
		zero.X = Math.Max(zero.X, val);
		return zero;
	}

	public string CreateWrappedText(string text, float maxWidth)
	{
		return CreateWrappedText(text, maxWidth, Thread.CurrentThread.CurrentCulture);
	}

	public string CreateWrappedText(string text, float maxWidth, CultureInfo culture)
	{
		WrappedTextBuilder wrappedTextBuilder = new WrappedTextBuilder(this, maxWidth, culture);
		wrappedTextBuilder.Append(text);
		return wrappedTextBuilder.ToString();
	}

	public string CreateCroppedText(string text, float maxWidth)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		Vector2 vector = MeasureString(text);
		Vector2 vector2 = MeasureString("…");
		maxWidth -= vector2.X;
		if (maxWidth <= vector2.X)
		{
			return "…";
		}
		if (vector.X > maxWidth)
		{
			int num = 200;
			while (vector.X > maxWidth && text.Length > 1)
			{
				num--;
				if (num <= 0)
				{
					break;
				}
				text = text.Substring(0, text.Length - 1);
				if (text.Length == 1)
				{
					text = "";
					break;
				}
				vector = MeasureString(text);
			}
			text += "…";
		}
		return text;
	}

	public GlyphMetrics GetCharacterMetrics(char character)
	{
		return GetCharacterData(character).ToGlyphMetric();
	}
}
